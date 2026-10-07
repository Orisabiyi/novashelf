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
    class="bg-white shadow-xl rounded-sm overflow-hidden animate-pulse"
    aria-hidden="true">
    <div class="aspect-video bg-gray-200"></div>
    <div class="p-4 space-y-3">
      <div class="h-3 w-1/3 bg-gray-200 rounded"></div>
      <div class="h-5 w-full bg-gray-200 rounded"></div>
      <div class="h-5 w-2/3 bg-gray-200 rounded"></div>
      <template v-if="!compact">
        <div class="h-3 w-full bg-gray-100 rounded"></div>
        <div class="h-3 w-5/6 bg-gray-100 rounded"></div>
      </template>
    </div>
  </div>
</template>
