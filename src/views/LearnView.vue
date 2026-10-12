<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import { getTracks } from "@/api/learn";
import TrackProgress from "@/components/learn/TrackProgress.vue";
import { loadLessonProgress, useLessonProgress } from "@/composables/useLessonProgress";

const tracks = ref([]);
const { isCompleted, completedIn } = useLessonProgress();
loadLessonProgress();
const loading = ref(true);
const error = ref(null);

async function load() {
  loading.value = true;
  error.value = null;
  try {
    tracks.value = await getTracks();
  } catch (e) {
    error.value = e;
  } finally {
    loading.value = false;
  }
}
onMounted(load);

const startTrack = computed(() => tracks.value.find((t) => t.startHere) ?? null);
const otherTracks = computed(() => tracks.value.filter((t) => !t.startHere));
const lessonCount = computed(() => tracks.value.reduce((sum, t) => sum + t.lessons.length, 0));
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48 space-y-10">
    <header class="space-y-3 max-w-3xl">
      <p class="text-sm font-medium text-link">Learn</p>
      <h1 class="font-display text-4xl lg:text-5xl font-bold tracking-tight text-balance">
        New to space? Start here.
      </h1>
      <p class="text-lg text-muted">
        Short lessons that explain the ideas behind the news, from what a light-year is to how
        rockets reach orbit. No science background needed.
        <template v-if="lessonCount">{{ tracks.length }} paths, {{ lessonCount }} lessons.</template>
      </p>
    </header>

    <div v-if="error" role="alert" class="flex flex-col items-center gap-4 rounded-3xl bg-surface py-16 text-center">
      <p class="text-lg">{{ error.message }}</p>
      <button type="button" class="rounded-full bg-action text-on-action px-5 py-2 font-medium" @click="load">
        Try again
      </button>
    </div>

    <template v-else-if="loading">
      <div class="h-56 rounded-3xl bg-surface animate-pulse" aria-hidden="true"></div>
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <div v-for="n in 5" :key="n" class="h-44 rounded-2xl bg-surface animate-pulse" aria-hidden="true"></div>
      </div>
    </template>

    <template v-else>
      <!-- The recommended first path -->
      <RouterLink
        v-if="startTrack"
        :to="`/learn/${startTrack.slug}`"
        class="group relative block overflow-hidden rounded-3xl bg-surface p-6 sm:p-8 ring-1 ring-brand/30 transition hover:ring-brand focus-visible:outline-2 focus-visible:outline-link">
        <div class="absolute -right-24 -top-24 size-72 rounded-full bg-brand/15 blur-3xl" aria-hidden="true"></div>
        <div class="relative flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
          <div class="space-y-3 max-w-2xl">
            <span class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-3 py-1 text-xs font-medium">
              <i class="pi pi-flag-fill" style="font-size: 0.7rem" aria-hidden="true"></i>
              Start here
            </span>
            <h2 class="font-display text-3xl font-bold">{{ startTrack.title }}</h2>
            <p class="text-lg text-muted">{{ startTrack.summary }}</p>
            <ol class="flex flex-wrap gap-2 pt-1 text-sm">
              <li
                v-for="(lesson, index) in startTrack.lessons"
                :key="lesson.slug"
                :class="['rounded-full px-3 py-1', isCompleted(startTrack.slug, lesson.slug) ? 'bg-brand/15 text-brand' : 'bg-page']">
                <i
                  v-if="isCompleted(startTrack.slug, lesson.slug)"
                  class="pi pi-check mr-1"
                  style="font-size: 0.7rem"
                  aria-label="Finished:"></i>
                <span v-else class="text-muted">{{ index + 1 }}.</span>
                {{ lesson.title }}
              </li>
            </ol>
            <TrackProgress :track="startTrack" class="max-w-sm pt-1" />
          </div>
          <span class="inline-flex shrink-0 items-center gap-2 self-start rounded-full bg-action text-on-action px-6 py-3 font-medium transition-transform group-hover:-translate-y-0.5 lg:self-center">
            {{ completedIn(startTrack) ? "Continue learning" : "Start learning" }}
            <i class="pi pi-arrow-right" style="font-size: 0.8rem" aria-hidden="true"></i>
          </span>
        </div>
      </RouterLink>

      <!-- Everything else -->
      <section aria-labelledby="paths-heading" class="space-y-5">
        <h2 id="paths-heading" class="font-display text-2xl font-bold">Then explore further</h2>
        <ul class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          <li v-for="track in otherTracks" :key="track.slug">
            <RouterLink
              :to="`/learn/${track.slug}`"
              class="group flex h-full flex-col gap-4 rounded-2xl bg-surface p-5 ring-1 ring-transparent transition hover:ring-brand/60 focus-visible:outline-2 focus-visible:outline-link">
              <span class="grid size-11 place-items-center rounded-xl bg-brand/15 text-brand transition-colors group-hover:bg-action group-hover:text-on-action">
                <i :class="`pi ${track.icon}`" aria-hidden="true"></i>
              </span>
              <div class="space-y-1.5">
                <h3 class="font-display text-xl font-bold">{{ track.title }}</h3>
                <p class="text-muted">{{ track.summary }}</p>
              </div>
              <TrackProgress :track="track" class="mt-auto" />
              <p :class="['flex items-center justify-between text-sm font-medium', !completedIn(track) && 'mt-auto']">
                <span class="text-muted">{{ track.lessons.length }} lessons</span>
                <span class="inline-flex items-center gap-1.5 text-link">
                  Open
                  <i class="pi pi-arrow-right transition-transform group-hover:translate-x-0.5" style="font-size: 0.7rem" aria-hidden="true"></i>
                </span>
              </p>
            </RouterLink>
          </li>
        </ul>
      </section>

      <p class="text-sm text-muted">
        Lessons combine our short summaries with articles from
        <a href="https://simple.wikipedia.org/" target="_blank" rel="noopener noreferrer" class="underline underline-offset-2 hover:text-ink">Simple English Wikipedia</a>
        and
        <a href="https://science.nasa.gov/" target="_blank" rel="noopener noreferrer" class="underline underline-offset-2 hover:text-ink">NASA Science</a>.
        Each lesson credits its sources.
      </p>
    </template>
  </section>
</template>
