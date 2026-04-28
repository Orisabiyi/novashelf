<script setup>
import logoImg from "@/assets/novashelf-logo-ord.svg";
import Navbar from "../shared/Navbar.vue";
import Card from "../shared/Card.vue";
</script>

<template>
  <Navbar className="fixed top-0 left-0 w-full" />

  <header class="h-screen flex items-center p-10 2xl:py-5 2xl:px-48">
    <div class="w-2/5 flex flex-col items-start gap-5">
      <div class="flex items-center gap-2">
        <figure class="w-12 h-auto flex items-center justify-center">
          <img
            :src="logoImg"
            alt="novashelf logo with no background"
            class="inline-block w-full h-full" />
        </figure>
        <h3 class="font-medium">Powered by NASA</h3>
      </div>

      <h2 class="text-6xl text-base-red font-bold flex flex-col">
        Space made simple
      </h2>

      <p class="text-xl w-5/6">
        The universe is fascinating you shouldn't need a PhD to enjoy it.
        Explore NASA's best content, simplified for the curious.
      </p>
    </div>

    <div class="flex-1 h-full overflow-hidden flex items-center gap-4">
      <div class="space-y-4">
        <Card />
        <Card />
        <Card />
      </div>

      <div class="space-y-4 translate-y-5">
        <Card />
        <Card />
      </div>

      <div class="space-y-4">
        <Card />
        <Card />
        <Card />
      </div>
    </div>
  </header>
</template>

<style scoped>
@import url("https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300..700&display=swap");

h3 {
  font-family: "Space Grotesk", sans-serif;
}
</style>
