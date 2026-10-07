<script setup>
import { onBeforeUnmount, onMounted, ref } from "vue";
import {
  FONT_SIZES,
  THEMES,
  WIDTHS,
  useReaderPreferences,
} from "@/composables/useReaderPreferences";

const preferences = useReaderPreferences();
const open = ref(false);
const root = ref(null);

const groups = [
  { key: "fontSize", label: "Text size", options: FONT_SIZES },
  { key: "width", label: "Line width", options: WIDTHS },
  { key: "theme", label: "Theme", options: THEMES },
];

function onDocumentClick(event) {
  if (open.value && !root.value?.contains(event.target)) open.value = false;
}

function onKeydown(event) {
  if (event.key === "Escape") open.value = false;
}

onMounted(() => {
  document.addEventListener("click", onDocumentClick);
  document.addEventListener("keydown", onKeydown);
});

onBeforeUnmount(() => {
  document.removeEventListener("click", onDocumentClick);
  document.removeEventListener("keydown", onKeydown);
});

const segmentClass = (selected) => [
  "flex-1 inline-flex items-center justify-center gap-1.5 rounded-full px-3 py-1.5 text-sm font-medium transition-colors",
  selected ? "bg-action text-on-action" : "text-muted hover:text-ink",
];
</script>

<template>
  <div ref="root" class="relative">
    <button
      type="button"
      class="inline-flex items-center gap-2 rounded-full border border-line-strong px-3 py-1.5 text-sm font-medium text-muted hover:text-ink hover:border-ink transition-colors"
      :aria-expanded="open"
      aria-controls="reader-settings"
      @click="open = !open">
      <span class="font-display font-bold leading-none" aria-hidden="true">Aa</span>
      Reading settings
    </button>

    <div
      v-if="open"
      id="reader-settings"
      role="dialog"
      aria-label="Reading settings"
      class="absolute right-0 top-full z-10 mt-2 w-72 space-y-4 rounded-2xl border border-line-strong bg-surface p-4 shadow-2xl shadow-black/40">
      <fieldset v-for="group in groups" :key="group.key" class="space-y-2">
        <legend class="text-xs font-medium uppercase tracking-wider text-muted">
          {{ group.label }}
        </legend>
        <div class="flex gap-1 rounded-full bg-page p-1">
          <button
            v-for="option in group.options"
            :key="option.value"
            type="button"
            :class="segmentClass(preferences[group.key] === option.value)"
            :aria-pressed="preferences[group.key] === option.value"
            @click="preferences[group.key] = option.value">
            <i v-if="option.icon" :class="`pi ${option.icon}`" style="font-size: 0.75rem" aria-hidden="true"></i>
            {{ option.label }}
          </button>
        </div>
      </fieldset>
    </div>
  </div>
</template>
