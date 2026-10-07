<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { formatRelativeDate } from "@/utils/format";

const props = defineProps({
  /** @type {import("@/api/content").ContentItem} */
  item: {
    type: Object,
    required: true,
  },
  /** Title only, no summary — for the landing page collage. */
  compact: {
    type: Boolean,
    default: false,
  },
});

const imageFailed = ref(false);

const meta = computed(() =>
  [props.item.publisher, formatRelativeDate(props.item.publishedAt)]
    .filter(Boolean)
    .join(" · "),
);
</script>

<template>
  <RouterLink
    :to="`/read/${item.id}`"
    class="group flex flex-col bg-surface shadow-xl rounded-sm overflow-hidden focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-link">
    <figure
      v-if="item.imageUrl && !imageFailed"
      class="aspect-video overflow-hidden bg-subtle">
      <img
        :src="item.imageUrl"
        alt=""
        loading="lazy"
        class="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
        @error="imageFailed = true" />
    </figure>

    <div class="p-4 space-y-2">
      <p v-if="meta" class="text-xs font-medium text-muted truncate">
        {{ meta }}
      </p>

      <h3
        :class="`font-bold leading-tight line-clamp-3 group-hover:text-link transition-colors ${compact ? 'text-lg' : 'text-xl'}`">
        {{ item.title }}
      </h3>

      <p v-if="!compact && item.summary" class="text-muted line-clamp-3">
        {{ item.summary }}
      </p>
    </div>
  </RouterLink>
</template>
