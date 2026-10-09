import { computed, reactive } from "vue";
import * as authApi from "@/api/auth";

// One shared sign-in state for the whole app. The session itself is an HttpOnly cookie the page
// can't read, so "who am I" comes from GET /api/me.
const state = reactive({
  /** @type {import("@/api/auth").Account | null} */
  account: null,
  /** "unknown" until the first check finishes, then "signedIn" / "signedOut" / "unavailable". */
  status: "unknown",
});

let sessionCheck = null;

/** Ask the API who's signed in (once; later calls share the same check). */
export function loadSession() {
  sessionCheck ??= authApi
    .getMe()
    .then((account) => {
      state.account = account;
      state.status = "signedIn";
    })
    .catch((error) => {
      state.account = null;
      // 503: accounts are switched off (no database). Anything else: treat as signed out.
      state.status = error.status === 503 ? "unavailable" : "signedOut";
    });
  return sessionCheck;
}

function signedIn(account) {
  state.account = account;
  state.status = "signedIn";
  sessionCheck = Promise.resolve();
  return account;
}

export function useAuth() {
  return {
    account: computed(() => state.account),
    status: computed(() => state.status),
    isSignedIn: computed(() => state.status === "signedIn"),
    /** What to call the reader: first name, else the part of the email before "@". */
    displayName: computed(() => state.account?.firstName || state.account?.email.split("@")[0] || ""),

    register: (details) => authApi.register(details).then(signedIn),
    signIn: (credentials) => authApi.login(credentials).then(signedIn),
    async signOut() {
      try {
        await authApi.logout();
      } finally {
        state.account = null;
        state.status = "signedOut";
      }
    },
  };
}
