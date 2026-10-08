<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { apodDate, formatShortDate } from "@/utils/dates";
import { imageAt, imageSrcset } from "@/utils/images";

const props = defineProps({
  /** @type {import("@/api/content").ContentItem} */
  item: {
    type: Object,
    required: true,
  },
  selected: {
    type: Boolean,
    default: false,
  },
});

const imageFailed = ref(false);
const date = computed(() => apodDate(props.item.id));
const hasImage = computed(() => props.item.imageUrl && !imageFailed.value);
</script>

<template>
  <RouterLink
    :to="{ path: '/apod', query: { date } }"
    :aria-current="selected ? 'true' : undefined"
    :class="[
      'group relative block aspect-square overflow-hidden rounded-2xl bg-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-link',
      selected ? 'ring-2 ring-brand ring-offset-2 ring-offset-page' : '',
    ]">
    <img
      v-if="hasImage"
      :src="imageAt(item.imageUrl, 480)"
      :srcset="imageSrcset(item.imageUrl, [320, 480, 640])"
      sizes="(min-width: 1024px) 25vw, (min-width: 640px) 33vw, 50vw"
      alt=""
      loading="lazy"
      decoding="async"
      class="absolute inset-0 size-full object-cover transition-transform duration-500 group-hover:scale-105"
      @error="imageFailed = true" />
    <!-- Some early pictures aren't available any more: show the title instead. -->
    <div v-else class="absolute inset-0 grid place-items-center p-4 text-center">
      <i class="pi pi-star text-brand" style="font-size: 1.5rem" aria-hidden="true"></i>
    </div>

    <!-- Text sits on a dark scrim over the photo, so it's white in either theme. -->
    <div
      class="absolute inset-x-0 bottom-0 flex flex-col gap-0.5 bg-gradient-to-t from-black/85 via-black/50 to-transparent p-3 pt-10 text-white">
      <span class="text-xs text-white/75">{{ formatShortDate(date) }}</span>
      <span class="text-sm font-medium leading-snug line-clamp-2">{{ item.title }}</span>
    </div>
  </RouterLink>
</template>
