import { useState } from "react";
import { LeftColumn } from "./LeftColumn";
import { CenterColumn } from "./CenterColumn";
import { RightColumn } from "./RightColumn";
import { ENGAGE_ITEMS } from "./data";

export function SocialInbox() {
  const [selectedId, setSelectedId] = useState<string>("jenna");
  const [unread, setUnread] = useState<Set<string>>(
    () => new Set(ENGAGE_ITEMS.filter((i) => i.unread).map((i) => i.id)),
  );

  const select = (id: string) => {
    setSelectedId(id);
    setUnread((prev) => {
      if (!prev.has(id)) return prev;
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  };

  return (
    <div
      className="social-inbox flex h-full min-h-0 w-full overflow-hidden"
      style={{ background: "var(--si-bg)" }}
    >
      <LeftColumn selectedId={selectedId} onSelect={select} unread={unread} />
      <CenterColumn />
      <RightColumn />
    </div>
  );
}
