<script setup>
import Navbar from "@/components/shared/Navbar.vue";
import { RouterView } from "vue-router";
</script>

<template>
  <div class="min-h-svh flex flex-col">
    <Navbar />
    <main class="flex-1 flex flex-col">
      <RouterView />
    </main>
  </div>
</template>
