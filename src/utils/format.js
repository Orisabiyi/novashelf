const relative = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
const absolute = new Intl.DateTimeFormat("en", { month: "short", day: "numeric", year: "numeric" });

/**
 * "5 min ago", "3 hours ago", "yesterday", "4 days ago", then "Oct 6, 2026".
 *
 * @param {string | null | undefined} iso
 * @param {Date} [now]
 */
export function formatRelativeDate(iso, now = new Date()) {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";

  const minutes = Math.round((date.getTime() - now.getTime()) / 60_000);
  if (minutes > -1) return "just now";
  if (minutes > -60) return relative.format(minutes, "minute");

  const hours = Math.round(minutes / 60);
  if (hours > -24) return relative.format(hours, "hour");

  const days = Math.round(hours / 24);
  if (days > -7) return relative.format(days, "day");

  return absolute.format(date);
}
