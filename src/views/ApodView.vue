<script setup>
import { computed, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getApod, getApodArchive } from "@/api/content";
import PhotoTile from "@/components/content/PhotoTile.vue";
import { useInfiniteScroll } from "@/composables/useInfiniteScroll";
import { usePagedList } from "@/composables/usePagedList";
import {
  APOD_FIRST_DATE,
  addDays,
  apodDate,
  formatFullDate,
  formatShortDate,
  isIsoDate,
  randomDateBetween,
  todayIso,
} from "@/utils/dates";
import { imageAt, imageSrcset } from "@/utils/images";

const route = useRoute();
const router = useRouter();

// The day lives in the URL (?date=2001-07-04) so it can be shared and Back works.
const requestedDate = computed(() =>
  isIsoDate(route.query.date) ? String(route.query.date) : null,
);

/** @type {import("vue").Ref<import("@/api/content").ContentItem | null>} */
const picture = ref(null);
const loading = ref(true);
/** @type {import("vue").Ref<import("@/api/client").ApiError | null>} */
const error = ref(null);
const imageLoaded = ref(false);
const imageFailed = ref(false);
// The newest published day; "next" stops there (APOD posts on US time, so it can lag today).
const latestDate = ref(null);

let requestId = 0;

async function load(date) {
  const id = ++requestId;
  loading.value = true;
  error.value = null;
  imageLoaded.value = false;
  imageFailed.value = false;
  try {
    const result = await getApod(date);
    if (id !== requestId) return;
    picture.value = result;
    if (!date) latestDate.value = apodDate(result.id);
  } catch (e) {
    if (id === requestId) {
      error.value = e;
      picture.value = null;
    }
  } finally {
    if (id === requestId) loading.value = false;
  }
}

watch(requestedDate, (date) => load(date), { immediate: true });

const currentDate = computed(() => requestedDate.value ?? apodDate(picture.value?.id));
const maxDate = computed(() => latestDate.value ?? todayIso());
const canGoBack = computed(() => !!currentDate.value && currentDate.value > APOD_FIRST_DATE);
const canGoForward = computed(() => !!currentDate.value && currentDate.value < maxDate.value);

function goTo(date) {
  if (!isIsoDate(date)) return;
  const clamped = date < APOD_FIRST_DATE ? APOD_FIRST_DATE : date > maxDate.value ? maxDate.value : date;
  router.push({ query: { date: clamped } });
  window.scrollTo({ top: 0, behavior: "smooth" });
}

const goToday = () => {
  router.push({ query: {} });
  window.scrollTo({ top: 0, behavior: "smooth" });
};
const surprise = () => goTo(randomDateBetween(APOD_FIRST_DATE, maxDate.value));

// The archive below: newest first, loading more as you scroll.
const archive = usePagedList(ref((page, options) => getApodArchive({ page }, options)));
const sentinel = ref(null);
useInfiniteScroll(sentinel, archive);

// The archive's first entry is the latest day, which also tells us where "next" ends.
watch(
  () => archive.items.value[0],
  (first) => first && !latestDate.value && (latestDate.value = apodDate(first.id)),
);

const paragraphs = computed(() =>
  (picture.value?.summary ?? "").split(/\n{2,}/).filter(Boolean),
);
</script>

<template>
  <section class="px-6 py-8 lg:px-10 2xl:px-48 space-y-10">
    <header class="space-y-3 max-w-3xl">
      <p class="text-sm font-medium text-link">Astronomy Picture of the Day</p>
      <h1 class="font-display text-4xl lg:text-5xl font-bold tracking-tight">Picture of the day</h1>
      <p class="text-lg text-muted">
        Every day since June 1995, NASA has featured one image or video of our universe,
        explained by a professional astronomer.
      </p>
    </header>

    <!-- Day navigation -->
    <nav aria-label="Choose a day" class="flex flex-wrap items-center gap-2">
      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-full border border-line-strong px-4 py-2 text-sm font-medium transition-colors hover:border-ink disabled:opacity-40 disabled:hover:border-line-strong"
        :disabled="!canGoBack"
        @click="goTo(addDays(currentDate, -1))">
        <i class="pi pi-arrow-left" style="font-size: 0.7rem" aria-hidden="true"></i>
        Previous day
      </button>

      <label class="relative">
        <span class="sr-only">Pick a date</span>
        <input
          id="apod-date"
          type="date"
          :min="APOD_FIRST_DATE"
          :max="maxDate"
          :value="currentDate"
          class="rounded-full border border-line-strong bg-surface px-4 py-2 text-sm font-medium focus:outline-2 focus:outline-link"
          @change="goTo($event.target.value)" />
      </label>

      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-full border border-line-strong px-4 py-2 text-sm font-medium transition-colors hover:border-ink disabled:opacity-40 disabled:hover:border-line-strong"
        :disabled="!canGoForward"
        @click="goTo(addDays(currentDate, 1))">
        Next day
        <i class="pi pi-arrow-right" style="font-size: 0.7rem" aria-hidden="true"></i>
      </button>

      <span class="hidden sm:block w-px h-6 bg-line-strong mx-1" aria-hidden="true"></span>

      <button
        v-if="requestedDate"
        type="button"
        class="rounded-full px-4 py-2 text-sm font-medium text-muted hover:text-ink"
        @click="goToday">
        Latest
      </button>
      <button
        type="button"
        class="inline-flex items-center gap-2 rounded-full bg-action text-on-action px-4 py-2 text-sm font-medium"
        @click="surprise">
        <i class="pi pi-sparkles" style="font-size: 0.8rem" aria-hidden="true"></i>
        Surprise me
      </button>
    </nav>

    <!-- The picture -->
    <div
      v-if="error"
      role="alert"
      class="flex flex-col items-center gap-4 rounded-3xl bg-surface px-6 py-16 text-center">
      <i class="pi pi-calendar-times text-brand" style="font-size: 2.5rem" aria-hidden="true"></i>
      <h2 class="font-display text-2xl font-bold">
        {{ error.isNotFound ? "No picture that day" : "This picture didn't load" }}
      </h2>
      <p class="max-w-md text-muted">
        <template v-if="error.isNotFound">
          NASA didn't publish a Picture of the Day on {{ formatFullDate(requestedDate) }}.
        </template>
        <template v-else>{{ error.message }}</template>
      </p>
      <div class="flex flex-wrap justify-center gap-3">
        <button
          v-if="error.isNotFound"
          type="button"
          class="rounded-full bg-action text-on-action px-5 py-2 font-medium"
          @click="surprise">
          Try another day
        </button>
        <button
          v-else
          type="button"
          class="rounded-full bg-action text-on-action px-5 py-2 font-medium"
          @click="load(requestedDate)">
          Try again
        </button>
        <button
          type="button"
          class="rounded-full border border-line-strong px-5 py-2 font-medium hover:border-ink"
          @click="goToday">
          See the latest
        </button>
      </div>
    </div>

    <article
      v-else
      class="grid gap-8 lg:grid-cols-[minmax(0,1.5fr)_minmax(0,1fr)] lg:items-start"
      :aria-busy="loading">
      <figure
        :class="[
          'relative overflow-hidden rounded-3xl bg-black ring-1 ring-ink/10 shadow-2xl shadow-black/40',
          (!imageLoaded || loading) && 'aspect-[4/3]',
        ]">
        <template v-if="picture && picture.imageUrl && !imageFailed">
          <img
            :key="picture.id"
            :src="imageAt(picture.imageUrl, 1600)"
            :srcset="imageSrcset(picture.imageUrl, [960, 1280, 1600, 2048])"
            sizes="(min-width: 1024px) 60vw, 100vw"
            :alt="picture.title"
            fetchpriority="high"
            :class="[
              'mx-auto block max-h-[75vh] w-full object-contain transition-opacity duration-500',
              imageLoaded && !loading ? 'opacity-100' : 'opacity-0',
            ]"
            @load="imageLoaded = true"
            @error="imageFailed = true" />
        </template>
        <div
          v-else-if="!loading"
          class="absolute inset-0 grid place-items-center p-8 text-center text-white/70">
          This day's picture isn't available here. It may be a video or an image NASA no
          longer hosts.
        </div>
        <div
          v-if="loading || (!imageLoaded && picture?.imageUrl && !imageFailed)"
          class="absolute inset-0 animate-pulse bg-white/5"
          aria-hidden="true"></div>
      </figure>

      <div class="space-y-5 lg:sticky lg:top-28">
        <template v-if="loading">
          <div class="h-4 w-40 rounded bg-surface animate-pulse"></div>
          <div class="h-9 w-3/4 rounded bg-surface animate-pulse"></div>
          <div v-for="n in 6" :key="n" class="h-4 rounded bg-surface animate-pulse"></div>
        </template>
        <template v-else-if="picture">
          <p class="text-sm font-medium text-muted">{{ formatFullDate(currentDate) }}</p>
          <h2 class="font-display text-3xl lg:text-4xl font-bold leading-tight tracking-tight text-balance">
            {{ picture.title }}
          </h2>
          <p v-if="picture.publisher" class="text-sm text-muted">
            <span class="font-medium text-ink">Credit:</span> {{ picture.publisher }}
          </p>
          <div class="space-y-4 text-lg leading-relaxed">
            <p v-for="(paragraph, index) in paragraphs" :key="index">{{ paragraph }}</p>
          </div>
          <a
            v-if="picture.url"
            :href="picture.url"
            target="_blank"
            rel="noopener noreferrer"
            class="inline-flex items-center gap-2 rounded-full border border-line-strong px-4 py-2 text-sm font-medium hover:border-ink">
            View on NASA
            <i class="pi pi-external-link" style="font-size: 0.7rem" aria-hidden="true"></i>
          </a>
        </template>
      </div>
    </article>

    <!-- Archive -->
    <section aria-labelledby="archive-heading" class="space-y-5 pt-6">
      <div>
        <p class="text-sm font-medium text-link">Back to 1995</p>
        <h2 id="archive-heading" class="font-display text-3xl font-bold">From the archive</h2>
      </div>

      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 lg:gap-4">
        <PhotoTile
          v-for="item in archive.items.value"
          :key="item.id"
          :item="item"
          :to="{ path: '/apod', query: { date: apodDate(item.id) } }"
          :label="formatShortDate(apodDate(item.id))"
          :selected="apodDate(item.id) === currentDate" />
        <template v-if="archive.loading.value">
          <div
            v-for="n in 8"
            :key="`loading-${n}`"
            class="aspect-square rounded-2xl bg-surface animate-pulse"
            aria-hidden="true"></div>
        </template>
      </div>

      <div
        v-if="archive.error.value"
        role="alert"
        class="flex flex-wrap items-center justify-center gap-4 text-center">
        <p>{{ archive.error.value.message }}</p>
        <button
          type="button"
          class="rounded-full border border-link px-4 py-1.5 font-medium text-link"
          @click="archive.loadMore">
          Try again
        </button>
      </div>
      <p v-if="!archive.hasMore.value && archive.items.value.length" class="text-center text-muted">
        That's every picture since June 16, 1995.
      </p>
      <div ref="sentinel" aria-hidden="true"></div>
    </section>
  </section>
</template>
