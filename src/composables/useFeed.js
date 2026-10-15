import { computed } from "vue";
import { getFeed } from "@/api/content";
import { useAuth } from "@/composables/useAuth";
import { useFollowedTopics } from "@/composables/useFollowedTopics";
import { usePagedList } from "@/composables/usePagedList";

export function useFeed(topic) {
  const { status } = useAuth();
  const { followed, loaded } = useFollowedTopics();

  const source = computed(() => {
    const slug = topic.value;
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
