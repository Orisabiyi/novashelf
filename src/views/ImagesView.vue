<script setup>
import { computed, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { searchImages } from "@/api/content";
import PhotoTile from "@/components/content/PhotoTile.vue";
import { useInfiniteScroll } from "@/composables/useInfiniteScroll";
import { usePagedList } from "@/composables/usePagedList";

// NASA's search has no relevance ranking ("mars" mostly returns a festival in Mars,
// Pennsylvania), so each chip runs a more specific query than its label.
const SUGGESTIONS = [
  { label: "Mars", q: "mars rover" },
  { label: "Moon landing", q: "apollo 11 lunar surface" },
  { label: "Saturn", q: "saturn" },
  { label: "Jupiter", q: "jupiter" },
  { label: "The Moon", q: "moon" },
  { label: "Nebulae", q: "nebula" },
  { label: "Galaxies", q: "galaxy" },
  { label: "Hubble", q: "hubble" },
  { label: "Space station", q: "space station" },
];
// Shown before anyone searches, so the page is never empty.
const FEATURED_QUERY = "Hubble";

const route = useRoute();
const router = useRouter();

// The search lives in the URL (?q=saturn) so results can be shared and Back works.
const query = computed(() =>
  typeof route.query.q === "string" ? route.query.q.trim().slice(0, 100) : "",
);
const effectiveQuery = computed(() => query.value || FEATURED_QUERY);

const draft = ref(query.value);
watch(query, (q) => (draft.value = q));

function search(text) {
  const q = text.trim();
  router.push({ query: q ? { q } : {} });
}

const source = computed(() => {
  const q = effectiveQuery.value;
  return (page, options) => searchImages({ q, page }, options);
});
const { items, hasMore, loading, error, loadMore } = usePagedList(source);

const sentinel = ref(null);
useInfiniteScroll(sentinel, { loading, hasMore, error, loadMore });

const isEmpty = computed(
  () => !loading.value && !error.value && !hasMore.value && items.value.length === 0,
);

const chipClass = (selected) =>
  `shrink-0 rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
    selected
      ? "bg-action text-on-action"
      : "bg-surface text-slate hover:text-link"
  }`;
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48 space-y-8">
    <header class="space-y-3 max-w-3xl">
      <p class="text-sm font-medium text-link">NASA Image and Video Library</p>
      <h1 class="font-display text-4xl lg:text-5xl font-bold tracking-tight">Images</h1>
      <p class="text-lg text-muted">
        Search NASA's library of mission photography, from the Apollo landings to the
        James Webb Space Telescope.
      </p>
    </header>

    <div class="space-y-4">
      <form role="search" class="flex max-w-2xl gap-2" @submit.prevent="search(draft)">
        <label for="image-search" class="sr-only">Search NASA images</label>
        <div class="relative flex-1">
          <i
            class="pi pi-search pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-muted"
            aria-hidden="true"></i>
          <input
            id="image-search"
            v-model="draft"
            type="search"
            maxlength="100"
            placeholder="Search for a planet, mission or telescope…"
            autocomplete="off"
            class="w-full rounded-full border border-line-strong bg-surface py-3 pl-11 pr-4 placeholder:text-muted focus:outline-2 focus:outline-link" />
        </div>
        <button type="submit" class="rounded-full bg-action text-on-action px-6 font-medium">
          Search
        </button>
      </form>

      <nav aria-label="Suggested searches" class="flex gap-2 overflow-x-auto pb-1 -mx-6 px-6 lg:mx-0 lg:px-0 lg:flex-wrap">
        <button
          v-for="suggestion in SUGGESTIONS"
          :key="suggestion.q"
          type="button"
          :class="chipClass(query.toLowerCase() === suggestion.q)"
          :aria-pressed="query.toLowerCase() === suggestion.q"
          @click="search(suggestion.q)">
          {{ suggestion.label }}
        </button>
      </nav>
    </div>

    <div>
      <h2 class="mb-4 text-sm font-medium text-muted" aria-live="polite">
        <template v-if="query">Results for “{{ query }}”</template>
        <template v-else>Featured: {{ FEATURED_QUERY }}</template>
      </h2>

      <div
        v-if="error && items.length === 0"
        role="alert"
        class="flex flex-col items-center gap-4 py-16 text-center">
        <i class="pi pi-exclamation-circle text-brand" style="font-size: 2.5rem" aria-hidden="true"></i>
        <p class="max-w-md text-lg">{{ error.message }}</p>
        <button type="button" class="rounded-full bg-action text-on-action px-5 py-2 font-medium" @click="loadMore">
          Try again
        </button>
      </div>

      <div v-else-if="isEmpty" class="flex flex-col items-center gap-3 py-16 text-center">
        <i class="pi pi-image text-muted" style="font-size: 2.5rem" aria-hidden="true"></i>
        <p class="text-lg">No images found for “{{ query }}”.</p>
        <p class="text-muted">Try a broader word, like a planet or mission name.</p>
      </div>

      <div v-else class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 lg:gap-4">
        <PhotoTile v-for="item in items" :key="item.id" :item="item" />
        <template v-if="loading">
          <div
            v-for="n in 8"
            :key="`loading-${n}`"
            class="aspect-square rounded-2xl bg-surface animate-pulse"
            aria-hidden="true"></div>
        </template>
      </div>

      <div
        v-if="error && items.length > 0"
        role="alert"
        class="mt-6 flex flex-wrap items-center justify-center gap-4 text-center">
        <p>{{ error.message }}</p>
        <button type="button" class="rounded-full border border-link px-4 py-1.5 font-medium text-link" @click="loadMore">
          Try again
        </button>
      </div>

      <div ref="sentinel" aria-hidden="true"></div>
    </div>
  </section>
</template>
