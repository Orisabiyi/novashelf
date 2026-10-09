<script setup>
import { computed, reactive, ref, watch } from "vue";
import { RouterLink, useRouter } from "vue-router";
import { clearHistory } from "@/api/me";
import { changePassword } from "@/api/auth";
import TextField from "@/components/auth/TextField.vue";
import { useAuth } from "@/composables/useAuth";
import { useFollowedTopics } from "@/composables/useFollowedTopics";
import { forgetAllReads } from "@/composables/useReadState";
import { FONT_SIZES, THEMES, WIDTHS, useReaderPreferences } from "@/composables/useReaderPreferences";

const router = useRouter();
const { account, updateAccount, deleteAccount } = useAuth();
const preferences = useReaderPreferences();
const { followed } = useFollowedTopics();

/** First message for a field from an API validation error. */
const fieldError = (error, field) => error?.fieldErrors?.[field]?.[0] ?? "";

// Profile
const profile = reactive({ firstName: account.value?.firstName ?? "", error: "", saved: false, saving: false });
watch(account, (a) => a && !profile.saving && (profile.firstName = a.firstName ?? ""));
const nameChanged = computed(() => (profile.firstName.trim() || null) !== (account.value?.firstName ?? null));

async function saveProfile() {
  profile.error = "";
  profile.saved = false;
  profile.saving = true;
  try {
    await updateAccount({ firstName: profile.firstName.trim() || null });
    profile.saved = true;
  } catch (e) {
    profile.error = fieldError(e, "firstName") || e.message;
  } finally {
    profile.saving = false;
  }
}

// Reading settings (saved to the account automatically)
const READING_GROUPS = [
  { key: "fontSize", label: "Text size", options: FONT_SIZES },
  { key: "width", label: "Line width", options: WIDTHS },
  { key: "theme", label: "Theme", options: THEMES },
];

// Password
const password = reactive({ current: "", next: "", errors: {}, error: "", saved: false, saving: false });

async function savePassword() {
  password.errors = {};
  password.error = "";
  password.saved = false;
  if (!password.current) password.errors.current = "Enter your current password.";
  if (password.next.length < 8) password.errors.next = "Use at least 8 characters.";
  if (Object.keys(password.errors).length) return;

  password.saving = true;
  try {
    await changePassword({ currentPassword: password.current, newPassword: password.next });
    Object.assign(password, { current: "", next: "", saved: true });
  } catch (e) {
    password.errors = { current: fieldError(e, "currentPassword"), next: fieldError(e, "newPassword") };
    if (!password.errors.current && !password.errors.next) password.error = e.message;
  } finally {
    password.saving = false;
  }
}

// History
const history = reactive({ confirming: false, cleared: false, error: "" });

async function confirmClearHistory() {
  history.error = "";
  try {
    await clearHistory();
    forgetAllReads();
    Object.assign(history, { confirming: false, cleared: true });
  } catch (e) {
    history.error = e.message;
  }
}

// Delete account
const removal = reactive({ open: false, password: "", error: "", deleting: false });

async function confirmDelete() {
  removal.error = "";
  if (!removal.password) {
    removal.error = "Enter your password to confirm.";
    return;
  }
  removal.deleting = true;
  try {
    await deleteAccount(removal.password);
    router.replace({ path: "/", query: { deleted: "1" } });
  } catch (e) {
    removal.error = fieldError(e, "password") || e.message;
    removal.password = "";
  } finally {
    removal.deleting = false;
  }
}

const card = "rounded-3xl bg-surface p-6 sm:p-8 space-y-5";
const secondaryButton =
  "rounded-full border border-line-strong px-5 py-2.5 font-medium transition-colors hover:border-ink disabled:opacity-50";
const primaryButton = "rounded-full bg-action text-on-action px-5 py-2.5 font-medium disabled:opacity-50";
const optionClass = (selected) => [
  "rounded-full px-4 py-1.5 text-sm font-medium transition-colors",
  selected ? "bg-action text-on-action" : "bg-page text-muted hover:text-ink",
];
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48">
    <div class="mx-auto max-w-3xl space-y-6">
      <header class="space-y-2 pb-2">
        <p class="text-sm font-medium text-link">Your account</p>
        <h1 class="font-display text-4xl font-bold tracking-tight">Settings</h1>
        <p class="text-muted">Signed in as <span class="text-ink">{{ account?.email }}</span></p>
      </header>

      <!-- Profile -->
      <form :class="card" novalidate aria-labelledby="profile-heading" @submit.prevent="saveProfile">
        <div>
          <h2 id="profile-heading" class="font-display text-xl font-bold">Profile</h2>
          <p class="text-muted">What we call you around Novashelf.</p>
        </div>
        <TextField
          id="settings-first-name"
          v-model="profile.firstName"
          label="First name"
          autocomplete="given-name"
          hint="Optional. Without it we use the start of your email."
          :error="profile.error" />
        <div class="flex items-center gap-4">
          <button type="submit" :class="primaryButton" :disabled="profile.saving || !nameChanged">
            {{ profile.saving ? "Saving…" : "Save" }}
          </button>
          <p v-if="profile.saved && !nameChanged" class="flex items-center gap-1.5 text-sm text-muted" role="status">
            <i class="pi pi-check text-brand" style="font-size: 0.75rem" aria-hidden="true"></i>
            Saved
          </p>
        </div>
      </form>

      <!-- Reading -->
      <section :class="card" aria-labelledby="reading-heading">
        <div>
          <h2 id="reading-heading" class="font-display text-xl font-bold">Reading</h2>
          <p class="text-muted">Saved to your account, so they follow you to other devices.</p>
        </div>
        <div v-for="group in READING_GROUPS" :key="group.key" class="flex flex-wrap items-center justify-between gap-3">
          <span class="font-medium">{{ group.label }}</span>
          <div class="flex flex-wrap gap-2" role="group" :aria-label="group.label">
            <button
              v-for="option in group.options"
              :key="option.value"
              type="button"
              :class="optionClass(preferences[group.key] === option.value)"
              :aria-pressed="preferences[group.key] === option.value"
              @click="preferences[group.key] = option.value">
              {{ option.label }}
            </button>
          </div>
        </div>
        <p class="flex flex-wrap items-center justify-between gap-3 border-t border-line pt-5">
          <span>
            <span class="font-medium">Followed topics</span>
            <span class="block text-sm text-muted">
              {{ followed.length ? `You follow ${followed.length} ${followed.length === 1 ? "topic" : "topics"}.` : "You don't follow any topics yet." }}
            </span>
          </span>
          <RouterLink :to="{ path: '/explore', query: { topic: 'following' } }" :class="secondaryButton">
            {{ followed.length ? "Your Following feed" : "Pick topics" }}
          </RouterLink>
        </p>
      </section>

      <!-- Password -->
      <form :class="card" novalidate aria-labelledby="password-heading" @submit.prevent="savePassword">
        <div>
          <h2 id="password-heading" class="font-display text-xl font-bold">Password</h2>
          <p class="text-muted">You'll stay signed in here. Other devices will need the new password.</p>
        </div>
        <!-- Lets password managers match the change to this account. -->
        <input type="email" :value="account?.email" autocomplete="username" class="hidden" readonly tabindex="-1" aria-hidden="true" />
        <div class="grid gap-5 sm:grid-cols-2">
          <TextField
            id="settings-current-password"
            v-model="password.current"
            label="Current password"
            type="password"
            autocomplete="current-password"
            :error="password.errors.current" />
          <TextField
            id="settings-new-password"
            v-model="password.next"
            label="New password"
            type="password"
            autocomplete="new-password"
            hint="At least 8 characters."
            :error="password.errors.next" />
        </div>
        <p v-if="password.error" class="rounded-xl bg-page px-4 py-3 text-sm" role="alert">{{ password.error }}</p>
        <div class="flex items-center gap-4">
          <button type="submit" :class="primaryButton" :disabled="password.saving">
            {{ password.saving ? "Changing…" : "Change password" }}
          </button>
          <p v-if="password.saved" class="flex items-center gap-1.5 text-sm text-muted" role="status">
            <i class="pi pi-check text-brand" style="font-size: 0.75rem" aria-hidden="true"></i>
            Password changed
          </p>
        </div>
      </form>

      <!-- History -->
      <section :class="card" aria-labelledby="history-heading">
        <div>
          <h2 id="history-heading" class="font-display text-xl font-bold">Reading history</h2>
          <p class="text-muted">What you've opened, so you can find it again. Only you can see it.</p>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <RouterLink to="/history" :class="secondaryButton">View history</RouterLink>
          <template v-if="history.confirming">
            <span class="text-sm text-muted">Clear everything?</span>
            <button type="button" :class="primaryButton" @click="confirmClearHistory">Yes, clear it</button>
            <button type="button" class="px-3 py-2 text-sm font-medium text-muted hover:text-ink" @click="history.confirming = false">
              Cancel
            </button>
          </template>
          <button v-else type="button" :class="secondaryButton" @click="Object.assign(history, { confirming: true, cleared: false })">
            Clear history
          </button>
          <p v-if="history.cleared" class="flex items-center gap-1.5 text-sm text-muted" role="status">
            <i class="pi pi-check text-brand" style="font-size: 0.75rem" aria-hidden="true"></i>
            History cleared
          </p>
        </div>
        <p v-if="history.error" class="text-sm text-link" role="alert">{{ history.error }}</p>
      </section>

      <!-- Delete account -->
      <section class="rounded-3xl border-2 border-brand/50 p-6 sm:p-8 space-y-5" aria-labelledby="delete-heading">
        <div>
          <h2 id="delete-heading" class="font-display text-xl font-bold text-brand">Delete account</h2>
          <p class="text-muted">
            Removes your account, reading history, followed topics, reading settings and lesson progress.
            This can't be undone.
          </p>
        </div>
        <button
          v-if="!removal.open"
          type="button"
          class="rounded-full border-2 border-brand px-5 py-2.5 font-medium text-brand transition-colors hover:bg-action hover:text-on-action"
          @click="removal.open = true">
          Delete my account…
        </button>
        <form v-else class="space-y-4" novalidate @submit.prevent="confirmDelete">
          <input type="email" :value="account?.email" autocomplete="username" class="hidden" readonly tabindex="-1" aria-hidden="true" />
          <TextField
            id="settings-delete-password"
            v-model="removal.password"
            label="Enter your password to confirm"
            type="password"
            autocomplete="current-password"
            :error="removal.error" />
          <div class="flex flex-wrap items-center gap-3">
            <button type="submit" :class="primaryButton" :disabled="removal.deleting">
              {{ removal.deleting ? "Deleting…" : "Delete my account forever" }}
            </button>
            <button
              type="button"
              class="px-3 py-2 text-sm font-medium text-muted hover:text-ink"
              @click="Object.assign(removal, { open: false, password: '', error: '' })">
              Cancel
            </button>
          </div>
        </form>
      </section>
    </div>
  </section>
</template>
