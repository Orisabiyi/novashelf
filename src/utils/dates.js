// Calendar-day helpers for the Picture of the Day, which is keyed by date (yyyy-MM-dd).
// Everything is UTC so a date never shifts with the reader's time zone.

export const APOD_FIRST_DATE = "1995-06-16";

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

export function isIsoDate(value) {
  return typeof value === "string" && ISO_DATE.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

export function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

export function addDays(iso, days) {
  const date = new Date(`${iso}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

export function randomDateBetween(first, latest) {
  const start = Date.parse(`${first}T00:00:00Z`);
  const end = Date.parse(`${latest}T00:00:00Z`);
  const day = 24 * 60 * 60 * 1000;
  const offset = Math.floor(Math.random() * (Math.round((end - start) / day) + 1));
  return new Date(start + offset * day).toISOString().slice(0, 10);
}

export function apodDate(id) {
  return typeof id === "string" && id.startsWith("apod-") ? id.slice(5) : null;
}

const fullDate = new Intl.DateTimeFormat("en", {
  weekday: "long",
  month: "long",
  day: "numeric",
  year: "numeric",
  timeZone: "UTC",
});
const shortDate = new Intl.DateTimeFormat("en", {
  month: "short",
  day: "numeric",
  year: "numeric",
  timeZone: "UTC",
});

export function formatFullDate(iso) {
  return isIsoDate(iso) ? fullDate.format(new Date(`${iso}T00:00:00Z`)) : "";
}

export function formatShortDate(iso) {
  return isIsoDate(iso) ? shortDate.format(new Date(`${iso}T00:00:00Z`)) : "";
}

export function itemLink(item) {
  return item.kind === "apod" ? { path: "/apod", query: { date: apodDate(item.id) } } : `/read/${item.id}`;
}
