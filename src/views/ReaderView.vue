<script setup>
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import DOMPurify from "dompurify";
import { getReadable } from "@/api/content";
import ReaderSettings from "@/components/reader/ReaderSettings.vue";
import { FONT_SIZES, WIDTHS, useReaderPreferences } from "@/composables/useReaderPreferences";
import { formatLongDate, formatRelativeDate } from "@/utils/format";
import { itemTag } from "@/utils/topics";

const route = useRoute();
const router = useRouter();
const preferences = useReaderPreferences();

const maxWidth = computed(
  () => WIDTHS.find((w) => w.value === preferences.width)?.maxWidth ?? "68ch",
);
const proseSize = computed(
  () => FONT_SIZES.find((f) => f.value === preferences.fontSize)?.proseClass ?? "prose-lg",
);

/** @type {import("vue").Ref<import("@/api/content").ReadableContent | null>} */
const readable = ref(null);
const loading = ref(true);
/** @type {import("vue").Ref<import("@/api/client").ApiError | null>} */
const error = ref(null);
const heroFailed = ref(false);
// Pictures keep their natural shape, so until one loads it has no height. Hold its space.
const heroLoaded = ref(false);

let controller = null;

async function load(id) {
  controller?.abort();
  const current = new AbortController();
  controller = current;

  loading.value = true;
  error.value = null;
  readable.value = null;
  heroFailed.value = false;
  heroLoaded.value = false;

  try {
    readable.value = await getReadable(id, { signal: current.signal });
    document.title = `${readable.value.item.title} · Novashelf`;
  } catch (e) {
    if (e.name !== "AbortError") error.value = e;
  } finally {
    if (controller === current) loading.value = false;
  }
}

watch(() => route.params.id, (id) => id && load(String(id)), { immediate: true });

const item = computed(() => readable.value?.item ?? null);
const tag = computed(() => (item.value ? itemTag(item.value) : null));
const isPicture = computed(() => item.value && item.value.kind !== "article");

// The server already sanitises; this is a second, independent layer before v-html.
const safeHtml = computed(() =>
  readable.value
    ? DOMPurify.sanitize(readable.value.html, {
        ADD_ATTR: ["target", "loading", "decoding", "referrerpolicy"],
      })
    : "",
);

// Skip the hero when the article body already opens with the same picture.
const showHero = computed(() => {
  const url = item.value?.imageUrl;
  if (!url || heroFailed.value) return false;
  const file = url.split("?")[0].split("/").pop();
  return !(file && safeHtml.value.slice(0, 2000).includes(file));
});

const meta = computed(() => {
  if (!readable.value) return [];
  const { item, byline, readingMinutes } = readable.value;
  const date = item.kind === "article"
    ? formatRelativeDate(item.publishedAt)
    : formatLongDate(item.publishedAt);
  return [
    byline && byline !== item.publisher ? byline : null,
    item.publisher,
    date,
    `${readingMinutes} min read`,
  ].filter(Boolean);
});

function goBack() {
  // Return to wherever the reader came from (keeping Explore's scroll), else to Explore.
  if (window.history.state?.back) router.back();
  else router.push("/explore");
}
</script>

<template>
  <article class="px-6 pt-8 pb-20 lg:px-10">
    <div class="mx-auto transition-[max-width] duration-300" :style="{ maxWidth }">
      <div class="mb-8 flex items-center justify-between gap-4">
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-full px-3 py-1.5 -ml-3 text-sm font-medium text-muted hover:text-ink hover:bg-surface transition-colors"
          @click="goBack">
          <i class="pi pi-arrow-left" style="font-size: 0.75rem" aria-hidden="true"></i>
          Back
        </button>
        <ReaderSettings />
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-6 animate-pulse" aria-busy="true" aria-label="Loading article">
        <div class="h-6 w-28 rounded-full bg-surface"></div>
        <div class="space-y-3">
          <div class="h-10 w-full rounded bg-surface"></div>
          <div class="h-10 w-3/4 rounded bg-surface"></div>
        </div>
        <div class="h-4 w-1/2 rounded bg-surface"></div>
        <div class="aspect-video rounded-2xl bg-surface"></div>
        <div class="space-y-3">
          <div v-for="n in 6" :key="n" class="h-4 rounded bg-surface" :class="n === 6 ? 'w-2/3' : 'w-full'"></div>
        </div>
      </div>

      <!-- Error -->
      <div
        v-else-if="error"
        role="alert"
        class="flex flex-col items-center gap-4 py-20 text-center">
        <i class="pi pi-exclamation-circle text-brand" style="font-size: 2.5rem" aria-hidden="true"></i>
        <h1 class="font-display text-2xl font-bold">
          {{ error.isNotFound ? "We couldn't find that story" : "This story didn't load" }}
        </h1>
        <p class="max-w-md text-muted">
          {{ error.isNotFound ? "It may have been removed by its publisher." : error.message }}
        </p>
        <div class="flex flex-wrap justify-center gap-3">
          <button
            v-if="!error.isNotFound"
            type="button"
            class="rounded-full bg-action text-on-action px-5 py-2 font-medium"
            @click="load(String(route.params.id))">
            Try again
          </button>
          <RouterLink
            to="/explore"
            class="rounded-full border border-line-strong px-5 py-2 font-medium hover:border-ink">
            Back to Explore
          </RouterLink>
        </div>
      </div>

      <!-- Article -->
      <template v-else-if="readable && item">
        <header class="space-y-5">
          <span
            v-if="tag"
            class="inline-flex items-center gap-1.5 rounded-full bg-slate text-on-slate px-3 py-1 text-xs font-medium">
            <i :class="`pi ${tag.icon}`" style="font-size: 0.75rem" aria-hidden="true"></i>
            {{ tag.label }}
          </span>

          <h1
            class="font-display text-3xl sm:text-4xl lg:text-5xl font-bold leading-[1.05] tracking-tight text-balance">
            {{ item.title }}
          </h1>

          <p class="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-muted">
            <template v-for="(part, index) in meta" :key="part">
              <span v-if="index > 0" aria-hidden="true">·</span>
              <span>{{ part }}</span>
            </template>
          </p>
        </header>

        <figure v-if="showHero" :class="isPicture ? 'my-8 -mx-6 sm:mx-0' : 'my-8'">
          <img
            :src="item.imageUrl"
            :alt="isPicture ? item.title : ''"
            referrerpolicy="no-referrer"
            :class="[
              'w-full bg-surface sm:rounded-2xl',
              isPicture
                ? heroLoaded
                  ? 'max-h-[80vh] object-contain'
                  : 'aspect-[4/3] animate-pulse'
                : 'aspect-video object-cover rounded-2xl',
            ]"
            @load="heroLoaded = true"
            @error="heroFailed = true" />
        </figure>

        <div
          v-if="!readable.extracted"
          class="my-8 flex flex-col gap-3 rounded-2xl border border-line-strong bg-surface p-5 sm:flex-row sm:items-center">
          <i class="pi pi-info-circle text-link" aria-hidden="true"></i>
          <p class="flex-1 text-sm">
            We couldn't load the full story from {{ item.publisher || "its publisher" }}, so here's
            the summary.
          </p>
          <a
            v-if="item.url"
            :href="item.url"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-2 self-start rounded-full bg-action text-on-action px-4 py-1.5 text-sm font-medium">
            Read the full story
            <i class="pi pi-external-link" style="font-size: 0.7rem" aria-hidden="true"></i>
          </a>
        </div>

        <div
          :class="proseSize"
          class="prose max-w-none mt-8 prose-headings:font-display prose-img:rounded-xl prose-a:underline-offset-2 prose-figcaption:text-sm"
          v-html="safeHtml"></div>

        <footer
          v-if="item.url"
          class="mt-12 flex flex-wrap items-center justify-between gap-4 border-t border-line pt-6 text-sm text-muted">
          <span>Originally published by {{ item.publisher || "its source" }}</span>
          <a
            :href="item.url"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-2 rounded-full border border-line-strong px-4 py-1.5 font-medium text-ink hover:border-ink">
            View original
            <i class="pi pi-external-link" style="font-size: 0.7rem" aria-hidden="true"></i>
          </a>
        </footer>
      </template>
    </div>
  </article>
</template>
