<script setup>
import { onMounted, ref } from "vue";
import { RouterLink } from "vue-router";
import { getTopics } from "@/api/content";
import { TOPIC_ICONS } from "@/utils/topics";

const topics = ref([]);

onMounted(async () => {
  try {
    topics.value = await getTopics();
  } catch {
    // Without topics the section simply doesn't render.
  }
});
</script>

<template>
  <ul v-if="topics.length" class="grid grid-cols-2 sm:grid-cols-4 gap-3">
    <li v-for="topic in topics" :key="topic.slug">
      <RouterLink
        :to="{ path: '/explore', query: { topic: topic.slug } }"
        class="group flex items-center gap-3 rounded-2xl bg-surface p-3 pr-4 ring-1 ring-transparent transition hover:ring-brand/60 focus-visible:outline-2 focus-visible:outline-link">
        <span
          class="size-10 shrink-0 grid place-items-center rounded-xl bg-brand/15 text-brand transition-colors group-hover:bg-brand group-hover:text-on-action">
          <i :class="`pi ${TOPIC_ICONS[topic.slug] ?? 'pi-compass'}`" aria-hidden="true"></i>
        </span>
        <span class="font-medium leading-tight">{{ topic.name }}</span>
        <i
          class="pi pi-arrow-right ml-auto text-muted opacity-0 -translate-x-1 transition group-hover:opacity-100 group-hover:translate-x-0"
          style="font-size: 0.75rem"
          aria-hidden="true"></i>
      </RouterLink>
    </li>
  </ul>
</template>
