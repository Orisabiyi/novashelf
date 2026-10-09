<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { clearHistory, getHistory, removeFromHistory } from "@/api/me";
import { useInfiniteScroll } from "@/composables/useInfiniteScroll";
import { usePagedList } from "@/composables/usePagedList";
import { forgetAllReads, forgetRead } from "@/composables/useReadState";
import { itemLink } from "@/utils/dates";
import { formatRelativeDate } from "@/utils/format";
import { imageAt } from "@/utils/images";
import { itemTag } from "@/utils/topics";

const list = usePagedList(ref((page, options) => getHistory({ page }, options)));
const { items, hasMore, loading, error, loadMore } = list;
const sentinel = ref(null);
useInfiniteScroll(sentinel, list);

// Group entries by the day they were read: Today, Yesterday, then dates.
const groups = computed(() => {
  const dayFormat = new Intl.DateTimeFormat("en", { weekday: "long", month: "long", day: "numeric" });
  const today = new Date().toDateString();
  const yesterday = new Date(Date.now() - 86_400_000).toDateString();
  const result = [];
  for (const entry of items.value) {
    const day = new Date(entry.readAt);
    const label = day.toDateString() === today ? "Today" : day.toDateString() === yesterday ? "Yesterday" : dayFormat.format(day);
    if (result.at(-1)?.label !== label) result.push({ label, entries: [] });
    result.at(-1).entries.push(entry);
  }
  return result;
});

const confirmingClear = ref(false);
const actionError = ref("");

async function remove(entry) {
  actionError.value = "";
  try {
    await removeFromHistory(entry.item.id);
    forgetRead(entry.item.id);
    items.value = items.value.filter((e) => e.item.id !== entry.item.id);
  } catch (e) {
    actionError.value = e.message;
  }
}

async function clearAll() {
  actionError.value = "";
  try {
    await clearHistory();
    forgetAllReads();
    confirmingClear.value = false;
    items.value = [];
    hasMore.value = false;
  } catch (e) {
    actionError.value = e.message;
  }
}

const isEmpty = computed(() => !loading.value && !error.value && !hasMore.value && items.value.length === 0);
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48">
    <div class="mx-auto max-w-3xl space-y-8">
      <header class="flex flex-wrap items-end justify-between gap-4">
        <div class="space-y-2">
          <p class="text-sm font-medium text-link">Your account</p>
          <h1 class="font-display text-4xl font-bold tracking-tight">Reading history</h1>
          <p class="text-muted">Everything you've opened, newest first. Only you can see this.</p>
        </div>

        <div v-if="items.length" class="flex items-center gap-2">
          <template v-if="confirmingClear">
            <span class="text-sm text-muted">Clear all history?</span>
            <button type="button" class="rounded-full bg-action text-on-action px-4 py-1.5 text-sm font-medium" @click="clearAll">
              Yes, clear it
            </button>
            <button type="button" class="rounded-full px-3 py-1.5 text-sm font-medium text-muted hover:text-ink" @click="confirmingClear = false">
              Cancel
            </button>
          </template>
          <button
            v-else
            type="button"
            class="rounded-full border border-line-strong px-4 py-1.5 text-sm font-medium hover:border-ink"
            @click="confirmingClear = true">
            Clear history
          </button>
        </div>
      </header>

      <p v-if="actionError" role="alert" class="rounded-xl bg-surface px-4 py-3 text-sm">{{ actionError }}</p>

      <div v-if="error && items.length === 0" role="alert" class="flex flex-col items-center gap-4 rounded-3xl bg-surface py-16 text-center">
        <p class="text-lg">{{ error.message }}</p>
        <button type="button" class="rounded-full bg-action text-on-action px-5 py-2 font-medium" @click="loadMore">Try again</button>
      </div>

      <div v-else-if="isEmpty" class="flex flex-col items-center gap-4 rounded-3xl bg-surface py-16 text-center">
        <i class="pi pi-history text-muted" style="font-size: 2.5rem" aria-hidden="true"></i>
        <p class="text-lg">Nothing here yet.</p>
        <p class="text-muted">Articles and pictures you open will appear here.</p>
        <RouterLink to="/explore" class="rounded-full bg-action text-on-action px-5 py-2 font-medium">Start exploring</RouterLink>
      </div>

      <div v-else class="space-y-8">
        <section v-for="group in groups" :key="group.label" :aria-label="group.label" class="space-y-3">
          <h2 class="text-sm font-medium text-muted">{{ group.label }}</h2>
          <ul class="space-y-2">
            <li v-for="entry in group.entries" :key="entry.item.id" class="group relative">
              <RouterLink
                :to="itemLink(entry.item)"
                class="flex items-center gap-4 rounded-2xl bg-surface p-3 pr-14 ring-1 ring-transparent transition hover:ring-brand/60">
                <img
                  v-if="entry.item.imageUrl"
                  :src="imageAt(entry.item.imageUrl, 320)"
                  alt=""
                  loading="lazy"
                  class="size-16 shrink-0 rounded-xl bg-subtle object-cover sm:h-16 sm:w-24" />
                <span v-else class="grid size-16 shrink-0 place-items-center rounded-xl bg-subtle text-muted sm:w-24" aria-hidden="true">
                  <i :class="`pi ${itemTag(entry.item).icon}`"></i>
                </span>
                <span class="min-w-0">
                  <span class="block font-medium leading-snug line-clamp-2">{{ entry.item.title }}</span>
                  <span class="block truncate text-sm text-muted">
                    {{ [entry.item.publisher, `read ${formatRelativeDate(entry.readAt)}`].filter(Boolean).join(" · ") }}
                  </span>
                </span>
              </RouterLink>
              <button
                type="button"
                class="absolute right-3 top-1/2 grid size-9 -translate-y-1/2 place-items-center rounded-full text-muted hover:bg-page hover:text-ink"
                :aria-label="`Remove “${entry.item.title}” from history`"
                @click="remove(entry)">
                <i class="pi pi-times" style="font-size: 0.8rem" aria-hidden="true"></i>
              </button>
            </li>
          </ul>
        </section>

        <div v-if="loading" class="space-y-2" aria-hidden="true">
          <div v-for="n in 3" :key="n" class="h-22 rounded-2xl bg-surface animate-pulse"></div>
        </div>
      </div>

      <div ref="sentinel" aria-hidden="true"></div>
    </div>
  </section>
</template>
