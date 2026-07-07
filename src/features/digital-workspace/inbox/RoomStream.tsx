import { useMemo } from "react";
import {
  ChevronsLeft,
  ChevronsRight,
  Filter as FilterIcon,
  Search,
  SortAsc,
} from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuCheckboxItem,
} from "@/components/ui/dropdown-menu";
import { RoomListItem } from "./RoomListItem";
import { RoomRailItem } from "./RoomRailItem";
import { useInboxRooms } from "../hooks/useInbox";
import {
  useRoomStore,
  useInboxStore,
  useLayoutStore,
  usePreferencesStore,
} from "../stores";
import { EmptyState } from "../shared/States";
import { ALL_CHANNELS } from "../models";
import { CHANNEL_LABEL } from "../shared/channelTokens";

const SORT_LABELS: Record<string, string> = {
  newest: "Newest activity",
  oldest: "Oldest activity",
  longest_waiting: "Longest waiting",
  sla: "Nearest SLA",
  priority: "Highest priority",
  name: "Customer name",
};

export function RoomStream() {
  const list = useInboxRooms();
  const selectedId = useRoomStore((s) => s.selectedId);
  const openTab = useRoomStore((s) => s.openTab);
  const filter = useInboxStore((s) => s.filter);
  const setFilter = useInboxStore((s) => s.setFilter);
  const sort = useInboxStore((s) => s.sort);
  const setSort = useInboxStore((s) => s.setSort);
  const activeView = useInboxStore((s) => s.activeView);
  const streamCollapsed = useLayoutStore((s) => s.streamCollapsed);
  const setStreamCollapsed = useLayoutStore((s) => s.setStreamCollapsed);
  const compactList = usePreferencesStore((s) => s.compactList);

  const title = useMemo(() => activeView.replace(/_/g, " "), [activeView]);

  if (streamCollapsed) {
    return (
      <div className="flex h-full w-16 flex-col bg-card">
        <div className="flex items-center justify-center border-b border-app-border px-1 py-2">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            onClick={() => setStreamCollapsed(false)}
            aria-label="Expand"
          >
            <ChevronsRight className="h-3.5 w-3.5" />
          </Button>
        </div>
        <div className="flex-1 overflow-y-auto" role="list" aria-label="Rooms">
          {list.length === 0 ? (
            <EmptyState title="No rooms" description="Nothing matches the current view." />
          ) : (
            list.map((c) => (
              <RoomRailItem
                key={c.id}
                room={c}
                selected={c.id === selectedId}
                onSelect={() => openTab(c.id)}
              />
            ))
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full w-full flex-col bg-card">
      <div className="flex flex-col gap-2 border-b border-app-border px-3 py-2">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-semibold capitalize">{title}</h2>
            <span className="rounded-full bg-muted px-1.5 py-0.5 text-[10px] tabular-nums text-muted-foreground">
              {list.length}
            </span>
          </div>
          <div className="flex items-center gap-0.5">
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={() => setStreamCollapsed(true)}
              aria-label="Collapse"
            >
              <ChevronsLeft className="h-3.5 w-3.5" />
            </Button>
          </div>
        </div>
        <div className="relative">
          <Search className="pointer-events-none absolute start-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={filter.search}
            onChange={(e) => setFilter({ search: e.target.value })}
            placeholder="Search name, phone, ID, content…"
            className="h-8 ps-7 text-xs"
            aria-label="Search rooms"
          />
        </div>
        <div className="flex items-center gap-1">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="h-7 px-2 text-xs">
                <FilterIcon className="me-1 h-3 w-3" /> Filter
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="w-56">
              <DropdownMenuLabel>Channel</DropdownMenuLabel>
              {ALL_CHANNELS.map((ch) => (
                <DropdownMenuCheckboxItem
                  key={ch}
                  checked={filter.channels.includes(ch)}
                  onCheckedChange={(v) => {
                    const next = new Set(filter.channels);
                    v ? next.add(ch) : next.delete(ch);
                    setFilter({ channels: Array.from(next) });
                  }}
                >
                  {CHANNEL_LABEL[ch]}
                </DropdownMenuCheckboxItem>
              ))}
              <DropdownMenuSeparator />
              <DropdownMenuCheckboxItem
                checked={filter.unreadOnly}
                onCheckedChange={(v) => setFilter({ unreadOnly: !!v })}
              >
                Unread only
              </DropdownMenuCheckboxItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm" className="h-7 px-2 text-xs">
                <SortAsc className="me-1 h-3 w-3" /> {SORT_LABELS[sort]}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              {Object.entries(SORT_LABELS).map(([k, label]) => (
                <DropdownMenuItem key={k} onClick={() => setSort(k as any)}>
                  {label}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>

      <div className="flex-1 overflow-y-auto" role="list" aria-label="Rooms">
        {list.length === 0 ? (
          <EmptyState title="No rooms" description="Nothing matches the current view and filters." />
        ) : (
          list.map((c) => (
            <RoomListItem
              key={c.id}
              room={c}
              selected={c.id === selectedId}
              onSelect={() => openTab(c.id)}
              compact={compactList}
              showCloseAction={activeView === "assigned_me"}
            />
          ))
        )}
      </div>
    </div>
  );
}
