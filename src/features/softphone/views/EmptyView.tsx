import { MessageSquare } from "lucide-react";
import { useTranslation } from "react-i18next";

export function MessagesView() {
  const { t } = useTranslation();
  return (
    <div className="flex-1 flex flex-col items-center justify-center text-center gap-4 p-8">
      <div className="h-24 w-24 rounded-full bg-primary/10 flex items-center justify-center">
        <MessageSquare className="h-12 w-12 text-primary" />
      </div>
      <div className="text-lg font-semibold">{t("softphone.empty.messagesTitle")}</div>
      <div className="text-sm text-muted-foreground max-w-sm">{t("softphone.empty.messagesDesc")}</div>
    </div>
  );
}
