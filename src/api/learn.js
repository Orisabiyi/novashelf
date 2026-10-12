import { apiGet } from "@/api/client";

/**
 * @typedef {object} TrackSummary
 * @property {string} slug
 * @property {string} title
 * @property {string} summary
 * @property {string} icon primeicons class, e.g. "pi-sun"
 * @property {boolean} startHere
 * @property {{ slug: string, title: string, summary: string }[]} lessons
 */

/**
 * @typedef {object} LessonSection
 * @property {"wikipedia" | "nasa"} kind
 * @property {string} title
 * @property {string} sourceUrl
 * @property {string} attribution
 * @property {string | null} licenseUrl
 * @property {string | null} html null when the source couldn't be loaded
 * @property {number} readingMinutes
 * @property {boolean} truncated
 */

let tracks = null;

/** All tracks (fetched once per visit; the curriculum rarely changes). @returns {Promise<TrackSummary[]>} */
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
