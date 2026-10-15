<script setup>
import { computed } from "vue";
import { useLessonProgress } from "@/composables/useLessonProgress";

const props = defineProps({
  track: {
    type: Object,
    required: true,
  },
});

const { completedIn } = useLessonProgress();
const done = computed(() => completedIn(props.track));
const total = computed(() => props.track.lessons.length);
</script>

<template>
  <div v-if="done > 0" class="space-y-1.5">
    <p class="flex items-center gap-1.5 text-sm font-medium">
      <i v-if="done === total" class="pi pi-check-circle text-brand" style="font-size: 0.85rem" aria-hidden="true"></i>
      <span :class="done === total ? 'text-brand' : 'text-muted'">
        {{ done === total ? "Finished" : `${done} of ${total} finished` }}
      </span>
    </p>
    <div
      class="h-1.5 overflow-hidden rounded-full bg-line"
      role="progressbar"
      :aria-valuenow="done"
      aria-valuemin="0"
      :aria-valuemax="total"
      :aria-label="`${done} of ${total} lessons finished`">
      <div class="h-full rounded-full bg-brand transition-[width] duration-500" :style="{ width: `${(done / total) * 100}%` }"></div>
    </div>
  </div>
</template>
