import { ref, watch } from "vue";
import { getFeed } from "@/api/content";

/**
 * Paged article feed for a topic, accumulating pages as the reader scrolls.
 * Changing the topic aborts any in-flight request and starts over.
 *
 * @param {import("vue").Ref<string | null>} topic topic slug, or null for everything
 */
export function useFeed(topic) {
  const items = ref([]);
  const page = ref(0);
  const hasMore = ref(true);
  const loading = ref(false);
  /** @type {import("vue").Ref<import("@/api/client").ApiError | null>} */
  const error = ref(null);

  let controller = null;

  async function loadMore() {
    if (loading.value || !hasMore.value) return;

    const current = new AbortController();
    controller = current;
    loading.value = true;
    error.value = null;

    try {
      const result = await getFeed(
        { topic: topic.value, page: page.value + 1 },
        { signal: current.signal },
      );

      // New articles shift offset-based pages while you read, which can repeat an item.
      const seen = new Set(items.value.map((item) => item.id));
      items.value.push(...result.items.filter((item) => !seen.has(item.id)));
      page.value = result.page;
      hasMore.value = result.hasMore;
    } catch (e) {
      if (e.name !== "AbortError") error.value = e;
    } finally {
      // A topic change may have replaced this request; only the latest one owns `loading`.
      if (controller === current) loading.value = false;
    }
  }

  function reset() {
    controller?.abort();
    controller = null;
    items.value = [];
    page.value = 0;
    hasMore.value = true;
    loading.value = false;
    error.value = null;
  }

  watch(topic, () => {
    reset();
    loadMore();
  });

  return { items, hasMore, loading, error, loadMore, reset };
}
