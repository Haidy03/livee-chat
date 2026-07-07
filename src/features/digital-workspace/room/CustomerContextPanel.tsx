import { useMemo, useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  CircleUser,
  History,
  Briefcase,
  StickyNote,
  Building2,
  BookOpen,
  Sparkles,
  Copy,
  Check,
  Star,
  ThumbsDown,
  ThumbsUp,
} from "lucide-react";
import { format } from "date-fns";
import type { Room } from "../models";
import {
  useCustomerStore,
  useKnowledgeStore,
  useLayoutStore,
  usePreferencesStore,
} from "../stores";
import { ChannelIcon } from "../shared/ChannelIcon";
import { getMockAISuggestions } from "../mocks";
import { useToast } from "@/hooks/use-toast";
import { cn } from "@/lib/utils";

const EMPTY_JOURNEY: ReturnType<typeof useCustomerStore.getState>["journeyByCustomer"][string] = [];
const EMPTY_CASES: ReturnType<typeof useCustomerStore.getState>["casesByCustomer"][string] = [];

interface Props {
  room: Room;
}

export function CustomerContextPanel({ room: c }: Props) {
  const customer = useCustomerStore((s) => s.byId[c.customerId]);
  const tab = useLayoutStore((s) => s.activeContextTab);
  const setTab = useLayoutStore((s) => s.setActiveContextTab);
  const showAI = usePreferencesStore((s) => s.showAIAssist);
  if (!customer) return null;

  return (
    <aside className="flex h-full flex-col bg-card">
      <Tabs value={tab} onValueChange={setTab} className="flex h-full min-h-0 flex-col">
        <TabsList className="grid h-8 w-full grid-cols-7 rounded-none border-b border-app-border bg-transparent p-0">
          {[
            { id: "overview", icon: CircleUser },
            { id: "customer", icon: CircleUser },
            { id: "history", icon: History },
            { id: "cases", icon: Briefcase },
            { id: "notes", icon: StickyNote },
            { id: "knowledge", icon: BookOpen },
            { id: showAI ? "ai" : "crm", icon: showAI ? Sparkles : Building2 },
          ].map((t) => (
            <TabsTrigger key={t.id} value={t.id} className="h-7 rounded-none text-[10px] capitalize" title={t.id}>
              <t.icon className="h-3.5 w-3.5" />
            </TabsTrigger>
          ))}
        </TabsList>

        <ScrollArea className="flex-1">
          <TabsContent value="overview" className="m-0 p-3">
            <OverviewTab room={c} />
          </TabsContent>
          <TabsContent value="customer" className="m-0 p-3">
            <CustomerTab room={c} />
          </TabsContent>
          <TabsContent value="history" className="m-0 p-3">
            <HistoryTab customerId={customer.id} />
          </TabsContent>
          <TabsContent value="cases" className="m-0 p-3">
            <CasesTab customerId={customer.id} />
          </TabsContent>
          <TabsContent value="notes" className="m-0 p-3">
            <NotesTab customerId={customer.id} />
          </TabsContent>
          <TabsContent value="knowledge" className="m-0 p-3">
            <KnowledgeTab />
          </TabsContent>
          <TabsContent value="crm" className="m-0 p-3">
            <CRMTab />
          </TabsContent>
          <TabsContent value="ai" className="m-0 p-3">
            <AIAssistTab room={c} />
          </TabsContent>
        </ScrollArea>
      </Tabs>
    </aside>
  );
}

function Row({ k, v }: { k: string; v?: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3 border-b border-app-border/60 py-1.5 text-xs">
      <span className="text-muted-foreground">{k}</span>
      <span className="text-end font-medium" dir="auto">{v ?? "—"}</span>
    </div>
  );
}

function OverviewTab({ room: c }: { room: Room }) {
  const customer = useCustomerStore((s) => s.byId[c.customerId]);
  if (!customer) return null;
  return (
    <div>
      <h3 className="text-sm font-semibold">Snapshot</h3>
      <p className="mt-1 text-xs text-muted-foreground">{c.subject ?? "No subject"}</p>
      <div className="mt-3 grid gap-1.5">
        <Row k="Intent" v="support.order.status" />
        <Row k="Sentiment" v={c.sentiment} />
        <Row k="Language" v={c.language} />
        <Row k="Segment" v={customer.segment ?? "—"} />
        <Row k="Priority" v={c.priority} />
        <Row k="Queue" v={c.queueId} />
        <Row k="Tags" v={c.tags.join(", ") || "—"} />
        <Row k="Previous interactions" v="2" />
        <Row k="Open cases" v="1" />
      </div>
    </div>
  );
}

function CustomerTab({ room: c }: { room: Room }) {
  const customer = useCustomerStore((s) => s.byId[c.customerId])!;
  return (
    <div>
      <div className="flex items-center gap-2">
        <Avatar className="h-9 w-9"><AvatarFallback>{customer.name[0]}</AvatarFallback></Avatar>
        <div className="min-w-0">
          <h3 className="truncate text-sm font-semibold" dir="auto">{customer.name}</h3>
          <p className="text-[11px] text-muted-foreground">{customer.id}</p>
        </div>
        {customer.vip && <Star className="ms-auto h-4 w-4 fill-amber-400 text-amber-500" />}
      </div>
      <div className="mt-3 grid gap-1.5">
        <Row k="Phone" v={customer.phone ?? "—"} />
        <Row k="Email" v={customer.email ?? "—"} />
        <Row k="Country" v={customer.country ?? "—"} />
        <Row k="Language" v={customer.language} />
        <Row k="Segment" v={customer.segment} />
      </div>
      <h4 className="mt-4 mb-1 text-xs font-semibold text-muted-foreground">Identities</h4>
      <ul className="space-y-1.5">
        {customer.identities.map((id) => (
          <li key={id.id} className="flex items-center gap-2 rounded-md border border-app-border px-2 py-1.5 text-xs">
            <ChannelIcon channel={id.channel} size={12} />
            <span className="truncate" dir="auto">{id.handle}</span>
            {id.verified && <Check className="ms-auto h-3 w-3 text-emerald-600" />}
          </li>
        ))}
      </ul>
      <h4 className="mt-4 mb-1 text-xs font-semibold text-muted-foreground">Tags</h4>
      <div className="flex flex-wrap gap-1">
        {customer.tags.length === 0 && <span className="text-[11px] text-muted-foreground">No tags</span>}
        {customer.tags.map((t) => <Badge key={t} variant="outline">{t}</Badge>)}
      </div>
    </div>
  );
}

function HistoryTab({ customerId }: { customerId: string }) {
  const items = useCustomerStore((s) => s.journeyByCustomer[customerId] ?? EMPTY_JOURNEY);
  if (!items.length) return <p className="text-xs text-muted-foreground">No history.</p>;
  return (
    <ul className="space-y-2">
      {items.map((j) => (
        <li key={j.id} className="rounded-md border border-app-border p-2 text-xs">
          <div className="flex items-center gap-1.5">
            <ChannelIcon channel={j.channel} size={11} withBackground={false} />
            <span className="font-medium">{j.agentName}</span>
            <span className="ms-auto text-[10px] text-muted-foreground">{format(new Date(j.at), "MMM d, HH:mm")}</span>
          </div>
          <p className="mt-1 text-foreground/90">{j.summary}</p>
          <div className="mt-1 flex flex-wrap gap-1 text-[10px] text-muted-foreground">
            <span>Status: {j.status}</span>
            {j.intent && <span>Intent: {j.intent}</span>}
            {j.sentiment && <span>Sentiment: {j.sentiment}</span>}
          </div>
        </li>
      ))}
    </ul>
  );
}

function CasesTab({ customerId }: { customerId: string }) {
  const items = useCustomerStore((s) => s.casesByCustomer[customerId] ?? EMPTY_CASES);
  return (
    <div>
      <div className="mb-2 flex items-center justify-between">
        <h3 className="text-xs font-semibold text-muted-foreground">Cases</h3>
        <Button size="sm" variant="outline" className="h-6 text-xs">+ New</Button>
      </div>
      {items.length === 0 ? (
        <p className="text-xs text-muted-foreground">No cases yet.</p>
      ) : (
        <ul className="space-y-2">
          {items.map((c) => (
            <li key={c.id} className="rounded-md border border-app-border p-2 text-xs">
              <div className="flex items-center justify-between">
                <span className="font-medium">{c.subject}</span>
                <Badge variant="outline" className="capitalize">{c.status}</Badge>
              </div>
              <p className="mt-1 text-[10px] text-muted-foreground">
                {c.id} · {c.priority} · updated {format(new Date(c.updatedAt), "MMM d, HH:mm")}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function NotesTab({ customerId }: { customerId: string }) {
  const [notes, setNotes] = useState<{ id: string; body: string; at: string }[]>([]);
  const [val, setVal] = useState("");
  return (
    <div>
      <div className="flex gap-1">
        <Input value={val} onChange={(e) => setVal(e.target.value)} placeholder="Add a note…" className="h-7 text-xs" dir="auto" />
        <Button
          size="sm"
          className="h-7"
          onClick={() => {
            if (!val.trim()) return;
            setNotes((p) => [{ id: String(Date.now()), body: val, at: new Date().toISOString() }, ...p]);
            setVal("");
          }}
        >Add</Button>
      </div>
      <ul className="mt-3 space-y-2">
        {notes.map((n) => (
          <li key={n.id} className="rounded-md border border-app-border p-2 text-xs">
            <p dir="auto">{n.body}</p>
            <p className="mt-1 text-[10px] text-muted-foreground">{format(new Date(n.at), "MMM d, HH:mm")} · you</p>
          </li>
        ))}
        {!notes.length && <p className="text-xs text-muted-foreground">No notes for this customer.</p>}
      </ul>
    </div>
  );
}

function KnowledgeTab() {
  const [q, setQ] = useState("");
  const articles = useKnowledgeStore((s) => s.articles);
  const filtered = useMemo(
    () => articles.filter((a) => !q || a.title.toLowerCase().includes(q.toLowerCase())),
    [articles, q],
  );
  return (
    <div>
      <Input value={q} onChange={(e) => setQ(e.target.value)} placeholder="Search KB…" className="h-7 text-xs" />
      <ul className="mt-3 space-y-2">
        {filtered.map((a) => (
          <li key={a.id} className="rounded-md border border-app-border p-2 text-xs">
            <p className="font-semibold">{a.title}</p>
            <p className="mt-1 text-muted-foreground">{a.excerpt}</p>
            <div className="mt-2 flex items-center gap-1">
              <Button size="sm" variant="outline" className="h-6 text-[11px]"><Copy className="me-1 h-3 w-3" />Copy</Button>
              <span className="ms-auto text-[10px] text-muted-foreground">
                {a.source} · {format(new Date(a.updatedAt), "MMM d")}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function CRMTab() {
  return (
    <div className="rounded-md border border-dashed border-app-border p-3 text-xs">
      <p className="font-semibold">External CRM widget</p>
      <p className="mt-1 text-muted-foreground">
        Connect a CRM, ticketing, OMS, payments, or any iframe-able widget here. This area is intentionally
        decoupled — see <code>ExternalWidget</code> for embedding.
      </p>
      <div className="mt-3 grid grid-cols-2 gap-2 text-[11px]">
        <Row k="Lifetime value" v="$2,481" />
        <Row k="Orders" v="14" />
        <Row k="Open tickets" v="1" />
        <Row k="Last order" v="3d ago" />
      </div>
    </div>
  );
}

function AIAssistTab({ room: c }: { room: Room }) {
  const customer = useCustomerStore((s) => s.byId[c.customerId])!;
  const suggestions = useMemo(() => getMockAISuggestions(c, customer), [c, customer]);
  const setDraft = usePreferencesStore((s) => s.setDraft);
  const { toast } = useToast();
  return (
    <div>
      <div className="mb-2 flex items-center gap-1.5 text-[11px] uppercase tracking-wider text-muted-foreground">
        <Sparkles className="h-3.5 w-3.5" /> AI Assist (mock)
      </div>
      <ul className="space-y-2">
        {suggestions.map((s) => (
          <li key={s.id} className="rounded-md border border-app-border p-2 text-xs">
            <div className="flex items-center justify-between">
              <span className="font-semibold capitalize">{s.title}</span>
              <span className="text-[10px] text-muted-foreground">{Math.round(s.confidence * 100)}%</span>
            </div>
            <p className="mt-1 whitespace-pre-wrap text-foreground/90" dir="auto">{s.body}</p>
            <div className="mt-2 flex items-center gap-1">
              {s.kind === "reply" && (
                <Button
                  size="sm"
                  variant="outline"
                  className="h-6 text-[11px]"
                  onClick={() => { setDraft(c.id, s.body); toast({ title: "Inserted into composer" }); }}
                >
                  Insert
                </Button>
              )}
              <Button size="sm" variant="ghost" className="h-6 text-[11px]"
                onClick={() => navigator.clipboard.writeText(s.body)}>
                <Copy className="h-3 w-3" />
              </Button>
              <span className="ms-auto inline-flex gap-1">
                <button aria-label="Thumbs up" className="rounded p-1 hover:bg-muted"><ThumbsUp className="h-3 w-3" /></button>
                <button aria-label="Thumbs down" className="rounded p-1 hover:bg-muted"><ThumbsDown className="h-3 w-3" /></button>
              </span>
            </div>
            {s.source && <p className="mt-1 text-[10px] text-muted-foreground">Source: {s.source}</p>}
          </li>
        ))}
      </ul>
      <p className={cn("mt-3 text-[10px] text-muted-foreground")}>
        AI suggestions are never sent automatically — review before insert.
      </p>
    </div>
  );
}
