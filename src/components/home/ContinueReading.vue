<script setup>
import { onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import { getHistory } from "@/api/me";
import { itemLink } from "@/utils/dates";
import { formatRelativeDate } from "@/utils/format";
import { imageAt } from "@/utils/images";
import { itemTag } from "@/utils/topics";

// The last few things the reader opened, to pick up where they left off. Hidden until there are some.
const recent = ref([]);

onMounted(() => {
  getHistory()
    .then((history) => (recent.value = history.items.slice(0, 3)))
    .catch(() => {});
});
</script>

<template>
  <section
    v-if="recent.length"
    class="px-6 pt-4 pb-8 lg:px-10 2xl:px-48 space-y-5"
    aria-labelledby="continue-heading">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="text-sm font-medium text-link">Pick up where you left off</p>
        <h2 id="continue-heading" class="font-display text-3xl font-bold">Continue reading</h2>
      </div>
      <RouterLink to="/history" class="inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-ink">
        Reading history
        <i class="pi pi-arrow-right" style="font-size: 0.7rem" aria-hidden="true"></i>
      </RouterLink>
    </div>

    <ul class="grid gap-3 md:grid-cols-3">
      <li v-for="entry in recent" :key="entry.item.id">
        <RouterLink
          :to="itemLink(entry.item)"
          class="flex h-full items-center gap-4 rounded-2xl bg-surface p-3 ring-1 ring-transparent transition hover:ring-brand/60">
          <img
            v-if="entry.item.imageUrl"
            :src="imageAt(entry.item.imageUrl, 320)"
            alt=""
            loading="lazy"
            class="size-20 shrink-0 rounded-xl bg-subtle object-cover" />
          <span v-else class="grid size-20 shrink-0 place-items-center rounded-xl bg-subtle text-muted" aria-hidden="true">
            <i :class="`pi ${itemTag(entry.item).icon}`"></i>
          </span>
          <span class="min-w-0 space-y-1">
            <span class="block font-medium leading-snug line-clamp-2">{{ entry.item.title }}</span>
            <span class="block truncate text-sm text-muted">Opened {{ formatRelativeDate(entry.readAt) }}</span>
          </span>
        </RouterLink>
      </li>
    </ul>
  </section>
</template>
