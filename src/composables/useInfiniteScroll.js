import { watch } from "vue";
import { useInView } from "@/composables/useInView";

const MARGIN = 600;

export function useInfiniteScroll(sentinel, { loading, hasMore, error, loadMore }) {
  const nearBottom = useInView(sentinel, `${MARGIN}px`);

  // The observer reports asynchronously, so right after a page renders `nearBottom` can still
  // be true from before the new items pushed the sentinel down. Measure for real.
  function sentinelIsNear() {
    const rect = sentinel.value?.getBoundingClientRect();
    return !!rect && rect.top < window.innerHeight + MARGIN;
  }

  watch(
    [nearBottom, loading, hasMore, error],
    () => {
      if (nearBottom.value && !loading.value && hasMore.value && !error.value && sentinelIsNear()) {
        loadMore();
      }
    },
    { immediate: true, flush: "post" },
  );
}
