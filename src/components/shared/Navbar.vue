<script setup>
import { ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import logoImg from "@/assets/novashelf-logo-ord.svg";

const navLinks = [
  {
    name: "Explore",
    path: "/explore",
  },
];

const route = useRoute();
const menuOpen = ref(false);

// Close the mobile menu after navigating.
watch(
  () => route.fullPath,
  () => (menuOpen.value = false),
);
</script>

<template>
  <nav class="sticky top-0 z-10 bg-nav text-on-nav px-6 py-4 lg:px-10 2xl:px-48">
    <div class="flex items-center">
      <RouterLink to="/" class="flex items-center gap-2">
        <figure class="w-14 lg:w-20 h-auto flex items-center justify-center">
          <img
            :src="logoImg"
            alt="novashelf logo with no background"
            class="inline-block w-full h-full" />
        </figure>
        <h1 class="text-2xl text-brand font-medium">novashelf</h1>
      </RouterLink>

      <ul class="hidden md:flex ml-auto items-center gap-10 2xl:text-base">
        <li v-for="navLink in navLinks" :key="navLink.path">
          <RouterLink
            :to="navLink.path"
            class="font-medium text-on-nav-muted hover:text-on-nav"
            active-class="!text-nav-link"
            >{{ navLink.name }}</RouterLink
          >
        </li>
        <li>
          <RouterLink
            to="/sign-up"
            class="bg-action px-4 py-1 font-medium rounded-md text-on-action"
            >Get Started</RouterLink
          >
        </li>
      </ul>

      <button
        type="button"
        class="md:hidden ml-auto p-2"
        :aria-expanded="menuOpen"
        aria-controls="mobile-nav"
        :aria-label="menuOpen ? 'Close menu' : 'Open menu'"
        @click="menuOpen = !menuOpen">
        <i
          :class="menuOpen ? 'pi pi-times' : 'pi pi-bars'"
          style="font-size: 1.25rem"></i>
      </button>
    </div>

    <ul
      v-if="menuOpen"
      id="mobile-nav"
      class="md:hidden flex flex-col gap-4 pt-4 text-lg">
      <li v-for="navLink in navLinks" :key="navLink.path">
        <RouterLink
          :to="navLink.path"
          class="font-medium text-on-nav-muted"
          active-class="!text-nav-link"
          >{{ navLink.name }}</RouterLink
        >
      </li>
      <li>
        <RouterLink
          to="/sign-up"
          class="inline-block bg-action px-4 py-1 font-medium rounded-md text-on-action"
          >Get Started</RouterLink
        >
      </li>
    </ul>
  </nav>
</template>

<style scoped>
@import url("https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300..700&display=swap");

h1 {
  font-family: "Space Grotesk", sans-serif;
}
</style>
