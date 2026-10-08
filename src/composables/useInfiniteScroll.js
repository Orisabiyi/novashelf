import { watch } from "vue";
import { useInView } from "@/composables/useInView";

const MARGIN = 600;

/**
 * Calls `loadMore` while `sentinel` (an element after the list) is near the viewport. This also
 * does the first load, and keeps going if a page comes back short.
 *
 * @param {import("vue").Ref<Element | null>} sentinel
 * @param {{ loading: import("vue").Ref<boolean>, hasMore: import("vue").Ref<boolean>,
 *           error: import("vue").Ref<unknown>, loadMore: () => void }} list
 */
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
