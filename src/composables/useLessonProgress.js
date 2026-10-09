import { ref, watch } from "vue";
import { getCompletedLessons, setLessonCompleted } from "@/api/me";
import { loadSession, useAuth } from "@/composables/useAuth";

// Finished lessons ("track/lesson" keys) for the signed-in reader, loaded once per sign-in.
const completed = ref(new Set());
let loading = null;

const { isSignedIn } = useAuth();
const key = (track, lesson) => `${track}/${lesson}`;

watch(isSignedIn, (signedIn) => {
  completed.value = new Set();
  loading = null;
  if (signedIn) loadLessonProgress();
});

export function loadLessonProgress() {
  if (!isSignedIn.value) return Promise.resolve();
  loading ??= getCompletedLessons()
    .then((lessons) => {
      completed.value = new Set(lessons.map((l) => key(l.track, l.lesson)));
    })
    .catch(() => {
      loading = null;
    });
  return loading;
}

/**
 * Mark a lesson finished or not, for signed-in readers. Waits for the session check (a lesson
 * opened directly can be on screen before it finishes). Updates straight away; rolls back and
 * rethrows on failure.
 */
export async function setCompleted(track, lesson, done) {
  await loadSession();
  if (!isSignedIn.value || completed.value.has(key(track, lesson)) === done) return;
  const update = (on) => {
    const next = new Set(completed.value);
    if (on) next.add(key(track, lesson));
    else next.delete(key(track, lesson));
    completed.value = next;
  };
  update(done);
  try {
    await setLessonCompleted(track, lesson, done);
  } catch (error) {
    update(!done);
    throw error;
  }
}

export function useLessonProgress() {
  const isCompleted = (track, lesson) => completed.value.has(key(track, lesson));
  return {
    isCompleted,
    /** How many of a track's lessons are finished. @param {{ slug: string, lessons: { slug: string }[] }} track */
    completedIn: (track) => track.lessons.filter((l) => isCompleted(track.slug, l.slug)).length,
  };
}
