<script setup>
const features = [
  {
    icon: "pi pi-compass",
    text: "Explore daily space content from NASA, straight to your feed.",
  },
  {
    icon: "pi pi-bookmark-fill",
    text: "Save articles and images to your personal reading list.",
  },
  {
    icon: "pi pi-lightbulb",
    text: "Every article comes with a 'Simply Put' space explained like you're talking to a friend",
  },
  {
    icon: "pi pi-lock-open",
    text: "Built by a curious mind, for curious minds. Open source and community-driven.",
  },
];
</script>

<template>
  <div class="w-1/4 flex flex-col items-center gap-10">
    <h2 class="text-3xl text-center font-medium">
      <span>Get Started</span>
      <span class="block">with NovaShelf</span>
    </h2>

    <ul class="space-y-10">
      <li v-for="feature in features" class="flex items-center gap-2 text-lg">
        <figure>
          <i
            :class="`${feature.icon} text-base-red`"
            style="font-size: 2rem"></i>
        </figure>

        <span>{{ feature.text }}</span>
      </li>
    </ul>
  </div>
</template>
