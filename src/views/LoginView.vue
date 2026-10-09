<script setup>
import { reactive, ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import logoImg from "@/assets/novashelf-logo.svg";
import TextField from "@/components/auth/TextField.vue";
import { useAuth } from "@/composables/useAuth";
import { fieldErrors, signInSchema } from "@/validation/auth";

const route = useRoute();
const router = useRouter();
const { signIn } = useAuth();

const form = reactive({
  // Prefilled when coming from "this email already has an account" on sign-up.
  email: typeof route.query.email === "string" ? route.query.email : "",
  password: "",
  remember: true,
});
const errors = ref({});
const formError = ref("");
const submitting = ref(false);

async function submit() {
  formError.value = "";
  const result = signInSchema.safeParse(form);
  errors.value = result.success ? {} : fieldErrors(result.error);
  if (!result.success) return;

  submitting.value = true;
  try {
    await signIn({ ...result.data, remember: form.remember });
    const next = typeof route.query.redirect === "string" && route.query.redirect.startsWith("/")
      ? route.query.redirect
      : "/explore";
    router.replace(next);
  } catch (error) {
    formError.value = error.message;
    form.password = "";
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <section class="flex-1 flex items-center justify-center px-6 py-12">
    <div class="w-full max-w-md rounded-3xl border border-line-strong bg-surface p-6 sm:p-8">
      <div class="flex flex-col items-center gap-3 text-center">
        <img :src="logoImg" alt="" class="w-16 h-auto" />
        <h1 class="font-display text-2xl font-bold">Welcome back</h1>
        <p class="text-muted">Sign in to pick up where you left off.</p>
      </div>

      <form class="mt-8 flex flex-col gap-5" novalidate @submit.prevent="submit">
        <TextField
          id="login-email"
          v-model="form.email"
          label="Email"
          type="email"
          autocomplete="email"
          :error="errors.email" />
        <TextField
          id="login-password"
          v-model="form.password"
          label="Password"
          type="password"
          autocomplete="current-password"
          :error="errors.password" />

        <label class="flex items-center gap-3 text-sm">
          <input v-model="form.remember" type="checkbox" class="size-4 accent-action" />
          Keep me signed in on this device
        </label>

        <p v-if="formError" class="rounded-xl bg-page px-4 py-3 text-sm" role="alert">{{ formError }}</p>

        <button
          type="submit"
          class="rounded-full bg-action text-on-action py-3 font-medium disabled:opacity-60"
          :disabled="submitting">
          {{ submitting ? "Signing in…" : "Sign in" }}
        </button>
      </form>

      <p class="mt-6 text-center text-sm text-muted">
        New to Novashelf?
        <RouterLink
          :to="{ path: '/sign-up', query: { redirect: route.query.redirect } }"
          class="font-medium text-link">
          Create a free account
        </RouterLink>
      </p>
    </div>
  </section>
</template>
