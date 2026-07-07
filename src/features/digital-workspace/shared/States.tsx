import { type ReactNode } from "react";
import { Inbox } from "lucide-react";
import { cn } from "@/lib/utils";

export function EmptyState({
  title,
  description,
  icon: Icon = Inbox,
  action,
  className,
}: {
  title: string;
  description?: string;
  icon?: any;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex h-full w-full flex-col items-center justify-center gap-3 p-8 text-center", className)}>
      <div className="rounded-full bg-muted p-4 text-muted-foreground">
        <Icon className="h-6 w-6" aria-hidden />
      </div>
      <div className="space-y-1">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        {description ? <p className="text-xs text-muted-foreground max-w-sm">{description}</p> : null}
      </div>
      {action}
    </div>
  );
}

export function ErrorState({ title, description, retry }: { title: string; description?: string; retry?: () => void }) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center gap-3 p-8 text-center">
      <div className="rounded-full bg-destructive/10 p-4 text-destructive">!</div>
      <h3 className="text-sm font-semibold">{title}</h3>
      {description ? <p className="text-xs text-muted-foreground">{description}</p> : null}
      {retry ? (
        <button onClick={retry} className="text-xs underline text-primary">
          Retry
        </button>
      ) : null}
    </div>
  );
}

export function LoadingSkeleton({ lines = 4 }: { lines?: number }) {
  return (
    <div className="space-y-2 p-3">
      {Array.from({ length: lines }).map((_, i) => (
        <div key={i} className="h-3 w-full animate-pulse rounded bg-muted" />
      ))}
    </div>
  );
}

export function PermissionGate({ when, children }: { when: boolean; children: ReactNode }) {
  if (!when) return null;
  return <>{children}</>;
}
