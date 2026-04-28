<script setup>
const features = [
  {
    icon: "",
    text: "Explore daily space content from NASA, straight to your feed.",
  },
  {
    icon: "",
    text: "Save articles and images to your personal reading list.",
  },
  {
    icon: "",
    text: "Every article comes with a 'Simply Put' — space explained like you're talking to a friend",
  },
  {
    icon: "",
    text: "Built by a curious mind, for curious minds. Open source and community-driven.",
  },
];
</script>

<template>
  <div class="w-1/2">
    <h2>Get Started with NovaShelf</h2>

    <ul>
      <li v-for="feature in features">
        <figure>
          <img :src="feature.icon" alt="icon for supporting text" />
        </figure>

        <span>{{ feature.text }}</span>
      </li>
    </ul>
  </div>
</template>
