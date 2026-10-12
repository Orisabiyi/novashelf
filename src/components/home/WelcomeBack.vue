<script setup>
import { computed } from "vue";
import { RouterLink } from "vue-router";
import { previousVisit } from "@/composables/useLastVisit";

const props = defineProps({
  name: {
    type: String,
    required: true,
  },
  /** The newest articles (page 1 of the feed), to count what's new. */
  articles: {
    type: Array,
    default: () => [],
  },
  loading: {
    type: Boolean,
    default: false,
  },
});

const lastVisit = previousVisit();
const now = new Date();

const greeting = computed(() => {
  const hour = now.getHours();
  if (hour < 5) return "Still up";
  if (hour < 12) return "Good morning";
  if (hour < 18) return "Good afternoon";
  return "Good evening";
});

const today = new Intl.DateTimeFormat("en", { weekday: "long", month: "long", day: "numeric" }).format(now);

const newCount = computed(() =>
  lastVisit
    ? props.articles.filter((article) => article.publishedAt && new Date(article.publishedAt) > lastVisit).length
    : 0,
);

const summary = computed(() => {
  if (props.loading) return "Checking what's new in space…";
  if (!lastVisit) return "Here's what's happening in space today.";
  if (newCount.value === 0) return "You're all caught up. Nothing new since your last visit.";
  // The first page of the feed holds 20 stories, so "20" can mean "at least 20".
  const count = newCount.value >= props.articles.length ? `${newCount.value}+` : newCount.value;
  return `${count} new ${newCount.value === 1 ? "story" : "stories"} since your last visit.`;
});
</script>

<template>
  <div class="flex flex-col items-start gap-6">
    <span
      class="inline-flex items-center gap-2 rounded-full border border-line-strong bg-surface/60 backdrop-blur px-3 py-1 text-xs font-medium text-muted">
      <span class="size-1.5 rounded-full bg-brand" aria-hidden="true"></span>
      {{ today }}
    </span>

    <h1 class="font-display text-5xl sm:text-6xl lg:text-7xl font-bold leading-[0.95] tracking-tight text-balance">
      {{ greeting }},<br />
      <span class="text-brand">{{ name }}.</span>
    </h1>

    <p class="max-w-xl text-lg lg:text-xl text-muted" aria-live="polite">
      <span v-if="newCount > 0 && !loading" class="font-medium text-ink">{{ summary }}</span>
      <template v-else>{{ summary }}</template>
    </p>

    <div class="flex flex-wrap items-center gap-3">
      <RouterLink
        to="/explore"
        class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-6 py-3 font-medium transition-transform hover:-translate-y-0.5">
        {{ newCount > 0 ? "Catch up" : "Continue exploring" }}
        <i class="pi pi-arrow-right" style="font-size: 0.8rem" aria-hidden="true"></i>
      </RouterLink>
      <RouterLink
        to="/apod"
        class="inline-flex items-center gap-2 rounded-full border border-line-strong px-6 py-3 font-medium transition-colors hover:border-ink">
        <i class="pi pi-star-fill text-brand" style="font-size: 0.8rem" aria-hidden="true"></i>
        Today's picture
      </RouterLink>
    </div>
  </div>
</template>
