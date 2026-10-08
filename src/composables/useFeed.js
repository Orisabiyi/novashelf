import { computed } from "vue";
import { getFeed } from "@/api/content";
import { usePagedList } from "@/composables/usePagedList";

/**
 * Paged article feed for a topic, accumulating pages as the reader scrolls.
 * Changing the topic aborts any in-flight request and starts over.
 *
 * @param {import("vue").Ref<string | null>} topic topic slug, or null for everything
 */
export function useFeed(topic) {
  const source = computed(() => {
    const slug = topic.value;
    return (page, options) => getFeed({ topic: slug, page }, options);
  });
  return usePagedList(source);
}
