<script setup>
import { computed, ref } from "vue";
import { RouterLink } from "vue-router";
import { formatRelativeDate } from "@/utils/format";
import { itemTag } from "@/utils/topics";

const props = defineProps({
  /** @type {import("@/api/content").ContentItem} */
  item: {
    type: Object,
    required: true,
  },
  /**
   * - stacked: image above text (default)
   * - feature: spans two grid columns on large screens, image beside a bigger headline
   * - compact: title only, for the landing page collage
   */
  variant: {
    type: String,
    default: "stacked",
    validator: (value) => ["stacked", "feature", "compact"].includes(value),
  },
});

const imageFailed = ref(false);
const hasImage = computed(() => props.item.imageUrl && !imageFailed.value);
const tag = computed(() => itemTag(props.item));

const meta = computed(() =>
  [props.item.publisher, formatRelativeDate(props.item.publishedAt)]
    .filter(Boolean)
    .join(" · "),
);

const isFeature = computed(() => props.variant === "feature");
const isCompact = computed(() => props.variant === "compact");
</script>

<template>
  <RouterLink
    :to="`/read/${item.id}`"
    :class="[
      'group h-full flex flex-col gap-4 bg-surface rounded-2xl p-3 transition-colors hover:bg-surface/70 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-link',
      isFeature && hasImage && 'lg:col-span-2 lg:grid lg:grid-cols-2 lg:gap-6',
      isFeature && !hasImage && 'lg:col-span-2',
    ]">
    <figure
      v-if="hasImage"
      :class="[
        'overflow-hidden rounded-xl bg-subtle',
        isFeature ? 'aspect-video lg:aspect-auto lg:h-full lg:min-h-72' : 'aspect-video',
      ]">
      <img
        :src="item.imageUrl"
        alt=""
        loading="lazy"
        class="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
        @error="imageFailed = true" />
    </figure>

    <div
      :class="[
        'flex-1 flex flex-col items-start gap-3 px-2 pb-2',
        isFeature && 'lg:justify-center lg:py-4 lg:pr-4',
      ]">
      <span
        class="inline-flex items-center gap-1.5 rounded-full bg-slate text-on-slate px-2.5 py-1 text-xs font-medium">
        <i :class="`pi ${tag.icon}`" style="font-size: 0.75rem" aria-hidden="true"></i>
        {{ tag.label }}
      </span>

      <h3
        :class="[
          'font-bold tracking-tight text-balance group-hover:text-link transition-colors',
          isFeature ? 'text-2xl lg:text-3xl leading-[1.1]' : 'text-xl leading-snug',
          isCompact ? 'text-lg line-clamp-3' : 'line-clamp-4',
        ]">
        {{ item.title }}
      </h3>

      <p
        v-if="!isCompact && item.summary"
        :class="['text-sm text-muted', isFeature ? 'line-clamp-4' : 'line-clamp-3']">
        {{ item.summary }}
      </p>

      <!-- Bottom-aligned in small cards so buttons line up across a row; in a feature card
           it follows the text, which is vertically centred beside the image. -->
      <div
        v-if="!isCompact"
        :class="[
          'mt-auto pt-1 w-full flex flex-wrap items-center justify-between gap-x-4 gap-y-2',
          isFeature && 'lg:mt-2',
        ]">
        <!-- The whole card is the link; this is its visual call to action, not a nested control. -->
        <span
          class="inline-flex items-center gap-1.5 rounded-full border border-line-strong px-3 py-1 text-xs font-medium transition-colors group-hover:border-link group-hover:text-link">
          <i class="pi pi-arrow-right" style="font-size: 0.7rem" aria-hidden="true"></i>
          Read
        </span>
        <span v-if="meta" class="text-xs text-muted truncate">{{ meta }}</span>
      </div>

      <p v-else-if="meta" class="text-xs text-muted truncate">{{ meta }}</p>
    </div>
  </RouterLink>
</template>
