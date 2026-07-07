import { format, subDays, startOfDay, endOfDay, parse, isValid } from "date-fns";

export type TimePeriod = "today" | "last7days" | "last30days" | "custom";

export interface ResolvedRange {
  startDate: string; // DD/MM/YYYY
  endDate: string;   // DD/MM/YYYY
  from: Date;
  to: Date;
  timePeriod: TimePeriod;
}

const fmt = (d: Date) => format(d, "dd/MM/yyyy");

function parseDmy(s?: string): Date | null {
  if (!s) return null;
  const d = parse(s, "dd/MM/yyyy", new Date());
  return isValid(d) ? d : null;
}

export function computeDateRange(
  timePeriod: TimePeriod | string = "today",
  startDate?: string,
  endDate?: string,
): ResolvedRange {
  const now = new Date();
  let from: Date;
  let to: Date = endOfDay(now);
  let period: TimePeriod = (["today", "last7days", "last30days", "custom"].includes(timePeriod)
    ? timePeriod
    : "today") as TimePeriod;

  if (period === "custom") {
    const s = parseDmy(startDate);
    const e = parseDmy(endDate);
    if (s && e) {
      from = startOfDay(s);
      to = endOfDay(e);
    } else {
      // fallback to last 30 days
      period = "last30days";
      from = startOfDay(subDays(now, 29));
    }
  } else if (period === "last7days") {
    from = startOfDay(subDays(now, 6));
  } else if (period === "last30days") {
    from = startOfDay(subDays(now, 29));
  } else {
    from = startOfDay(now);
  }

  return { startDate: fmt(from), endDate: fmt(to), from, to, timePeriod: period };
}
