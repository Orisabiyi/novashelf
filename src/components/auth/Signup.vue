<script setup>
import { onBeforeUnmount, ref } from "vue";
import logoImg from "@/assets/novashelf-logo.svg";
import { authSchema, emailSchema } from "@/validation/auth";

function showError(msg) {
  error.value = msg;
  setTimeout(() => (error.value = ""), 2000);
}

const age = ref("");
const email = ref("");
const password = ref("");
const confirmPassword = ref("");

const error = ref("");

const step = ref(1);

function handleSubmit() {
  const payload = {
    age: Number(age.value),
    email: email.value,
    password: password.value,
    confirmPassword: confirmPassword.value,
  };

  const emailResult = emailSchema.safeParse({ email: email.value });

  if (!emailResult.success)
    return showError(
      `${(emailResult.error.issues[0].path[0], emailResult.error.issues[0].message)}`,
    );

  if (step.value !== 2) return (step.value += 1);

  const result = authSchema.safeParse(payload);
  if (!result.success)
    return showError(
      `${(result.error.issues[0].path[0], result.error.issues[0].message)}`,
    );
}
</script>

<template>
  <div
    class="border-2 border-gray-300 rounded-xl h-[500px] p-8 w-1/3 flex flex-col items-center gap-4 text-center">
    <figure class="w-20 h-auto">
      <img :src="logoImg" alt="" class="w-full h-full inline-block" />
    </figure>

    <h3 class="text-2xl font-medium">Join the shelf</h3>

    <p class="w-4/5">
      Create an account to save articles, track what you've read, and explore
      space at your own pace.
    </p>

    <form
      @submit.prevent="handleSubmit"
      :class="`flex flex-col items-stretch justify-stretch w-full mt-4 ${error ? 'space-y-2' : 'space-y-4'}`">
      <input
        type="email"
        placeholder="Email"
        v-model="email"
        :class="`border-2 border-gray-300 rounded-sm p-2 focus:outline-none ${step === 1 ? 'block' : 'hidden'}`" />

      <input
        type="password"
        placeholder="Password"
        v-model="password"
        :class="`border-2 border-gray-300 rounded-sm p-2 focus:outline-none ${step === 2 ? 'block' : 'hidden'}`" />

      <input
        type="password"
        placeholder="Repeat Password"
        v-model="confirmPassword"
        :class="`border-2 border-gray-300 rounded-sm p-2 focus:outline-none ${step === 2 ? 'block' : 'hidden'}`" />

      <input
        type="number"
        placeholder="How old are you?"
        v-model="age"
        :class="`border-2 border-gray-300 rounded-sm p-2 focus:outline-none ${step === 2 ? 'block' : 'hidden'}`" />

      <p
        :class="`${error ? 'block' : 'hidden'} text-base-red text-left text-sm`">
        {{ error }}
      </p>

      <button
        class="block bg-base-red/80 hover:bg-base-red text-gray-100 p-2 rounded-sm">
        {{ step === 1 ? "Sign Up" : "Create an account" }}
      </button>
    </form>
  </div>
</template>
