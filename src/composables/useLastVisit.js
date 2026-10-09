const LAST_VISIT_KEY = "novashelf:last-visit";
const SESSION_KEY = "novashelf:previous-visit";

/**
 * When the reader was last here, before this browsing session (null on a first visit or when
 * storage is blocked). Kept in this browser only. Reloads and page changes in the same session
 * keep the same answer, so "new since your last visit" doesn't reset to zero on refresh.
 *
 * @returns {Date | null}
 */
export function previousVisit() {
  try {
    let previous = sessionStorage.getItem(SESSION_KEY);
    if (previous === null) {
      previous = localStorage.getItem(LAST_VISIT_KEY) ?? "";
      sessionStorage.setItem(SESSION_KEY, previous);
    }
    localStorage.setItem(LAST_VISIT_KEY, new Date().toISOString());

    const date = previous ? new Date(previous) : null;
    return date && !Number.isNaN(date.getTime()) ? date : null;
  } catch {
    return null;
  }
}
