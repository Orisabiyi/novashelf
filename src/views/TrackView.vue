<script setup>
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import { getTracks } from "@/api/learn";

const route = useRoute();
const tracks = ref([]);
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
watch(() => route.params.track, load, { immediate: true });

const index = computed(() => tracks.value.findIndex((t) => t.slug === route.params.track));
const track = computed(() => tracks.value[index.value] ?? null);
const nextTrack = computed(() => tracks.value[index.value + 1] ?? null);

watch(track, (t) => t && (document.title = `${t.title} · Learn · Novashelf`));
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48">
    <div class="mx-auto max-w-3xl space-y-8">
      <RouterLink to="/learn" class="inline-flex items-center gap-2 text-sm font-medium text-muted hover:text-ink">
        <i class="pi pi-arrow-left" style="font-size: 0.7rem" aria-hidden="true"></i>
        All learning paths
      </RouterLink>

      <div v-if="error" role="alert" class="flex flex-col items-center gap-4 rounded-3xl bg-surface py-16 text-center">
        <p class="text-lg">{{ error.message }}</p>
        <button type="button" class="rounded-full bg-action text-on-action px-5 py-2 font-medium" @click="load">Try again</button>
      </div>

      <div v-else-if="loading" class="space-y-4" aria-hidden="true">
        <div class="h-12 w-2/3 rounded-xl bg-surface animate-pulse"></div>
        <div v-for="n in 4" :key="n" class="h-20 rounded-2xl bg-surface animate-pulse"></div>
      </div>

      <div v-else-if="!track" class="flex flex-col items-center gap-4 rounded-3xl bg-surface py-16 text-center">
        <h1 class="font-display text-2xl font-bold">We couldn't find that learning path</h1>
        <RouterLink to="/learn" class="rounded-full bg-action text-on-action px-5 py-2 font-medium">See all paths</RouterLink>
      </div>

      <template v-else>
        <header class="flex items-start gap-4">
          <span class="grid size-14 shrink-0 place-items-center rounded-2xl bg-brand/15 text-brand" aria-hidden="true">
            <i :class="`pi ${track.icon}`" style="font-size: 1.4rem"></i>
          </span>
          <div class="space-y-2">
            <p class="text-sm font-medium text-link">
              <template v-if="track.startHere">Start here · </template>{{ track.lessons.length }} lessons
            </p>
            <h1 class="font-display text-4xl font-bold tracking-tight text-balance">{{ track.title }}</h1>
            <p class="text-lg text-muted">{{ track.summary }}</p>
          </div>
        </header>

        <ol class="space-y-3">
          <li v-for="(lesson, i) in track.lessons" :key="lesson.slug">
            <RouterLink
              :to="`/learn/${track.slug}/${lesson.slug}`"
              class="group flex items-center gap-4 rounded-2xl bg-surface p-4 sm:p-5 ring-1 ring-transparent transition hover:ring-brand/60 focus-visible:outline-2 focus-visible:outline-link">
              <span class="grid size-10 shrink-0 place-items-center rounded-full border border-line-strong font-display font-bold tabular-nums transition-colors group-hover:border-brand group-hover:text-brand">
                {{ i + 1 }}
              </span>
              <span class="min-w-0 flex-1">
                <span class="block font-display text-lg font-bold">{{ lesson.title }}</span>
                <span class="block text-muted">{{ lesson.summary }}</span>
              </span>
              <i class="pi pi-arrow-right text-muted transition-transform group-hover:translate-x-0.5 group-hover:text-link" style="font-size: 0.8rem" aria-hidden="true"></i>
            </RouterLink>
          </li>
        </ol>

        <div class="flex flex-wrap items-center justify-between gap-4">
          <RouterLink
            :to="`/learn/${track.slug}/${track.lessons[0].slug}`"
            class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-6 py-3 font-medium">
            Start with lesson 1
            <i class="pi pi-arrow-right" style="font-size: 0.8rem" aria-hidden="true"></i>
          </RouterLink>
          <RouterLink v-if="nextTrack" :to="`/learn/${nextTrack.slug}`" class="text-sm font-medium text-muted hover:text-ink">
            Next path: {{ nextTrack.title }} →
          </RouterLink>
        </div>
      </template>
    </div>
  </section>
</template>
