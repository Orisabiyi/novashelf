<script setup>
import { computed, ref, watch } from "vue";
import { RouterLink, useRoute } from "vue-router";
import DOMPurify from "dompurify";
import { getFeed } from "@/api/content";
import { getLesson } from "@/api/learn";
import ContentCard from "@/components/content/ContentCard.vue";
import ReaderSettings from "@/components/reader/ReaderSettings.vue";
import { FONT_SIZES, WIDTHS, useReaderPreferences } from "@/composables/useReaderPreferences";

const route = useRoute();
const preferences = useReaderPreferences();

const lesson = ref(null);
const loading = ref(true);
const error = ref(null);
const news = ref([]);

let requestId = 0;

async function load() {
  const id = ++requestId;
  loading.value = true;
  error.value = null;
  news.value = [];
  try {
    const result = await getLesson(route.params.track, route.params.lesson);
    if (id !== requestId) return;
    lesson.value = result;
    document.title = `${result.lesson.title} · Learn · Novashelf`;
    loadNews(result.lesson.topic, id);
  } catch (e) {
    if (id === requestId) {
      error.value = e;
      lesson.value = null;
    }
  } finally {
    if (id === requestId) loading.value = false;
  }
}

// Current articles on the lesson's subject, to connect the basics to the news.
async function loadNews(topic, id) {
  if (!topic) return;
  try {
    const feed = await getFeed({ topic });
    if (id === requestId) news.value = feed.items.filter((item) => item.imageUrl).slice(0, 3);
  } catch {
    // Optional extra; the lesson stands on its own.
  }
}

watch(() => [route.params.track, route.params.lesson], load, { immediate: true });

const maxWidth = computed(() => WIDTHS.find((w) => w.value === preferences.width)?.maxWidth ?? "68ch");
const proseSize = computed(() => FONT_SIZES.find((f) => f.value === preferences.fontSize)?.proseClass ?? "prose-lg");

const SECTION_LABELS = { wikipedia: "The basics", nasa: "From NASA" };
const SOURCE_NAMES = { wikipedia: "Wikipedia", nasa: "NASA" };

// The server sanitises; this is a second, independent layer before v-html.
const sanitize = (html) =>
  DOMPurify.sanitize(html, { ADD_ATTR: ["target", "loading", "decoding", "referrerpolicy", "srcset", "sizes"] });

const totalMinutes = computed(() =>
  (lesson.value?.sections ?? []).reduce((sum, s) => sum + s.readingMinutes, 0) + 1,
);
</script>

<template>
  <article class="px-6 pt-8 pb-20 lg:px-10">
    <div class="mx-auto transition-[max-width] duration-300" :style="{ maxWidth }">
      <div class="mb-8 flex items-center justify-between gap-4">
        <RouterLink
          :to="`/learn/${route.params.track}`"
          class="inline-flex items-center gap-2 rounded-full px-3 py-1.5 -ml-3 text-sm font-medium text-muted hover:text-ink hover:bg-surface">
          <i class="pi pi-arrow-left" style="font-size: 0.75rem" aria-hidden="true"></i>
          {{ lesson?.track.title ?? "Learning path" }}
        </RouterLink>
        <ReaderSettings />
      </div>

      <div v-if="error" role="alert" class="flex flex-col items-center gap-4 py-16 text-center">
        <i class="pi pi-exclamation-circle text-brand" style="font-size: 2.5rem" aria-hidden="true"></i>
        <h1 class="font-display text-2xl font-bold">
          {{ error.isNotFound ? "We couldn't find that lesson" : "This lesson didn't load" }}
        </h1>
        <p v-if="!error.isNotFound" class="text-muted">{{ error.message }}</p>
        <div class="flex flex-wrap justify-center gap-3">
          <button v-if="!error.isNotFound" type="button" class="rounded-full bg-action text-on-action px-5 py-2 font-medium" @click="load">
            Try again
          </button>
          <RouterLink to="/learn" class="rounded-full border border-line-strong px-5 py-2 font-medium hover:border-ink">
            All learning paths
          </RouterLink>
        </div>
      </div>

      <div v-else-if="loading && !lesson" class="space-y-6 animate-pulse" aria-busy="true" aria-label="Loading lesson">
        <div class="h-5 w-32 rounded bg-surface"></div>
        <div class="h-12 w-3/4 rounded-xl bg-surface"></div>
        <div class="h-40 rounded-2xl bg-surface"></div>
        <div v-for="n in 6" :key="n" class="h-4 rounded bg-surface"></div>
      </div>

      <template v-else-if="lesson">
        <div :class="['space-y-8 transition-opacity', loading && 'opacity-50']" :aria-busy="loading">
          <header class="space-y-3">
            <p class="text-sm font-medium text-link">
              Lesson {{ lesson.number }} of {{ lesson.lessonsInTrack }} · {{ totalMinutes }} min
            </p>
            <h1 class="font-display text-3xl sm:text-4xl lg:text-5xl font-bold leading-[1.05] tracking-tight text-balance">
              {{ lesson.lesson.title }}
            </h1>
            <p class="text-lg text-muted">{{ lesson.lesson.summary }}</p>
          </header>

          <!-- What to take away, in our own words -->
          <section aria-labelledby="key-ideas" class="rounded-3xl bg-surface p-6 ring-1 ring-brand/30">
            <h2 id="key-ideas" class="mb-4 flex items-center gap-2 font-display text-xl font-bold">
              <i class="pi pi-lightbulb text-brand" aria-hidden="true"></i>
              Key ideas
            </h2>
            <ol class="space-y-3">
              <li v-for="(idea, index) in lesson.lesson.keyIdeas" :key="index" class="flex gap-3 text-lg leading-relaxed">
                <span class="mt-1 grid size-6 shrink-0 place-items-center rounded-full bg-action text-on-action text-xs font-bold tabular-nums" aria-hidden="true">
                  {{ index + 1 }}
                </span>
                <span>{{ idea }}</span>
              </li>
            </ol>
          </section>

          <!-- Each source, credited -->
          <section
            v-for="section in lesson.sections"
            :key="section.sourceUrl"
            :aria-label="`${SECTION_LABELS[section.kind]}: ${section.title}`"
            class="space-y-4 border-t border-line pt-8">
            <p class="text-sm font-medium text-muted">
              <span class="text-link">{{ SECTION_LABELS[section.kind] }}</span>
              · {{ section.title }}
              <template v-if="section.readingMinutes"> · {{ section.readingMinutes }} min</template>
            </p>

            <div
              v-if="section.html"
              :class="proseSize"
              class="prose max-w-none prose-headings:font-display prose-img:rounded-xl prose-a:underline-offset-2 prose-figcaption:text-sm"
              v-html="sanitize(section.html)"></div>
            <p v-else class="rounded-2xl bg-surface p-5 text-muted">
              This part couldn't be loaded right now.
              <a :href="section.sourceUrl" target="_blank" rel="noopener noreferrer" class="font-medium text-link">
                Read it on {{ SOURCE_NAMES[section.kind] }}
              </a>
            </p>

            <a
              v-if="section.html && section.truncated"
              :href="section.sourceUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-2 rounded-full border border-line-strong px-4 py-1.5 text-sm font-medium hover:border-ink">
              Continue on {{ SOURCE_NAMES[section.kind] }}
              <i class="pi pi-external-link" style="font-size: 0.7rem" aria-hidden="true"></i>
            </a>

            <p class="text-xs text-muted">
              {{ section.attribution }}
              <a :href="section.sourceUrl" target="_blank" rel="noopener noreferrer" class="underline underline-offset-2 hover:text-ink">Source</a>
              <template v-if="section.licenseUrl">
                ·
                <a :href="section.licenseUrl" target="_blank" rel="noopener noreferrer" class="underline underline-offset-2 hover:text-ink">Licence</a>
              </template>
            </p>
          </section>

          <!-- Previous / next -->
          <nav aria-label="Lessons" class="grid gap-3 border-t border-line pt-8 sm:grid-cols-2">
            <RouterLink
              v-if="lesson.previous"
              :to="`/learn/${lesson.previous.track}/${lesson.previous.slug}`"
              class="group flex flex-col gap-1 rounded-2xl bg-surface p-4 ring-1 ring-transparent transition hover:ring-brand/60">
              <span class="text-xs font-medium text-muted">← Previous</span>
              <span class="font-display font-bold group-hover:text-link">{{ lesson.previous.title }}</span>
            </RouterLink>
            <span v-else></span>
            <RouterLink
              v-if="lesson.next"
              :to="`/learn/${lesson.next.track}/${lesson.next.slug}`"
              class="group flex flex-col gap-1 rounded-2xl bg-action/10 p-4 text-right ring-1 ring-brand/40 transition hover:ring-brand sm:col-start-2">
              <span class="text-xs font-medium text-muted">
                Next{{ lesson.next.track !== lesson.track.slug ? ` · new path: ${lesson.next.trackTitle}` : "" }} →
              </span>
              <span class="font-display font-bold group-hover:text-link">{{ lesson.next.title }}</span>
            </RouterLink>
            <RouterLink
              v-else
              to="/learn"
              class="flex flex-col gap-1 rounded-2xl bg-surface p-4 text-right sm:col-start-2">
              <span class="text-xs font-medium text-muted">You've finished every lesson</span>
              <span class="font-display font-bold">Back to all paths →</span>
            </RouterLink>
          </nav>
        </div>
      </template>
    </div>

    <!-- Connect the lesson to what's happening now -->
    <section
      v-if="news.length"
      aria-labelledby="in-the-news"
      class="mx-auto mt-14 max-w-6xl space-y-5">
      <h2 id="in-the-news" class="font-display text-2xl font-bold">In the news</h2>
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <ContentCard v-for="item in news" :key="item.id" :item="item" />
      </div>
    </section>
  </article>
</template>
