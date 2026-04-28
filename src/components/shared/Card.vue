<script setup>
import shelfButtonIcon from "@/assets/shelf-button.svg";

defineProps({
  width: {
    type: String,
    default: "w-64",
  },
});
</script>

<template>
  <div :class="`bg-white ${width} space-y-4 p-4 shadow-xl`">
    <figure>
      <img
        src="https://images.unsplash.com/photo-1707057538379-d62783e77f1d?q=80&w=1235&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
        alt="" />
    </figure>

    <h3 class="text-2xl font-bold leading-tight">
      What Happens Inside a Black Hole
    </h3>

    <button class="flex items-center gap-1 font-medium">
      <figure class="w-8 h-auto">
        <img
          :src="shelfButtonIcon"
          alt="the button icon to indicate shelfing of articles"
          class="w-full h-full block" />
      </figure>
      shelf
    </button>
  </div>
</template>
