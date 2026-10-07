import { onBeforeUnmount, ref, watch } from "vue";

/**
 * Whether an element is (near) the viewport. Used as an infinite-scroll sentinel.
 *
 * @param {import("vue").Ref<Element | null>} target
 * @param {string} [rootMargin] how far ahead of the viewport counts as "in view"
 */
export function useInView(target, rootMargin = "600px") {
  const inView = ref(false);
  const observer = new IntersectionObserver(
    ([entry]) => (inView.value = entry.isIntersecting),
    { rootMargin },
  );

  watch(
    target,
    (element, previous) => {
      if (previous) observer.unobserve(previous);
      if (element) observer.observe(element);
      else inView.value = false;
    },
    { immediate: true },
  );

  onBeforeUnmount(() => observer.disconnect());

  return inView;
}
