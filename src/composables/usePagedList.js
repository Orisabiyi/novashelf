import { ref, watch } from "vue";

export function usePagedList(source) {
  const items = ref([]);
  const page = ref(0);
  const hasMore = ref(true);
  const loading = ref(false);
  const error = ref(null);

  let controller = null;

  async function loadMore() {
    if (loading.value || !hasMore.value || !source.value) return;

    const current = new AbortController();
    controller = current;
    loading.value = true;
    error.value = null;

    try {
      const result = await source.value(page.value + 1, { signal: current.signal });

      // New items shift offset-based pages while you read, which can repeat an item.
      const seen = new Set(items.value.map((item) => item.id));
      items.value.push(...result.items.filter((item) => !seen.has(item.id)));
      page.value = result.page;
      hasMore.value = result.hasMore;
    } catch (e) {
      if (e.name !== "AbortError") error.value = e;
    } finally {
      // A newer request may have replaced this one; only the latest owns `loading`.
      if (controller === current) loading.value = false;
    }
  }

  function reset() {
    controller?.abort();
    controller = null;
    items.value = [];
    page.value = 0;
    hasMore.value = !!source.value;
    loading.value = false;
    error.value = null;
  }

  watch(source, () => {
    reset();
    loadMore();
  });

  return { items, hasMore, loading, error, loadMore, reset };
}
