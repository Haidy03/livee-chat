import { X } from "lucide-react";
import { tagColor } from "./tagColors";

interface Props {
  tag: string;
  onRemove?: () => void;
}

export function TagChip({ tag, onRemove }: Props) {
  const c = tagColor(tag);
  return (
    <span
      className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium border"
      style={{
        background: `hsl(${c.bg})`,
        color: `hsl(${c.fg})`,
        borderColor: `hsl(${c.border})`,
      }}
    >
      {tag}
      {onRemove && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          className="hover:opacity-70"
          aria-label={`Remove ${tag}`}
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </span>
  );
}
