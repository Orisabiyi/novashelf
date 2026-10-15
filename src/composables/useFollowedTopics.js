import { computed, ref, watch } from "vue";
import { getFollowedTopics, setFollowedTopics } from "@/api/me";
import { useAuth } from "@/composables/useAuth";

const followed = ref([]);
const loaded = ref(false);
let loading = null;

const { isSignedIn } = useAuth();

watch(isSignedIn, (signedIn) => {
  followed.value = [];
  loaded.value = false;
  loading = null;
  if (signedIn) loadFollowedTopics();
});

export function loadFollowedTopics() {
  if (!isSignedIn.value) return Promise.resolve();
  loading ??= getFollowedTopics()
    .then((result) => {
      followed.value = result.topics;
      loaded.value = true;
    })
    .catch(() => {
      loading = null;
    });
  return loading;
}

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
