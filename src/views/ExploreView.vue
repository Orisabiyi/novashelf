<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink, useRoute, useRouter } from "vue-router";
import { getTopics } from "@/api/content";
import ContentCard from "@/components/content/ContentCard.vue";
import ContentCardSkeleton from "@/components/content/ContentCardSkeleton.vue";
import { useAuth } from "@/composables/useAuth";
import { useFeed } from "@/composables/useFeed";
import { loadFollowedTopics, toggleFollow, useFollowedTopics } from "@/composables/useFollowedTopics";
import { useInfiniteScroll } from "@/composables/useInfiniteScroll";
import { TOPIC_ICONS } from "@/utils/topics";

const route = useRoute();
const router = useRouter();

const topic = computed(() =>
  typeof route.query.topic === "string" && route.query.topic
    ? route.query.topic
    : null,
);

const { status, isSignedIn } = useAuth();
const { followed, isFollowing } = useFollowedTopics();

const topics = ref([]);
onMounted(async () => {
  loadFollowedTopics();
  try {
    topics.value = await getTopics();
  } catch {
  }
});

function selectTopic(slug) {
  router.replace({ query: slug ? { topic: slug } : {} });
}

const { items, hasMore, loading, error, loadMore } = useFeed(topic);

const isFollowingFeed = computed(() => topic.value === "following");
const selectedTopic = computed(() => topics.value.find((t) => t.slug === topic.value) ?? null);
const followedTopics = computed(() => topics.value.filter((t) => isFollowing(t.slug)));

const followError = ref("");
async function onToggleFollow(slug) {
  followError.value = "";
  try {
    await toggleFollow(slug);
  } catch (e) {
    followError.value = e.message;
  }
}

const sentinel = ref(null);
useInfiniteScroll(sentinel, { loading, hasMore, error, loadMore });

const initialLoad = computed(
  () => (loading.value || status.value === "unknown") && items.value.length === 0 && !error.value,
);
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
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div class="space-y-4">
          <h2 class="text-4xl font-bold">{{ isFollowingFeed ? "Following" : selectedTopic?.name ?? "Explore" }}</h2>
          <p class="text-lg text-muted">
            <template v-if="isFollowingFeed">
              {{ followedTopics.length ? `The latest on ${followedTopics.map((t) => t.name).join(", ")}.` : "Topics you follow, in one feed." }}
            </template>
            <template v-else>The latest from NASA, ESA and the rest of the space world.</template>
          </p>
        </div>

        <button
          v-if="isSignedIn && selectedTopic"
          type="button"
          :aria-pressed="isFollowing(selectedTopic.slug)"
          :class="[
            'group inline-flex items-center gap-2 rounded-full px-5 py-2.5 font-medium transition-colors',
            isFollowing(selectedTopic.slug)
              ? 'border-2 border-brand text-brand hover:border-line-strong hover:text-muted'
              : 'bg-action text-on-action hover:-translate-y-px',
          ]"
          @click="onToggleFollow(selectedTopic.slug)">
          <template v-if="isFollowing(selectedTopic.slug)">
            <!-- On hover it turns into an Unfollow button. (Spans: .pi's own display beats `hidden`.) -->
            <span class="inline-flex items-center gap-2 group-hover:hidden">
              <i class="pi pi-check" style="font-size: 0.8rem" aria-hidden="true"></i>
              Following {{ selectedTopic.name }}
            </span>
            <span class="hidden items-center gap-2 group-hover:inline-flex">
              <i class="pi pi-times" style="font-size: 0.8rem" aria-hidden="true"></i>
              Unfollow
            </span>
          </template>
          <template v-else>
            <i class="pi pi-plus" style="font-size: 0.8rem" aria-hidden="true"></i>
            Follow {{ selectedTopic.name }}
          </template>
        </button>
        <RouterLink
          v-else-if="status === 'signedOut' && selectedTopic"
          :to="{ path: '/login', query: { redirect: route.fullPath } }"
          class="inline-flex items-center gap-2 rounded-full border-2 border-line-strong px-5 py-2.5 font-medium transition-colors hover:border-ink">
          <i class="pi pi-plus" style="font-size: 0.8rem" aria-hidden="true"></i>
          Follow {{ selectedTopic.name }}
        </RouterLink>
      </div>
      <p v-if="followError" role="alert" class="text-sm text-brand">{{ followError }}</p>

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
          v-if="isSignedIn"
          type="button"
          :class="chipClass(isFollowingFeed)"
          :aria-pressed="isFollowingFeed"
          @click="selectTopic('following')">
          <i class="pi pi-heart-fill mr-1" style="font-size: 0.75rem" aria-hidden="true"></i>
          Following
        </button>
        <button
          v-for="t in topics"
          :key="t.slug"
          type="button"
          :class="chipClass(topic === t.slug)"
          :aria-pressed="topic === t.slug"
          @click="selectTopic(t.slug)">
          {{ t.name }}
          <i
            v-if="isFollowing(t.slug)"
            class="pi pi-check ml-1"
            style="font-size: 0.7rem"
            aria-label="(following)"></i>
        </button>
      </nav>
    </header>

    <div
      v-if="error && items.length === 0"
      role="alert"
      class="flex flex-col items-center gap-4 py-20 text-center">
      <i :class="`pi ${error.status === 401 ? 'pi-heart' : 'pi-exclamation-circle'} text-brand`" style="font-size: 2.5rem" aria-hidden="true"></i>
      <p class="text-lg max-w-md">{{ error.message }}</p>
      <RouterLink
        v-if="error.status === 401"
        :to="{ path: '/login', query: { redirect: route.fullPath } }"
        class="bg-action px-4 py-1 rounded-md text-on-action">
        Sign in
      </RouterLink>
      <button
        v-else-if="error.status === 400"
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
      v-else-if="isFollowingFeed && isSignedIn && isEmpty && followed.length === 0"
      class="flex flex-col items-center gap-6 rounded-3xl bg-surface px-6 py-14 text-center">
      <i class="pi pi-heart text-brand" style="font-size: 2.5rem" aria-hidden="true"></i>
      <div class="space-y-2">
        <p class="text-xl font-medium">You're not following any topics yet.</p>
        <p class="text-muted">Pick a few and their latest articles will gather here.</p>
      </div>
      <ul class="flex max-w-2xl flex-wrap justify-center gap-2">
        <li v-for="t in topics" :key="t.slug">
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-full border-2 border-line-strong px-4 py-1.5 font-medium transition-colors hover:border-brand hover:text-brand"
            @click="onToggleFollow(t.slug)">
            <i :class="`pi ${TOPIC_ICONS[t.slug] ?? 'pi-compass'}`" style="font-size: 0.8rem" aria-hidden="true"></i>
            {{ t.name }}
            <i class="pi pi-plus" style="font-size: 0.7rem" aria-hidden="true"></i>
          </button>
        </li>
      </ul>
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
