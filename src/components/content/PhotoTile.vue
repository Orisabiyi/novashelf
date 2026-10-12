<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { prefetchReadable, rememberPreview } from "@/api/readerCache";
import { imageAt, imageSrcset } from "@/utils/images";
import { isRead } from "@/composables/useReadState";

const props = defineProps({
  /** @type {import("@/api/content").ContentItem} */
  item: {
    type: Object,
    required: true,
  },
  /** Where the tile goes; defaults to the reader. */
  to: {
    type: [String, Object],
    default: null,
  },
  /** Small line above the title, e.g. a date. */
  label: {
    type: String,
    default: "",
  },
  selected: {
    type: Boolean,
    default: false,
  },
});

const imageFailed = ref(false);
const imageEl = ref(null);
const hasImage = computed(() => props.item.imageUrl && !imageFailed.value);
const target = computed(() => props.to ?? `/read/${props.item.id}`);
const opensReader = computed(() => !props.to);

// Tiles that open the reader warm it up on hover/focus/touch, like content cards.
function onIntent() {
  if (opensReader.value) prefetchReadable(props.item, imageEl.value?.currentSrc);
}
function onOpen() {
  if (opensReader.value) rememberPreview(props.item, imageEl.value?.currentSrc);
}
</script>

<template>
  <RouterLink
    :to="target"
    :aria-current="selected ? 'true' : undefined"
    :class="[
      'group relative block aspect-square overflow-hidden rounded-2xl bg-surface focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-link',
      selected ? 'ring-2 ring-brand ring-offset-2 ring-offset-page' : '',
    ]"
    @pointerenter="onIntent"
    @focus="onIntent"
    @touchstart.passive="onIntent"
    @click="onOpen">
    <img
      v-if="hasImage"
      ref="imageEl"
      :src="imageAt(item.imageUrl, 480)"
      :srcset="imageSrcset(item.imageUrl, [320, 480, 640])"
      sizes="(min-width: 1024px) 25vw, (min-width: 640px) 33vw, 50vw"
      alt=""
      loading="lazy"
      decoding="async"
      class="absolute inset-0 size-full object-cover transition-transform duration-500 group-hover:scale-105"
      @error="imageFailed = true" />
    <!-- No picture (e.g. some of the earliest APODs): show the title on a plain tile. -->
    <div v-else class="absolute inset-0 grid place-items-center p-4 text-center">
      <i class="pi pi-star text-brand" style="font-size: 1.5rem" aria-hidden="true"></i>
    </div>

    <span
      v-if="isRead(item)"
      class="absolute right-2 top-2 inline-flex items-center gap-1 rounded-full bg-black/60 px-2 py-0.5 text-xs font-medium text-white backdrop-blur"
      title="You've opened this">
      <i class="pi pi-check" style="font-size: 0.6rem" aria-hidden="true"></i>
      Read
    </span>

    <!-- Text sits on a dark scrim over the photo, so it's white in either theme. -->
    <div
      class="absolute inset-x-0 bottom-0 flex flex-col gap-0.5 bg-gradient-to-t from-black/85 via-black/50 to-transparent p-3 pt-10 text-white">
      <span v-if="label" class="text-xs text-white/75">{{ label }}</span>
      <span class="text-sm font-medium leading-snug line-clamp-2">{{ item.title }}</span>
    </div>
  </RouterLink>
</template>
