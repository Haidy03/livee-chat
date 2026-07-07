import { useEffect, useMemo, useRef, useState, useCallback } from "react";
import { useLocation } from "react-router-dom";
import { PanelGroup, Panel, PanelResizeHandle, type ImperativePanelGroupHandle } from "react-resizable-panels";
import { Button } from "@/components/ui/button";
import { X } from "lucide-react";
import { cn } from "@/lib/utils";

import {
  useRoomStore,
  useCustomerStore,
  useInboxStore,
  useLayoutStore,
  useRealtimeStore,
} from "../stores";
import type { Room } from "../models";
import { ChannelIcon } from "../shared/ChannelIcon";
import { RoomStream } from "../inbox/RoomStream";
import { RoomHeader } from "../room/RoomHeader";
import { MessageTimeline } from "../room/MessageTimeline";
import { MessageComposer } from "../room/MessageComposer";
import { CustomerJourneyStrip } from "../room/CustomerJourneyStrip";
import { CustomerContextPanel } from "../room/CustomerContextPanel";
import { AssignmentDialog } from "../room/AssignmentDialog";
import { WrapUpDialog } from "../room/WrapUpDialog";
import { MockDevPanel } from "../room/MockDevPanel";
import { EmptyState } from "../shared/States";

const RAIL_WIDTH = 64;

export function LiveChatWorkspace() {
  const location = useLocation();

  const selectedId = useRoomStore((s) => s.selectedId);
  const byId = useRoomStore((s) => s.byId);
  const openTabs = useRoomStore((s) => s.openTabs);
  const openTab = useRoomStore((s) => s.openTab);
  const closeTab = useRoomStore((s) => s.closeTab);
  const customerById = useCustomerStore((s) => s.byId);

  const streamCollapsed = useLayoutStore((s) => s.streamCollapsed);
  const contextCollapsed = useLayoutStore((s) => s.contextCollapsed);
  const setContextCollapsed = useLayoutStore((s) => s.setContextCollapsed);
  const panelSizes = useLayoutStore((s) => s.panelSizes);
  const setPanelSizes = useLayoutStore((s) => s.setPanelSizes);
  const setPixelWidths = useLayoutStore((s) => s.setPixelWidths);
  const expandedSizes = useLayoutStore((s) => s.expandedSizes);
  const setExpandedSizes = useLayoutStore((s) => s.setExpandedSizes);

  const setView = useInboxStore((s) => s.setView);

  const openRooms = useMemo(
    () => openTabs.map((id) => byId[id]).filter((item): item is Room => Boolean(item)),
    [openTabs, byId],
  );
  const consumedDeepLinkRef = useRef(false);

  const panelSizesRef = useRef(panelSizes);
  useEffect(() => {
    panelSizesRef.current = panelSizes;
  }, [panelSizes]);

  useEffect(() => {
    if (consumedDeepLinkRef.current) return;
    consumedDeepLinkRef.current = true;
    const params = new URLSearchParams(location.search);
    const cid = params.get("roomId");
    if (cid && byId[cid] && cid !== useRoomStore.getState().selectedId) {
      useRoomStore.getState().openTab(cid);
    }
    const view = params.get("view");
    if (view) setView(view as any);
  }, [location.search, byId, setView]);

  const conv = selectedId ? byId[selectedId] : null;
  const customer = useCustomerStore((s) => (conv ? s.byId[conv.customerId] : undefined));

  const [assignOpen, setAssignOpen] = useState<{ open: boolean; mode: "assign" | "transfer" }>({
    open: false,
    mode: "assign",
  });
  const [wrapOpen, setWrapOpen] = useState(false);

  const mainRef = useRef<HTMLDivElement>(null);
  const groupRef = useRef<ImperativePanelGroupHandle>(null);
  const [containerWidth, setContainerWidth] = useState(0);
  const lastLayoutRef = useRef<string>("");
  const lastPixelWidthsRef = useRef<string>("");

  useEffect(() => {
    const el = mainRef.current;
    if (!el) return;
    const ro = new ResizeObserver((entries) => {
      for (const entry of entries) {
        const nextWidth = Math.round(entry.contentRect.width);
        setContainerWidth((current) => (Math.abs(current - nextWidth) > 1 ? nextWidth : current));
      }
    });
    ro.observe(el);
    return () => ro.disconnect();
  }, []);

  const handleLayout = useCallback(
    (sizes: number[]) => {
      const cw = containerWidth;
      if (!cw) return;
      const roundedSizes = sizes.map((size) => Number(size.toFixed(2)));
      const layoutKey = `${streamCollapsed ? "s0" : "s1"}-${contextCollapsed ? "c0" : "c1"}:${roundedSizes.join("/")}`;
      if (layoutKey !== lastLayoutRef.current) {
        let nextPanelSizes: Partial<typeof panelSizesRef.current> | null = null;
        if (!streamCollapsed && !contextCollapsed && roundedSizes.length === 3) {
          const [stream, workspace, context] = roundedSizes;
          nextPanelSizes = { stream, workspace, context };
        } else if (!streamCollapsed && contextCollapsed && roundedSizes.length === 2) {
          const [stream, workspace] = roundedSizes;
          nextPanelSizes = { stream, workspace };
        } else if (streamCollapsed && !contextCollapsed && roundedSizes.length === 2) {
          const [workspace, context] = roundedSizes;
          nextPanelSizes = { workspace, context };
        } else if (streamCollapsed && contextCollapsed && roundedSizes.length === 1) {
          const [workspace] = roundedSizes;
          nextPanelSizes = { workspace };
        }

        if (nextPanelSizes) {
          const current = panelSizesRef.current;
          const changed = Object.entries(nextPanelSizes).some(
            ([key, value]) => Math.abs(current[key as keyof typeof current] - (value ?? 0)) > 0.01,
          );
          if (changed) {
            lastLayoutRef.current = layoutKey;
            setPanelSizes(nextPanelSizes);
          }
        }
      }

      let nextPixelWidths: { stream: number; workspace: number; context: number; total: number } | null = null;
      if (!streamCollapsed && !contextCollapsed && sizes.length === 3) {
        const [streamPct, workspacePct, contextPct] = sizes;
        nextPixelWidths = {
          stream: (streamPct / 100) * cw,
          workspace: (workspacePct / 100) * cw,
          context: (contextPct / 100) * cw,
          total: cw,
        };
      } else if (!streamCollapsed && contextCollapsed && sizes.length === 2) {
        const [streamPct, workspacePct] = sizes;
        nextPixelWidths = {
          stream: (streamPct / 100) * cw,
          workspace: (workspacePct / 100) * cw,
          context: 0,
          total: cw,
        };
      } else if (streamCollapsed && !contextCollapsed && sizes.length === 2) {
        const groupWidth = cw - RAIL_WIDTH;
        const [workspacePct, contextPct] = sizes;
        nextPixelWidths = {
          stream: RAIL_WIDTH,
          workspace: (workspacePct / 100) * groupWidth,
          context: (contextPct / 100) * groupWidth,
          total: cw,
        };
      } else if (streamCollapsed && contextCollapsed && sizes.length === 1) {
        const groupWidth = cw - RAIL_WIDTH;
        const [workspacePct] = sizes;
        nextPixelWidths = {
          stream: RAIL_WIDTH,
          workspace: (workspacePct / 100) * groupWidth,
          context: 0,
          total: cw,
        };
      }

      if (nextPixelWidths) {
        const roundedPixels = {
          stream: Math.round(nextPixelWidths.stream),
          workspace: Math.round(nextPixelWidths.workspace),
          context: Math.round(nextPixelWidths.context),
          total: Math.round(nextPixelWidths.total),
        };
        const pixelsKey = `${roundedPixels.stream}/${roundedPixels.workspace}/${roundedPixels.context}/${roundedPixels.total}`;
        if (pixelsKey !== lastPixelWidthsRef.current) {
          lastPixelWidthsRef.current = pixelsKey;
          setPixelWidths(roundedPixels);
        }
      }
    },
    [containerWidth, streamCollapsed, contextCollapsed, setPanelSizes, setPixelWidths],
  );

  const prevStreamCollapsed = useRef(streamCollapsed);
  useEffect(() => {
    if (!prevStreamCollapsed.current && streamCollapsed) {
      setExpandedSizes({ ...panelSizesRef.current });
    }
    prevStreamCollapsed.current = streamCollapsed;
  }, [streamCollapsed, setExpandedSizes]);

  const defaults = (() => {
    if (!streamCollapsed && !contextCollapsed) {
      const es = expandedSizes;
      return {
        stream: es?.stream ?? panelSizes.stream,
        workspace: es?.workspace ?? panelSizes.workspace,
        context: es?.context ?? panelSizes.context,
      };
    }
    if (!streamCollapsed && contextCollapsed) {
      const total = panelSizes.stream + panelSizes.workspace;
      return {
        stream: (panelSizes.stream / total) * 100,
        workspace: (panelSizes.workspace / total) * 100,
        context: 0,
      };
    }
    if (streamCollapsed && !contextCollapsed) {
      const ws = 35;
      const ctx = 55;
      const sum = ws + ctx;
      return { stream: 0, workspace: (ws / sum) * 100, context: (ctx / sum) * 100 };
    }
    return { stream: 0, workspace: 100, context: 0 };
  })();

  const groupKey = `${streamCollapsed ? "s0" : "s1"}-${contextCollapsed ? "c0" : "c1"}`;
  const connectionState = useRealtimeStore((s) => s.connectionState);
  const isCoreMode = (import.meta.env.VITE_REALTIME_MODE as string | undefined)?.toLowerCase() === "core";
  const showDisconnectedBanner = isCoreMode && connectionState !== "connected";

  return (
    <>
      {showDisconnectedBanner && (
        <div className="border-b border-amber-300/60 bg-amber-50 px-3 py-1.5 text-xs text-amber-900 dark:border-amber-800/60 dark:bg-amber-950/30 dark:text-amber-200">
          Live Chat is <strong className="font-semibold">{connectionState}</strong> — check the backend URL
          (<code>VITE_API_URL</code>) and that you are signed in (<code>vf_access_token</code>).
        </div>
      )}
      <div ref={mainRef} className="flex min-h-0 flex-1">
        {streamCollapsed && (
          <div className="w-16 shrink-0 border-e border-app-border bg-card">
            <RoomStream />
          </div>
        )}
        <PanelGroup
          key={groupKey}
          ref={groupRef}
          direction="horizontal"
          className="flex-1"
          onLayout={handleLayout}
        >
          {!streamCollapsed && (
            <>
              <Panel
                defaultSize={defaults.stream}
                minSize={18}
                maxSize={36}
              >
                <RoomStream />
              </Panel>
              <PanelResizeHandle className="w-px bg-app-border hover:bg-primary/40" />
            </>
          )}

          <Panel
            defaultSize={defaults.workspace}
            minSize={28}
          >
            <div className="flex h-full min-h-0 flex-col">
              {conv && customer ? (
                <>
                  <CustomerJourneyStrip
                    customerId={customer.id}
                    onOpenFull={() => useLayoutStore.getState().setActiveContextTab("history")}
                  />
                  <div className="min-h-0 flex-1">
                    {openRooms.length > 0 && (
                      <div className="flex min-h-[3rem] items-center gap-1 overflow-x-auto border-b border-app-border bg-card px-2 py-1.5">
                        {openRooms.map((item) => {
                          const customerName = customerById[item.customerId]?.name ?? "Unknown";
                          const selected = item.id === selectedId;
                          return (
                            <div
                              key={item.id}
                              className={cn(
                                "flex min-w-[11rem] max-w-[16rem] items-center gap-2 rounded-md border px-2 py-1.5 text-xs transition-colors",
                                selected
                                  ? "border-primary/40 bg-primary/10 text-foreground"
                                  : "border-app-border bg-background text-muted-foreground hover:bg-muted/60",
                                item.status === "offered" && !selected && "border-amber-500/30 bg-amber-500/10",
                              )}
                            >
                              <button
                                type="button"
                                className="flex min-w-0 flex-1 items-center gap-1.5 text-start"
                                onClick={() => openTab(item.id)}
                              >
                                <ChannelIcon channel={item.channel} size={12} withBackground={false} />
                                <span className="truncate font-medium">{customerName}</span>
                                <span className="shrink-0 rounded bg-muted px-1 py-0.5 text-[10px] capitalize text-muted-foreground">
                                  {item.status.replace(/_/g, " ")}
                                </span>
                              </button>
                              <button
                                type="button"
                                className="shrink-0 rounded p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground"
                                onClick={() => closeTab(item.id)}
                                aria-label={`Close ${customerName}`}
                              >
                                <X className="h-3.5 w-3.5" />
                              </button>
                            </div>
                          );
                        })}
                      </div>
                    )}
                    <MessageTimeline room={conv} />
                  </div>
                  {conv.status === "resolved" ? (
                    <div className="flex items-center justify-between border-t border-app-border bg-emerald-50 px-3 py-2 text-xs text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-200">
                      <span>Room resolved — composer disabled.</span>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => useRoomStore.getState().setStatus(conv.id, "active")}
                      >
                        Reopen
                      </Button>
                    </div>
                  ) : (
                    <MessageComposer room={conv} onResolve={() => setWrapOpen(true)} />
                  )}
                </>
              ) : (
                <EmptyState
                  title="Select a room"
                  description="Pick one from the stream to start replying. Offered rooms show an Accept/Reject prompt."
                />
              )}
            </div>
          </Panel>

          {!contextCollapsed && (
            <>
              <PanelResizeHandle className="w-px bg-app-border hover:bg-primary/40" />
              <Panel
                defaultSize={defaults.context || panelSizes.context}
                minSize={20}
                maxSize={streamCollapsed ? 80 : 50}
              >
                {conv ? (
                  <div className="flex h-full flex-col border-s border-app-border bg-card">
                    <RoomHeader
                      room={conv}
                      onResolve={() => setWrapOpen(true)}
                      onTransfer={() => setAssignOpen({ open: true, mode: "transfer" })}
                      onAssign={() => setAssignOpen({ open: true, mode: "assign" })}
                      onCollapse={() => setContextCollapsed(true)}
                    />
                    <div className="min-h-0 flex-1 overflow-hidden">
                      <CustomerContextPanel room={conv} />
                    </div>
                  </div>
                ) : (
                  <div className="flex h-full border-s border-app-border bg-card">
                    <EmptyState title="No context" description="Select a room to view customer details." />
                  </div>
                )}
              </Panel>
            </>
          )}
          {contextCollapsed && conv && (
            <button
              onClick={() => setContextCollapsed(false)}
              className="border-s border-app-border bg-card px-1 text-[10px] uppercase tracking-wider text-muted-foreground"
            >
              Context
            </button>
          )}
        </PanelGroup>
      </div>

      {conv && (
        <>
          <AssignmentDialog
            open={assignOpen.open}
            onOpenChange={(v) => setAssignOpen((s) => ({ ...s, open: v }))}
            room={conv}
            mode={assignOpen.mode}
          />
          <WrapUpDialog open={wrapOpen} onOpenChange={setWrapOpen} room={conv} />
        </>
      )}
      <MockDevPanel />
    </>
  );
}
