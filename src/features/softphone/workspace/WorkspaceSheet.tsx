import { useTranslation } from "react-i18next";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { SettingsView } from "../views/SettingsView";
import { VoicemailView } from "../views/VoicemailView";
import { MessagesView } from "../views/EmptyView";

export type SheetKey = "settings" | "voicemail" | "messages" | null;

interface Props {
  open: SheetKey;
  onClose: () => void;
  theme: "light" | "dark";
}

export function WorkspaceSheet({ open, onClose, theme }: Props) {
  const { t } = useTranslation();
  const titles: Record<Exclude<SheetKey, null>, string> = {
    settings: t("softphone.nav.settings"),
    voicemail: t("softphone.nav.voicemail"),
    messages: t("softphone.nav.messages"),
  };
  return (
    <Dialog open={!!open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent
        className={`softphone-${theme} max-w-2xl p-0 overflow-hidden`}
        style={{
          background: `hsl(var(--sp-window))`,
          borderColor: `hsl(var(--sp-border))`,
          color: `hsl(var(--sp-text))`,
        }}
      >
        <DialogHeader className="px-5 pt-5">
          <DialogTitle style={{ color: `hsl(var(--sp-text))` }}>
            {open ? titles[open] : ""}
          </DialogTitle>
        </DialogHeader>
        <div className="max-h-[70vh] overflow-auto flex flex-col">
          {open === "settings" && <SettingsView />}
          {open === "voicemail" && <VoicemailView />}
          {open === "messages" && <MessagesView />}
        </div>
      </DialogContent>
    </Dialog>
  );
}
