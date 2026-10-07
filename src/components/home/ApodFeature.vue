<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { prefetchReadable, rememberPreview } from "@/api/readerCache";
import { formatLongDate } from "@/utils/format";
import { imageAt, imageSrcset } from "@/utils/images";

const props = defineProps({
  /** @type {import("@/api/content").ContentItem | null} */
  apod: {
    type: Object,
    default: null,
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const imageFailed = ref(false);
const imageEl = ref(null);

const intent = () => prefetchReadable(props.apod, imageEl.value?.currentSrc);
const open = () => rememberPreview(props.apod, imageEl.value?.currentSrc);
const date = computed(() => formatLongDate(props.apod?.publishedAt));
</script>

<template>
  <div
    v-if="loading"
    class="aspect-[4/5] w-full rounded-[2rem] bg-surface animate-pulse"
    aria-hidden="true"></div>

  <RouterLink
    v-else-if="apod"
    :to="`/read/${apod.id}`"
    @pointerenter="intent"
    @focus="intent"
    @touchstart.passive="intent"
    @click="open"
    class="group relative block aspect-[4/5] w-full overflow-hidden rounded-[2rem] bg-surface ring-1 ring-ink/10 shadow-2xl shadow-black/50 focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-link">
    <img
      v-if="apod.imageUrl && !imageFailed"
      ref="imageEl"
      :src="imageAt(apod.imageUrl, 960)"
      :srcset="imageSrcset(apod.imageUrl, [640, 960, 1280, 1600])"
      sizes="(min-width: 1024px) 40vw, 100vw"
      fetchpriority="high"
      :alt="apod.title"
      class="absolute inset-0 size-full object-cover transition-transform duration-700 group-hover:scale-105"
      @error="imageFailed = true" />

    <!-- Text sits on a dark scrim over the photo, so it's white in either theme. -->
    <div
      class="absolute inset-x-0 bottom-0 flex flex-col items-start gap-3 bg-gradient-to-t from-black/90 via-black/60 to-transparent p-6 pt-24 text-white">
      <span
        class="inline-flex items-center gap-1.5 rounded-full bg-white/15 backdrop-blur px-3 py-1 text-xs font-medium">
        <i class="pi pi-star-fill text-brand" style="font-size: 0.7rem" aria-hidden="true"></i>
        Picture of the day
        <template v-if="date">· {{ date }}</template>
      </span>
      <h3 class="font-display text-2xl lg:text-3xl font-bold leading-tight text-balance">
        {{ apod.title }}
      </h3>
      <p v-if="apod.publisher" class="text-sm text-white/70 line-clamp-1">
        {{ apod.publisher }}
      </p>
    </div>
  </RouterLink>

  <div
    v-else
    class="aspect-[4/5] w-full rounded-[2rem] bg-surface grid place-items-center p-8 text-center text-muted">
    Today's picture isn't available right now.
  </div>
</template>
