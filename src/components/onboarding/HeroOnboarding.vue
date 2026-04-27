<script setup>
import logoImg from "@/assets/novashelf-logo-ord.svg";
import Navbar from "../shared/Navbar.vue";
</script>

<template>
  <Navbar />

  <header class="h-screen flex items-center p-10 p-10 2xl:py-5 2xl:px-48">
    <div class="w-1/2 flex flex-col items-start gap-5">
      <div class="flex items-center gap-2">
        <figure class="w-12 h-auto flex items-center justify-center">
          <img
            :src="logoImg"
            alt="novashelf logo with no background"
            class="inline-block w-full h-full" />
        </figure>
        <h3 class="font-medium">Powered by NASA</h3>
      </div>

      <h2 class="text-7xl text-base-red font-bold flex flex-col">
        Space made simple
      </h2>

      <p class="text-xl w-2/3">
        The universe is fascinating you shouldn't need a PhD to enjoy it.
        Explore NASA's best content, simplified for the curious.
      </p>
    </div>

    <div class="w-1/2 h-full"></div>
  </header>
</template>

<style scoped>
@import url("https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300..700&display=swap");

h3 {
  font-family: "Space Grotesk", sans-serif;
}
</style>
