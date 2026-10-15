import { reactive, watch } from "vue";
import { recordRead } from "@/api/me";
import { loadSession, useAuth } from "@/composables/useAuth";

const { isSignedIn } = useAuth();

const readThisVisit = reactive(new Set());

watch(isSignedIn, (signedIn) => {
  if (!signedIn) readThisVisit.clear();
});

export function isRead(item) {
  return item?.read === true || readThisVisit.has(item?.id);
}

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
