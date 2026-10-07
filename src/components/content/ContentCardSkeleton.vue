<script setup>
defineProps({
  /** Mirrors ContentCard's variants so the layout doesn't jump when content arrives. */
  variant: {
    type: String,
    default: "stacked",
  },
});
</script>

<template>
  <div
    :class="[
      'h-full flex flex-col gap-4 bg-surface rounded-2xl p-3 animate-pulse',
      variant === 'feature' && 'lg:col-span-2 lg:grid lg:grid-cols-2 lg:gap-6',
    ]"
    aria-hidden="true">
    <div
      :class="[
        'rounded-xl bg-subtle aspect-video',
        variant === 'feature' && 'lg:aspect-auto lg:h-full lg:min-h-72',
      ]"></div>
    <div class="flex flex-col gap-3 px-2 pb-2 lg:justify-center">
      <div class="h-5 w-24 bg-subtle rounded-full"></div>
      <div class="h-5 w-full bg-subtle rounded"></div>
      <div class="h-5 w-2/3 bg-subtle rounded"></div>
      <template v-if="variant !== 'compact'">
        <div class="h-3 w-full bg-subtle/60 rounded"></div>
        <div class="h-3 w-5/6 bg-subtle/60 rounded"></div>
        <div class="h-6 w-20 bg-subtle/60 rounded-full mt-2"></div>
      </template>
    </div>
  </div>
</template>
