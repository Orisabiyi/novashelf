<script setup>
const features = [
  {
    icon: "pi pi-history",
    text: "See what you've read and pick up where you left off.",
  },
  {
    icon: "pi pi-compass",
    text: "Follow the topics you love, from Mars to the Moon, for a feed made for you.",
  },
  {
    icon: "pi pi-book",
    text: "New to space? Learn the basics with short, beginner-friendly lessons.",
  },
  {
    icon: "pi pi-sliders-h",
    text: "Your reading settings follow you to every device.",
  },
  {
    icon: "pi pi-lock-open",
    text: "Free and open source. Built by a curious mind, for curious minds.",
  },
];
</script>

<template>
  <div class="w-full max-w-md flex flex-col gap-8">
    <h2 class="font-display text-3xl font-bold text-balance">
      Why create an account?
    </h2>

    <ul class="space-y-6">
      <li v-for="feature in features" :key="feature.text" class="flex items-start gap-4 text-lg">
        <span
          class="grid size-10 shrink-0 place-items-center rounded-xl bg-brand/15 text-brand"
          aria-hidden="true">
          <i :class="feature.icon"></i>
        </span>
        <span class="pt-1.5">{{ feature.text }}</span>
      </li>
    </ul>
  </div>
</template>
