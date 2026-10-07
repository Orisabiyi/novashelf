<script setup>
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getTopics } from "@/api/content";
import ContentCard from "@/components/content/ContentCard.vue";
import ContentCardSkeleton from "@/components/content/ContentCardSkeleton.vue";
import { useFeed } from "@/composables/useFeed";
import { useInView } from "@/composables/useInView";

const route = useRoute();
const router = useRouter();

// The selected topic lives in the URL (?topic=mars) so it survives reloads and can be shared.
const topic = computed(() =>
  typeof route.query.topic === "string" && route.query.topic
    ? route.query.topic
    : null,
);

const topics = ref([]);
onMounted(async () => {
  try {
    topics.value = await getTopics();
  } catch {
    // Without the topic list you can still read the full feed.
  }
});

function selectTopic(slug) {
  router.replace({ query: slug ? { topic: slug } : {} });
}

const { items, hasMore, loading, error, loadMore } = useFeed(topic);

// Keep loading while the bottom of the list is near the viewport. This also does the
// first load, and keeps going if a page comes back short.
const SENTINEL_MARGIN = 600;
const sentinel = ref(null);
const nearBottom = useInView(sentinel, `${SENTINEL_MARGIN}px`);

// The observer reports asynchronously, so right after a page renders `nearBottom` can
// still be true from before the new cards pushed the sentinel down. Measure for real.
function sentinelIsNear() {
  const rect = sentinel.value?.getBoundingClientRect();
  return !!rect && rect.top < window.innerHeight + SENTINEL_MARGIN;
}

watch(
  [nearBottom, loading, hasMore, error],
  () => {
    if (
      nearBottom.value &&
      !loading.value &&
      hasMore.value &&
      !error.value &&
      sentinelIsNear()
    ) {
      loadMore();
    }
  },
  { immediate: true, flush: "post" },
);

const initialLoad = computed(() => loading.value && items.value.length === 0);
const isEmpty = computed(
  () => !loading.value && !error.value && !hasMore.value && items.value.length === 0,
);

// Rows on large screens pair a small card with a two-column feature card, alternating
// sides: [small, feature], [feature, small], … Smaller screens show a plain grid.
function variantAt(index) {
  const row = Math.floor(index / 2);
  const first = index % 2 === 0;
  return (row % 2 === 0) !== first ? "feature" : "stacked";
}

const chipClass = (selected) =>
  `shrink-0 px-4 py-1 rounded-full border-2 font-medium transition-colors ${
    selected
      ? "bg-action border-action text-on-action"
      : "bg-surface border-surface text-slate hover:border-link hover:text-link"
  }`;
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48 space-y-8">
    <header class="space-y-4">
      <h2 class="text-4xl font-bold">Explore</h2>
      <p class="text-lg text-muted">
        The latest from NASA, ESA and the rest of the space world.
      </p>

      <nav
        aria-label="Topics"
        class="flex gap-2 overflow-x-auto pb-2 -mx-6 px-6 lg:mx-0 lg:px-0 lg:flex-wrap">
        <button
          type="button"
          :class="chipClass(topic === null)"
          :aria-pressed="topic === null"
          @click="selectTopic(null)">
          All
        </button>
        <button
          v-for="t in topics"
          :key="t.slug"
          type="button"
          :class="chipClass(topic === t.slug)"
          :aria-pressed="topic === t.slug"
          @click="selectTopic(t.slug)">
          {{ t.name }}
        </button>
      </nav>
    </header>

    <div
      v-if="error && items.length === 0"
      role="alert"
      class="flex flex-col items-center gap-4 py-20 text-center">
      <i class="pi pi-exclamation-circle text-brand" style="font-size: 2.5rem"></i>
      <p class="text-lg max-w-md">{{ error.message }}</p>
      <!-- A 400 (e.g. an unknown topic in the URL) won't succeed on retry. -->
      <button
        v-if="error.status === 400"
        type="button"
        class="bg-action px-4 py-1 rounded-md text-on-action"
        @click="selectTopic(null)">
        Show all articles
      </button>
      <button
        v-else
        type="button"
        class="bg-action px-4 py-1 rounded-md text-on-action"
        @click="loadMore">
        Try again
      </button>
    </div>

    <div
      v-else-if="isEmpty"
      class="flex flex-col items-center gap-4 py-20 text-center">
      <i class="pi pi-search text-muted" style="font-size: 2.5rem"></i>
      <p class="text-lg">No articles on this topic yet. Try another one.</p>
    </div>

    <div v-else class="grid gap-4 lg:gap-5 sm:grid-cols-2 lg:grid-cols-3">
      <template v-if="initialLoad">
        <ContentCardSkeleton v-for="n in 4" :key="n" :variant="variantAt(n - 1)" />
      </template>
      <template v-else>
        <ContentCard
          v-for="(item, index) in items"
          :key="item.id"
          :item="item"
          :variant="variantAt(index)" />
        <template v-if="loading">
          <ContentCardSkeleton
            v-for="n in 2"
            :key="`more-${n}`"
            :variant="variantAt(items.length + n - 1)" />
        </template>
      </template>
    </div>

    <div
      v-if="error && items.length > 0"
      role="alert"
      class="flex flex-wrap items-center justify-center gap-4 text-center">
      <p>{{ error.message }}</p>
      <button
        type="button"
        class="border-2 border-link text-link px-4 py-1 rounded-md font-medium"
        @click="loadMore">
        Try again
      </button>
    </div>

    <p
      v-if="!hasMore && items.length > 0"
      class="text-center text-muted pb-4">
      You've reached the edge of the known universe. 🌌
    </p>

    <div ref="sentinel" aria-hidden="true"></div>
  </section>
</template>
