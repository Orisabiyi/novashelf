<script setup>
import logoImg from "@/assets/novashelf-logo-ord.svg";

const navLinks = [
  {
    name: "Explore",
    path: "/explore",
  },
  {
    name: "About",
    path: "/about",
  },
  {
    name: "Sign In",
    path: "/login",
    type: "button",
  },
  {
    name: "Get Started",
    path: "/sign-up",
    type: "button",
  },
];
</script>

<template>
  <nav class="p-10 2xl:px-32 flex bg-gray-200/20">
    <a href="/" class="flex items-center gap-2 block">
      <figure class="w-15 h-auto flex items-center justify-center">
        <img
          :src="logoImg"
          alt="novashelf logo with no background"
          class="inline-block w-full h-full" />
      </figure>
      <h1 class="text-2xl font-medium">novashelf</h1>
    </a>

    <ul class="ml-auto flex items-center gap-10">
      <li
        v-for="navLink in navLinks"
        :key="navLink.path"
        :class="`${navLink.type === 'button' ? 'bg-[#D85357] px-4 py-1 font-medium rounded-md text-gray-100' : ''}`">
        <a :href="navLink.path">{{ navLink.name }}</a>
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
