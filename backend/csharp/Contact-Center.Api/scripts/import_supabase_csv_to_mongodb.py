#!/usr/bin/env python3
"""
Import Supabase PostgreSQL CSV exports into MongoDB.

Features:
  - Reads every *.csv in a directory (or one file via --csv + optional --collection)
  - Detects delimiter (; vs ,) — Supabase often exports with ';'
  - Normalizes UUID-shaped values to lowercase strings (recursive)
  - Infers missing tenant_id using account_settings (tenant PK id + owner user_id)
  - Optional enhancement using profiles rows (user_id -> tenant_id)

Requirements:
  pip install pymongo

Example:
  python scripts/import_supabase_csv_to_mongodb.py \\
    --csv-dir docs/csv-exports \\
    --mongo-uri mongodb://localhost:27017 \\
    --database voiceflow_dev

Generic export filename:
  python scripts/import_supabase_csv_to_mongodb.py \\
    --csv docs/query-results-export-2026-05-11_18-17-24.csv \\
    --collection calls \\
    --mongo-uri mongodb://localhost:27017 \\
    --database voiceflow_dev

Naming convention for directories:
  Place files as <table_name>.csv, e.g. calls.csv, profiles.csv, account_settings.csv.
"""

from __future__ import annotations

import argparse
import csv
import io
import json
import re
import sys
from pathlib import Path
from typing import Any

try:
    from pymongo import MongoClient
    from pymongo.errors import BulkWriteError
except ImportError as exc:
    print("Missing dependency: pymongo\n  pip install pymongo", file=sys.stderr)
    raise SystemExit(1) from exc

UUID_RE = re.compile(
    r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"
)

# Insert tenant-bearing tables after account_settings + profiles when possible
IMPORT_PRIORITY = (
    "account_settings",
    "profiles",
    "rbac_roles",
    "rbac_user_roles",
    "flows",
    "contacts",
    "tags",
    "calls",
    "voice_library",
    "sip_accounts",
    "edit_logs",
    "invoices",
    "refresh_tokens",
    "auth_users",
)


def is_blank(val: Any) -> bool:
    if val is None:
        return True
    if isinstance(val, str) and val.strip() == "":
        return True
    return False


def normalize_uuid_string(s: str) -> str:
    return s.strip().lower()


def is_uuid_string(s: str) -> bool:
    return bool(UUID_RE.match(s.strip()))


def coerce_cell(raw: str) -> Any:
    """Turn CSV cell into JSON-friendly Python values."""
    if raw is None:
        return None
    text = raw.strip()
    if text == "":
        return None
    lower = text.lower()
    if lower == "true":
        return True
    if lower == "false":
        return False
    # JSON object / array (Postgres json/jsonb export)
    if (text.startswith("{") and text.endswith("}")) or (
        text.startswith("[") and text.endswith("]")
    ):
        try:
            return json.loads(text)
        except json.JSONDecodeError:
            return text
    # PostgreSQL array literal: {a,b,c}
    if text.startswith("{") and text.endswith("}") and text != "{}":
        inner = text[1:-1]
        if inner == "":
            return []
        # naive split — quoted elements rare in exports
        parts = [p.strip().strip('"') for p in inner.split(",")]
        return parts
    return text


def stringify_uuids(value: Any) -> Any:
    if isinstance(value, str):
        return normalize_uuid_string(value) if is_uuid_string(value) else value
    if isinstance(value, list):
        return [stringify_uuids(v) for v in value]
    if isinstance(value, dict):
        return {k: stringify_uuids(v) for k, v in value.items()}
    return value


def detect_delimiter(sample: str) -> str:
    first_line = sample.splitlines()[0] if sample else ""
    semi = first_line.count(";")
    comma = first_line.count(",")
    return ";" if semi > comma else ","


def read_csv_rows(path: Path) -> tuple[list[str], list[dict[str, Any]]]:
    raw = path.read_text(encoding="utf-8-sig", errors="replace")
    delim = detect_delimiter(raw)
    reader = csv.DictReader(io.StringIO(raw), delimiter=delim)
    fieldnames = reader.fieldnames or []
    rows: list[dict[str, Any]] = []
    for row in reader:
        doc: dict[str, Any] = {}
        for k, v in row.items():
            if k is None:
                continue
            key = k.strip()
            doc[key] = coerce_cell(v if v is not None else "")
        rows.append(doc)
    return list(fieldnames), rows


def collection_sort_key(name: str) -> tuple[int, str]:
    try:
        idx = IMPORT_PRIORITY.index(name)
    except ValueError:
        idx = len(IMPORT_PRIORITY)
    return idx, name


def build_tenant_maps(tables: dict[str, list[dict[str, Any]]]) -> dict[str, str]:
    """auth user_id -> tenant uuid string (from account_settings PK id + profiles fallback)."""
    tenant_by_user: dict[str, str] = {}

    acct_rows = tables.get("account_settings") or []
    for row in acct_rows:
        tid = row.get("tenant_id") or row.get("id")
        uid = row.get("user_id")
        if isinstance(tid, str) and is_uuid_string(tid) and isinstance(uid, str) and is_uuid_string(uid):
            uid_s = normalize_uuid_string(uid)
            tenant_by_user[uid_s] = normalize_uuid_string(str(tid))

    prof_rows = tables.get("profiles") or []
    for row in prof_rows:
        uid = row.get("user_id")
        tid = row.get("tenant_id")
        if isinstance(uid, str) and is_uuid_string(uid) and isinstance(tid, str) and is_uuid_string(tid):
            tenant_by_user.setdefault(normalize_uuid_string(uid), normalize_uuid_string(tid))

    return tenant_by_user


def ensure_tenant_id(
    collection: str,
    doc: dict[str, Any],
    tenant_by_user: dict[str, str],
) -> dict[str, Any]:
    if collection == "account_settings":
        return stringify_uuids(doc)
    tid = doc.get("tenant_id")
    if isinstance(tid, str) and is_uuid_string(tid):
        doc["tenant_id"] = normalize_uuid_string(tid)
        return stringify_uuids(doc)
    if not is_blank(tid):
        return stringify_uuids(doc)

    uid = doc.get("user_id")
    if isinstance(uid, str) and is_uuid_string(uid):
        mapped = tenant_by_user.get(normalize_uuid_string(uid))
        if mapped:
            doc["tenant_id"] = mapped
    return stringify_uuids(doc)


def mongo_document(doc: dict[str, Any]) -> dict[str, Any]:
    """Use top-level id as _id when it's a UUID string."""
    out = stringify_uuids(doc)
    pk = out.get("id")
    if isinstance(pk, str) and is_uuid_string(pk):
        out["_id"] = normalize_uuid_string(pk)
    return out


def discover_tables(csv_dir: Path) -> dict[str, Path]:
    mapping: dict[str, Path] = {}
    for p in sorted(csv_dir.glob("*.csv")):
        stem = p.stem.lower()
        mapping[stem] = p
    return mapping


def parse_main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Import Supabase CSV exports into MongoDB.")
    src = parser.add_mutually_exclusive_group(required=True)
    src.add_argument(
        "--csv-dir",
        type=Path,
        help="Directory containing <table>.csv files",
    )
    src.add_argument(
        "--csv",
        type=Path,
        help="Single CSV file (use --collection if filename is not <table>.csv)",
    )
    parser.add_argument(
        "--collection",
        help="Mongo collection name when --csv points to a generic export filename",
    )
    parser.add_argument(
        "--mongo-uri",
        default="mongodb://localhost:27017",
        help="MongoDB connection URI (default: %(default)s)",
    )
    parser.add_argument(
        "--database",
        default="voiceflow_dev",
        help="Database name (default: %(default)s)",
    )
    parser.add_argument(
        "--drop",
        action="store_true",
        help="Drop each target collection before inserting",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Parse CSV and print counts without writing MongoDB",
    )
    parser.add_argument(
        "--batch-size",
        type=int,
        default=500,
        help="insert_many batch size (default: %(default)s)",
    )
    args = parser.parse_args(argv)

    files: dict[str, Path] = {}
    if args.csv_dir:
        files = discover_tables(args.csv_dir)
        if not files:
            print(f"No .csv files under {args.csv_dir}", file=sys.stderr)
            return 1
    else:
        if args.collection:
            files[args.collection.strip().lower()] = args.csv
        else:
            stem = args.csv.stem.lower()
            if stem.startswith("query-results-export") or stem.startswith("export"):
                print(
                    "Filename looks generic; pass --collection <supabase_table_name>",
                    file=sys.stderr,
                )
                return 1
            files[stem] = args.csv

    print(f"Loading {len(files)} CSV file(s)...")
    tables: dict[str, list[dict[str, Any]]] = {}
    for coll, path in files.items():
        _, rows = read_csv_rows(path)
        tables[coll] = rows
        print(f"  - {coll}: {len(rows)} rows ({path.name})")

    tenant_by_user = build_tenant_maps(tables)

    if args.dry_run:
        print("[dry-run] Tenant mappings from account_settings/profiles:", len(tenant_by_user))
        for name in sorted(tables.keys(), key=collection_sort_key):
            missing = sum(
                1
                for r in tables[name]
                if name != "account_settings"
                and is_blank(r.get("tenant_id"))
                and not (
                    isinstance(r.get("user_id"), str)
                    and is_uuid_string(r["user_id"])
                    and normalize_uuid_string(r["user_id"]) in tenant_by_user
                )
            )
            print(f"  - {name}: rows lacking tenant_id & user lookup: {missing}")
        return 0

    client = MongoClient(args.mongo_uri)
    db = client[args.database]

    ordered_names = sorted(tables.keys(), key=collection_sort_key)
    had_bulk_errors = False

    for coll_name in ordered_names:
        rows = tables[coll_name]
        col = db[coll_name]
        if args.drop:
            col.drop()
            print(f"Dropped collection {coll_name}")

        docs: list[dict[str, Any]] = []
        for row in rows:
            merged = ensure_tenant_id(coll_name, dict(row), tenant_by_user)
            docs.append(mongo_document(merged))

        inserted = 0
        for i in range(0, len(docs), args.batch_size):
            chunk = docs[i : i + args.batch_size]
            try:
                res = col.insert_many(chunk, ordered=False)
                inserted += len(res.inserted_ids)
            except BulkWriteError as e:
                had_bulk_errors = True
                inserted += e.details.get("nInserted", 0)
                errs = e.details.get("writeErrors") or []
                print(f"  [!] {coll_name}: BulkWriteError — inserted so far {inserted}, errors {len(errs)}")
                for err in errs[:5]:
                    print(f"      {err}")
        print(f"Inserted {inserted} document(s) into {args.database}.{coll_name}")

    client.close()
    print("Done.")
    return 1 if had_bulk_errors else 0


if __name__ == "__main__":
    raise SystemExit(parse_main(sys.argv[1:]))
