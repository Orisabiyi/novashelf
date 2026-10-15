const LAST_VISIT_KEY = "novashelf:last-visit";
const SESSION_KEY = "novashelf:previous-visit";

// The visit before this browsing session, held in sessionStorage so a refresh doesn't reset
// "new since your last visit" to zero.
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
