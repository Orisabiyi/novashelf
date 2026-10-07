<script setup>
import { computed, onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import logoImg from "@/assets/novashelf-logo-ord.svg";
import { getFeed } from "@/api/content";
import ContentCard from "@/components/content/ContentCard.vue";
import ContentCardSkeleton from "@/components/content/ContentCardSkeleton.vue";

// The collage is 3 + 2 + 3 cards; the middle column is offset for a staggered look.
const COLUMN_SIZES = [3, 2, 3];

const articles = ref([]);
const loading = ref(true);

const columns = computed(() => {
  let start = 0;
  return COLUMN_SIZES.map((size) => {
    const column = articles.value.slice(start, start + size);
    start += size;
    return column;
  });
});

onMounted(async () => {
  try {
    const feed = await getFeed();
    articles.value = feed.items.filter((item) => item.imageUrl);
  } catch {
    // The collage is decorative; the page still works without it.
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <header class="flex-1 flex items-center gap-10 p-10 2xl:py-5 2xl:px-48">
    <div class="w-full lg:w-2/5 flex flex-col items-start gap-5">
      <div class="flex items-center gap-2">
        <figure class="w-12 h-auto flex items-center justify-center">
          <img
            :src="logoImg"
            alt="novashelf logo with no background"
            class="inline-block w-full h-full" />
        </figure>
        <h3 class="font-medium">Powered by NASA</h3>
      </div>

      <h2 class="text-5xl lg:text-6xl text-brand font-bold flex flex-col">
        Space made simple
      </h2>

      <p class="text-xl lg:w-5/6">
        The universe is fascinating you shouldn't need a PhD to enjoy it.
        Explore NASA's best content, simplified for the curious.
      </p>

      <div class="flex flex-wrap items-center gap-4">
        <RouterLink
          to="/explore"
          class="bg-action px-4 py-1 rounded-md text-on-action text-lg"
          >Start reading</RouterLink
        >
        <RouterLink to="/sign-up" class="text-lg font-medium hover:text-link"
          >Sign up for
          <span class="product-name font-bold">novashelf</span>
        </RouterLink>
      </div>
    </div>

    <!-- Absolutely positioned so the cards never stretch the hero past the viewport. -->
    <div
      class="hidden lg:block relative flex-1 self-stretch overflow-hidden"
      aria-label="Latest articles">
      <div class="absolute inset-0 flex items-center gap-4">
        <div
          v-for="(column, index) in columns"
          :key="index"
          :class="`w-64 shrink-0 space-y-4 ${index === 1 ? 'translate-y-5' : ''}`">
          <template v-if="loading">
            <ContentCardSkeleton
              v-for="n in COLUMN_SIZES[index]"
              :key="n"
              variant="compact" />
          </template>
          <ContentCard
            v-for="article in column"
            v-else
            :key="article.id"
            :item="article"
            variant="compact" />
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
@import url("https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@300..700&display=swap");

h3,
.product-name {
  font-family: "Space Grotesk", sans-serif;
}
</style>
