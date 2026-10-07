<script setup>
import { RouterLink } from "vue-router";
</script>

<template>
  <section
    class="flex-1 flex flex-col items-center justify-center gap-4 p-10 text-center">
    <i class="pi pi-compass text-base-red" style="font-size: 3rem"></i>
    <h2 class="text-3xl font-bold">Lost in space</h2>
    <p class="text-lg text-gray-700 max-w-md">
      There's nothing at this address. It may have drifted off, or it hasn't
      launched yet.
    </p>
    <RouterLink
      to="/explore"
      class="bg-base-red px-4 py-1 rounded-md text-gray-100 text-lg"
      >Back to exploring</RouterLink
    >
  </section>
</template>
