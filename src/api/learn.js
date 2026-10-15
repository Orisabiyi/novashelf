import { apiGet } from "@/api/client";

let tracks = null;

export function getTracks() {
  tracks ??= apiGet("/learn").catch((error) => {
    tracks = null;
    throw error;
  });
  return tracks;
}

export function getLesson(track, lesson, options) {
  return apiGet(`/learn/${encodeURIComponent(track)}/${encodeURIComponent(lesson)}`, {}, options);
}
