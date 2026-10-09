import { computed, ref, watch } from "vue";
import { getFollowedTopics, setFollowedTopics } from "@/api/me";
import { useAuth } from "@/composables/useAuth";

// One shared list of followed topic slugs for the whole app, loaded once per sign-in.
const followed = ref([]);
const loaded = ref(false);
let loading = null;

const { isSignedIn } = useAuth();

// Signing out (or in as someone else) starts over.
watch(isSignedIn, (signedIn) => {
  followed.value = [];
  loaded.value = false;
  loading = null;
  if (signedIn) loadFollowedTopics();
});

/** Fetch the list once; later calls share the same request. */
export function loadFollowedTopics() {
  if (!isSignedIn.value) return Promise.resolve();
  loading ??= getFollowedTopics()
    .then((result) => {
      followed.value = result.topics;
      loaded.value = true;
    })
    .catch(() => {
      // Try again next time something asks.
      loading = null;
    });
  return loading;
}

/**
 * Follow or unfollow a topic. Updates straight away and rolls back if saving fails.
 * @returns {Promise<void>} rejects with the API error so callers can show it
 */
export async function toggleFollow(slug) {
  const before = followed.value;
  followed.value = before.includes(slug) ? before.filter((s) => s !== slug) : [...before, slug];
  try {
    followed.value = (await setFollowedTopics(followed.value)).topics;
  } catch (error) {
    followed.value = before;
    throw error;
  }
}

export function useFollowedTopics() {
  return {
    followed: computed(() => followed.value),
    loaded: computed(() => loaded.value),
    isFollowing: (slug) => followed.value.includes(slug),
  };
}
