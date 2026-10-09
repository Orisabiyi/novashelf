<script setup>
import { onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import { getApod, getFeed } from "@/api/content";
import ContentCard from "@/components/content/ContentCard.vue";
import ContentCardSkeleton from "@/components/content/ContentCardSkeleton.vue";
import ApodFeature from "@/components/home/ApodFeature.vue";
import StarField from "@/components/home/StarField.vue";
import StartHereBanner from "@/components/home/StartHereBanner.vue";
import TopicGrid from "@/components/home/TopicGrid.vue";
import WelcomeBack from "@/components/home/WelcomeBack.vue";
import { useAuth } from "@/composables/useAuth";

const { status, isSignedIn, displayName } = useAuth();

// Two rows: [small, feature] then [feature, small], matching Explore.
const LATEST_VARIANTS = ["stacked", "feature", "feature", "stacked"];

const apod = ref(null);
const apodLoading = ref(true);
const latest = ref([]);
/** Page 1 of the feed, used to count what's new for signed-in readers. */
const feedItems = ref([]);
const latestLoading = ref(true);

onMounted(() => {
  getApod()
    .then((item) => (apod.value = item))
    .catch(() => {})
    .finally(() => (apodLoading.value = false));

  getFeed()
    .then((feed) => {
      feedItems.value = feed.items;
      latest.value = feed.items
        .filter((item) => item.imageUrl)
        .slice(0, LATEST_VARIANTS.length);
    })
    .catch(() => {})
    .finally(() => (latestLoading.value = false));
});
</script>

<template>
  <div>
    <!-- Pulled up under the floating navbar so the starfield shows around it. -->
    <section class="relative overflow-hidden -mt-20 pt-20">
      <StarField />
      <div class="mars-horizon" aria-hidden="true"></div>
      <!-- Fade the glow into the page so the hero has no hard bottom edge. -->
      <div
        class="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-b from-transparent to-page pointer-events-none"
        aria-hidden="true"></div>

      <div
        class="relative grid items-center gap-12 px-6 pt-10 pb-24 lg:grid-cols-[1.15fr_0.85fr] lg:gap-16 lg:px-10 lg:pt-16 lg:pb-32 2xl:px-48">
        <!-- Signed in: a personal welcome instead of the pitch. -->
        <WelcomeBack
          v-if="isSignedIn"
          :name="displayName"
          :articles="feedItems"
          :loading="latestLoading" />

        <!-- Still checking the session: hold the space without flashing the sign-up pitch. -->
        <div v-else-if="status === 'unknown'" class="flex flex-col gap-6 animate-pulse" aria-hidden="true">
          <div class="h-6 w-48 rounded-full bg-surface"></div>
          <div class="h-16 w-4/5 rounded-xl bg-surface"></div>
          <div class="h-16 w-3/5 rounded-xl bg-surface"></div>
          <div class="h-5 w-full max-w-xl rounded bg-surface"></div>
        </div>

        <div v-else class="flex flex-col items-start gap-6">
          <span
            class="inline-flex items-center gap-2 rounded-full border border-line-strong bg-surface/60 backdrop-blur px-3 py-1 text-xs font-medium text-muted">
            <span class="size-1.5 rounded-full bg-brand" aria-hidden="true"></span>
            Built on NASA open data · Updated daily
          </span>

          <h1
            class="font-display text-5xl sm:text-6xl lg:text-7xl font-bold leading-[0.95] tracking-tight">
            Space,<br />
            <span class="text-brand">made simple.</span>
          </h1>

          <p class="max-w-xl text-lg lg:text-xl text-muted">
            The universe is fascinating, and you shouldn't need a PhD to enjoy
            it. Read the latest from NASA, ESA and beyond, in plain language.
          </p>

          <div class="flex flex-wrap items-center gap-3">
            <RouterLink
              to="/explore"
              class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-6 py-3 font-medium transition-transform hover:-translate-y-0.5">
              Start reading
              <i class="pi pi-arrow-right" style="font-size: 0.8rem" aria-hidden="true"></i>
            </RouterLink>
            <RouterLink
              v-if="status !== 'unavailable'"
              to="/sign-up"
              class="inline-flex items-center rounded-full border border-line-strong px-6 py-3 font-medium transition-colors hover:border-ink">
              Create a free account
            </RouterLink>
          </div>
        </div>

        <div id="today" class="w-full max-w-md mx-auto lg:max-w-none">
          <ApodFeature :apod="apod" :loading="apodLoading" />
        </div>
      </div>
    </section>

    <StartHereBanner />

    <section
      class="px-6 py-12 lg:px-10 2xl:px-48 space-y-6"
      aria-labelledby="topics-heading">
      <div>
        <p class="text-sm font-medium text-link">Find your corner of the sky</p>
        <h2 id="topics-heading" class="font-display text-3xl font-bold">
          Pick a topic
        </h2>
      </div>
      <TopicGrid />
    </section>

    <section
      class="px-6 pt-6 pb-16 lg:px-10 2xl:px-48 space-y-6"
      aria-labelledby="latest-heading">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p class="text-sm font-medium text-link">Fresh from orbit</p>
          <h2 id="latest-heading" class="font-display text-3xl font-bold">
            Latest stories
          </h2>
        </div>
        <RouterLink
          to="/explore"
          class="inline-flex items-center gap-2 font-medium text-muted hover:text-ink">
          See everything
          <i class="pi pi-arrow-right" style="font-size: 0.75rem" aria-hidden="true"></i>
        </RouterLink>
      </div>

      <div class="grid gap-4 lg:gap-5 sm:grid-cols-2 lg:grid-cols-3">
        <template v-if="latestLoading">
          <ContentCardSkeleton
            v-for="(variant, index) in LATEST_VARIANTS"
            :key="index"
            :variant="variant" />
        </template>
        <ContentCard
          v-for="(item, index) in latest"
          v-else
          :key="item.id"
          :item="item"
          :variant="LATEST_VARIANTS[index]" />
      </div>
    </section>
  </div>
</template>

<style scoped>
/* A huge circle whose top edge peeks over the bottom of the hero: Mars rising. */
.mars-horizon {
  position: absolute;
  left: 50%;
  top: calc(100% - 7rem);
  width: 190%;
  aspect-ratio: 1;
  transform: translateX(-50%);
  border-radius: 50%;
  background: radial-gradient(
    circle at 50% 0%,
    color-mix(in oklab, var(--color-brand) 30%, transparent),
    transparent 40%
  );
  box-shadow:
    0 -1px 0 color-mix(in oklab, var(--color-brand) 55%, transparent),
    0 -40px 120px -30px color-mix(in oklab, var(--color-brand) 45%, transparent);
  pointer-events: none;
}
</style>
