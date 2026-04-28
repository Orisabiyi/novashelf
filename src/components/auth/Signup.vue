<script setup>
import { ref } from "vue";
import logoImg from "@/assets/novashelf-logo.svg";

const email = ref("");
</script>

<template>
  <div
    class="border-2 border-gray-300 rounded-xl h-[500px] p-8 2xl:w-1/3 flex flex-col items-center gap-4 text-center">
    <figure class="w-20 h-auto">
      <img :src="logoImg" alt="" class="w-full h-full inline-block" />
    </figure>

    <h3 class="text-2xl font-medium">Join the shelf</h3>

    <p class="w-4/5">
      Create an account to save articles, track what you've read, and explore
      space at your own pace.
    </p>

    <form
      class="flex flex-col items-stretch justify-stretch w-full mt-4 space-y-4">
      <input
        type="email"
        placeholder="Email"
        v-model="email"
        class="block border-2 border-gray-300 rounded-sm p-2 focus:outline-none" />
      <button class="block bg-base-red/80 text-gray-100 p-2 rounded-sm">
        Sign Up
      </button>
    </form>
  </div>
</template>
