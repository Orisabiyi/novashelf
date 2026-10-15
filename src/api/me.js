import { apiGet, apiSend } from "@/api/client";

export function getHistory({ page = 1 } = {}, options) {
  return apiGet("/me/history", { page }, options);
}

export function recordRead(id) {
  return apiSend("POST", "/me/history", { id });
}

export function removeFromHistory(id) {
  return apiSend("DELETE", `/me/history/${encodeURIComponent(id)}`);
}

export function clearHistory() {
  return apiSend("DELETE", "/me/history");
}

export function getFollowedTopics() {
  return apiGet("/me/topics");
}

export function setFollowedTopics(topics) {
  return apiSend("PUT", "/me/topics", { topics });
}

export function getPreferences() {
  return apiGet("/me/preferences");
}

export function savePreferences(preferences) {
  return apiSend("PUT", "/me/preferences", preferences);
}

export function getCompletedLessons() {
  return apiGet("/me/lessons");
}

export function setLessonCompleted(track, lesson, completed) {
  return apiSend(completed ? "PUT" : "DELETE", `/me/lessons/${encodeURIComponent(track)}/${encodeURIComponent(lesson)}`);
}
