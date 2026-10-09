import { reactive } from "vue";
import { recordRead } from "@/api/me";
import { loadSession, useAuth } from "@/composables/useAuth";

const { isSignedIn } = useAuth();

// Items read during this visit, so cards show "Read" straight away without refetching feeds.
const readThisVisit = reactive(new Set());

/** Whether the reader has opened this item (from the API's flag or during this visit). */
export function isRead(item) {
  return item?.read === true || readThisVisit.has(item?.id);
}

/**
 * Record a read once per visit, for signed-in readers only. Waits for the session check, which
 * may still be running when a page opened directly finishes loading. Failures are ignored:
 * history is a convenience.
 */
export async function markRead(id) {
  if (!id || readThisVisit.has(id)) return;
  await loadSession();
  if (!isSignedIn.value || readThisVisit.has(id)) return;
  readThisVisit.add(id);
  recordRead(id).catch(() => readThisVisit.delete(id));
}

export function forgetRead(id) {
  readThisVisit.delete(id);
}

export function forgetAllReads() {
  readThisVisit.clear();
}
