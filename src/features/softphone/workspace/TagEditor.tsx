import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { Plus } from "lucide-react";
import { toast } from "sonner";
import { PRESET_TAGS, tagColor } from "./tagColors";
import { useTagsStore } from "./useTagsStore";

interface Props {
  rowId: string;
}

export function TagEditor({ rowId }: Props) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [value, setValue] = useState("");
  const wrapRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const existing = useTagsStore((s) => s.tags[rowId]) ?? [];
  const add = useTagsStore((s) => s.add);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (wrapRef.current && !wrapRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  const submit = (v: string) => {
    const trimmed = v.trim();
    if (!trimmed) return;
    if (existing.includes(trimmed)) return;
    add(rowId, trimmed);
    toast.success(t("softphone.tags.added"));
    setValue("");
  };

  if (!open) {
    return (
      <button
        type="button"
        onClick={(e) => {
          e.stopPropagation();
          setOpen(true);
        }}
        className="inline-flex items-center gap-1 rounded-full border border-dashed px-2 py-0.5 text-[11px] font-medium transition-colors"
        style={{ color: `hsl(var(--sp-muted))`, borderColor: `hsl(var(--sp-border))` }}
      >
        <Plus className="h-3 w-3" /> {t("softphone.tags.tag")}
      </button>
    );
  }

  return (
    <div
      ref={wrapRef}
      onClick={(e) => e.stopPropagation()}
      className="relative"
    >
      <div
        className="absolute z-30 top-0 start-0 w-64 rounded-xl border p-2 shadow-lg"
        style={{
          background: `hsl(var(--sp-window))`,
          borderColor: `hsl(var(--sp-border))`,
          boxShadow: "var(--sp-shadow)",
        }}
      >
        <input
          ref={inputRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              submit(value);
            } else if (e.key === "Escape") {
              setOpen(false);
            }
          }}
          placeholder={t("softphone.tags.typeAndEnter")}
          className="w-full rounded-md px-2 py-1.5 text-xs outline-none border"
          style={{
            background: `hsl(var(--sp-surface))`,
            borderColor: `hsl(var(--sp-border))`,
            color: `hsl(var(--sp-text))`,
          }}
        />
        <div className="mt-2 flex flex-wrap gap-1">
          {PRESET_TAGS.filter((tag) => !existing.includes(tag)).map((tag) => {
            const c = tagColor(tag);
            return (
              <button
                key={tag}
                type="button"
                onClick={() => submit(tag)}
                className="inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-medium hover:opacity-80"
                style={{
                  background: `hsl(${c.bg})`,
                  color: `hsl(${c.fg})`,
                  borderColor: `hsl(${c.border})`,
                }}
              >
                {tag}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}
