import { computed } from "vue";
import { getFeed } from "@/api/content";
import { useAuth } from "@/composables/useAuth";
import { useFollowedTopics } from "@/composables/useFollowedTopics";
import { usePagedList } from "@/composables/usePagedList";

/**
 * Paged article feed for a topic, accumulating pages as the reader scrolls.
 * Changing the topic aborts any in-flight request and starts over, and so does signing in or
 * out (read marks are per reader) or, on the "following" feed, following another topic.
 *
 * @param {import("vue").Ref<string | null>} topic topic slug, "following", or null for everything
 */
export function useFeed(topic) {
  const { status } = useAuth();
  const { followed, loaded } = useFollowedTopics();

  const source = computed(() => {
    const slug = topic.value;
    // Wait for the session check so the first page comes back with read marks.
    if (status.value === "unknown") return null;
    if (slug === "following" && status.value === "signedIn") {
      if (!loaded.value) return null;
      // Read the list so following or unfollowing a topic reloads this feed.
      void followed.value.join();
    }
    return (page, options) => getFeed({ topic: slug, page }, options);
  });
  return usePagedList(source);
}
