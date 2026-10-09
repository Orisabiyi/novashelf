import { apiGet, apiSend } from "@/api/client";

// The signed-in reader's own data (/api/me). All of these need a session.

/** @returns {Promise<import("@/api/content").PagedResult<{ item: import("@/api/content").ContentItem, readAt: string }>>} */
export function getHistory({ page = 1 } = {}, options) {
  return apiGet("/me/history", { page }, options);
}

/** Record that the reader opened an item (called by the reader once it's actually open). */
export function recordRead(id) {
  return apiSend("POST", "/me/history", { id });
}

export function removeFromHistory(id) {
  return apiSend("DELETE", `/me/history/${encodeURIComponent(id)}`);
}

export function clearHistory() {
  return apiSend("DELETE", "/me/history");
}

/** @returns {Promise<{ topics: string[] }>} slugs of the topics the reader follows */
export function getFollowedTopics() {
  return apiGet("/me/topics");
}

/** Replace the whole list; returns the saved list. */
export function setFollowedTopics(topics) {
  return apiSend("PUT", "/me/topics", { topics });
}

/** @returns {Promise<{ fontSize: string, width: string, theme: string } | null>} null until first saved */
export function getPreferences() {
  return apiGet("/me/preferences");
}

export function savePreferences(preferences) {
  return apiSend("PUT", "/me/preferences", preferences);
}

/** @returns {Promise<{ track: string, lesson: string, completedAt: string }[]>} */
export function getCompletedLessons() {
  return apiGet("/me/lessons");
}

export function setLessonCompleted(track, lesson, completed) {
  return apiSend(completed ? "PUT" : "DELETE", `/me/lessons/${encodeURIComponent(track)}/${encodeURIComponent(lesson)}`);
}
