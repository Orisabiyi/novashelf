<script setup>
import { nextTick, reactive, ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import logoImg from "@/assets/novashelf-logo.svg";
import TextField from "@/components/auth/TextField.vue";
import { useAuth } from "@/composables/useAuth";
import { emailSchema, fieldErrors, signUpSchema } from "@/validation/auth";

const route = useRoute();
const router = useRouter();
const { register } = useAuth();

const step = ref(1);
const form = reactive({
  email: "",
  firstName: "",
  password: "",
  confirmPassword: "",
  ageConfirmed: false,
});
const errors = ref({});
const formError = ref("");
const emailTaken = ref(false);
const submitting = ref(false);

async function continueToDetails() {
  const result = emailSchema.safeParse({ email: form.email });
  errors.value = result.success ? {} : fieldErrors(result.error);
  if (!result.success) return;

  form.email = result.data.email;
  step.value = 2;
  await nextTick();
  document.getElementById("signup-first-name")?.focus();
}

function changeEmail() {
  step.value = 1;
  formError.value = "";
  emailTaken.value = false;
  nextTick(() => document.getElementById("signup-email")?.focus());
}

async function createAccount() {
  formError.value = "";
  emailTaken.value = false;

  const result = signUpSchema.safeParse(form);
  errors.value = result.success ? {} : fieldErrors(result.error);
  if (!result.success) return;

  submitting.value = true;
  try {
    const { email, firstName, password, ageConfirmed } = result.data;
    await register({ email, firstName: firstName || undefined, password, ageConfirmed });
    const next = typeof route.query.redirect === "string" && route.query.redirect.startsWith("/")
      ? route.query.redirect
      : "/explore";
    router.replace(next);
  } catch (error) {
    const serverErrors = Object.fromEntries(
      Object.entries(error.fieldErrors ?? {}).map(([field, messages]) => [field, messages[0]]),
    );
    errors.value = serverErrors;
    emailTaken.value = error.status === 409;
    if (error.status === 409 || Object.keys(serverErrors).length === 0) formError.value = error.message;
    if (serverErrors.email) step.value = 1;
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <div class="w-full max-w-md rounded-3xl border border-line-strong bg-surface p-6 sm:p-8">
    <div class="flex flex-col items-center gap-3 text-center">
      <img :src="logoImg" alt="" class="w-16 h-auto" />
      <h1 class="font-display text-2xl font-bold">Join Novashelf</h1>
      <p class="text-muted">
        Free, and it takes a minute. Keep track of what you've read and follow the topics you
        love.
      </p>
    </div>

    <form
      v-if="step === 1"
      class="mt-8 flex flex-col gap-5"
      novalidate
      @submit.prevent="continueToDetails">
      <TextField
        id="signup-email"
        v-model="form.email"
        label="Email"
        type="email"
        autocomplete="email"
        :error="errors.email" />
      <button type="submit" class="rounded-full bg-action text-on-action py-3 font-medium">
        Continue
      </button>
    </form>

    <form v-else class="mt-8 flex flex-col gap-5" novalidate @submit.prevent="createAccount">
      <div class="flex items-center justify-between gap-3 rounded-xl bg-page px-4 py-3 text-sm">
        <span class="truncate"><span class="text-muted">Email:</span> {{ form.email }}</span>
        <button type="button" class="shrink-0 font-medium text-link" @click="changeEmail">Change</button>
      </div>

      <TextField
        id="signup-first-name"
        v-model="form.firstName"
        label="First name"
        autocomplete="given-name"
        hint="Optional. We'll use it to say hello."
        :error="errors.firstName" />
      <TextField
        id="signup-password"
        v-model="form.password"
        label="Password"
        type="password"
        autocomplete="new-password"
        hint="At least 8 characters. A short phrase works well."
        :error="errors.password" />
      <TextField
        id="signup-confirm"
        v-model="form.confirmPassword"
        label="Repeat password"
        type="password"
        autocomplete="new-password"
        :error="errors.confirmPassword" />

      <div class="flex flex-col gap-1.5">
        <label class="flex items-start gap-3 text-sm">
          <input
            id="signup-age"
            v-model="form.ageConfirmed"
            type="checkbox"
            :aria-invalid="errors.ageConfirmed ? 'true' : undefined"
            class="mt-0.5 size-4 shrink-0 accent-action" />
          <span>I'm 13 or older.</span>
        </label>
        <p v-if="errors.ageConfirmed" class="text-sm text-link" role="alert">{{ errors.ageConfirmed }}</p>
      </div>

      <p v-if="formError" class="rounded-xl bg-page px-4 py-3 text-sm" role="alert">
        {{ formError }}
        <RouterLink
          v-if="emailTaken"
          :to="{ path: '/login', query: { email: form.email, redirect: route.query.redirect } }"
          class="font-medium text-link underline underline-offset-2">
          Sign in
        </RouterLink>
      </p>

      <button
        type="submit"
        class="rounded-full bg-action text-on-action py-3 font-medium disabled:opacity-60"
        :disabled="submitting">
        {{ submitting ? "Creating your account…" : "Create account" }}
      </button>
    </form>

    <p class="mt-6 text-center text-sm text-muted">
      Already have an account?
      <RouterLink
        :to="{ path: '/login', query: { redirect: route.query.redirect } }"
        class="font-medium text-link">
        Sign in
      </RouterLink>
    </p>
  </div>
</template>
