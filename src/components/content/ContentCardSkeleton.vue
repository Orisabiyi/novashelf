<script setup>
defineProps({
  compact: {
    type: Boolean,
    default: false,
  },
});
</script>

<template>
  <div
    class="bg-surface shadow-xl rounded-sm overflow-hidden animate-pulse"
    aria-hidden="true">
    <div class="aspect-video bg-subtle"></div>
    <div class="p-4 space-y-3">
      <div class="h-3 w-1/3 bg-subtle rounded"></div>
      <div class="h-5 w-full bg-subtle rounded"></div>
      <div class="h-5 w-2/3 bg-subtle rounded"></div>
      <template v-if="!compact">
        <div class="h-3 w-full bg-subtle/60 rounded"></div>
        <div class="h-3 w-5/6 bg-subtle/60 rounded"></div>
      </template>
    </div>
  </div>
</template>
