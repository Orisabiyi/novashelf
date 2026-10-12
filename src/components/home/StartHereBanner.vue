<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import { getTracks } from "@/api/learn";

const track = ref(null);

onMounted(async () => {
  try {
    track.value = (await getTracks()).find((t) => t.startHere) ?? null;
  } catch {
    // The banner still links to /learn without the lesson preview.
  }
});

const firstLessons = computed(() => track.value?.lessons.slice(0, 3) ?? []);
</script>

<template>
  <section aria-labelledby="start-here-heading" class="px-6 lg:px-10 2xl:px-48">
    <div class="relative overflow-hidden rounded-3xl bg-surface p-6 sm:p-8 ring-1 ring-brand/30">
      <div class="absolute -left-20 -bottom-24 size-72 rounded-full bg-brand/15 blur-3xl" aria-hidden="true"></div>

      <div class="relative grid gap-8 lg:grid-cols-[1fr_1fr] lg:items-center">
        <div class="space-y-4">
          <p class="flex items-center gap-2 text-sm font-medium text-link">
            <i class="pi pi-book" aria-hidden="true"></i>
            Learn
          </p>
          <h2 id="start-here-heading" class="font-display text-3xl font-bold text-balance">
            New to space? Start here.
          </h2>
          <p class="text-lg text-muted max-w-xl">
            Short lessons on the ideas behind the news: how big space is, what stars and
            galaxies are, and how we explore them. No science background needed.
          </p>
          <RouterLink
            :to="track ? `/learn/${track.slug}/${track.lessons[0].slug}` : '/learn'"
            class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-6 py-3 font-medium transition-transform hover:-translate-y-0.5">
            Start the first lesson
            <i class="pi pi-arrow-right" style="font-size: 0.8rem" aria-hidden="true"></i>
          </RouterLink>
        </div>

        <ol v-if="firstLessons.length" class="space-y-3">
          <li v-for="(lesson, index) in firstLessons" :key="lesson.slug">
            <RouterLink
              :to="`/learn/${track.slug}/${lesson.slug}`"
              class="group flex items-center gap-4 rounded-2xl bg-page p-4 ring-1 ring-transparent transition hover:ring-brand/60">
              <span class="grid size-9 shrink-0 place-items-center rounded-full border border-line-strong font-display font-bold tabular-nums group-hover:border-brand group-hover:text-brand">
                {{ index + 1 }}
              </span>
              <span class="min-w-0">
                <span class="block font-medium">{{ lesson.title }}</span>
                <span class="block truncate text-sm text-muted">{{ lesson.summary }}</span>
              </span>
            </RouterLink>
          </li>
          <li class="pl-4 text-sm">
            <RouterLink to="/learn" class="font-medium text-link">See all learning paths →</RouterLink>
          </li>
        </ol>
      </div>
    </div>
  </section>
</template>
