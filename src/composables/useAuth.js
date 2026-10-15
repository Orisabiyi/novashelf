import { computed, reactive } from "vue";
import * as authApi from "@/api/auth";

const state = reactive({
  account: null,
  status: "unknown",
});

let sessionCheck = null;

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
    displayName: computed(() => state.account?.firstName || state.account?.email.split("@")[0] || ""),

    register: (details) => authApi.register(details).then(signedIn),
    signIn: (credentials) => authApi.login(credentials).then(signedIn),
    updateAccount: (details) =>
      authApi.updateAccount(details).then((account) => {
        state.account = account;
        return account;
      }),
    async deleteAccount(password) {
      await authApi.deleteAccount(password);
      state.account = null;
      state.status = "signedOut";
    },
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
