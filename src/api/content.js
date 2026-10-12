import { apiGet } from "@/api/client";

/**
 * @typedef {object} ContentItem
 * @property {string} id Source-prefixed id: "sfn-…", "apod-yyyy-MM-dd", "img-…".
 * @property {"article" | "apod" | "image"} kind
 * @property {string} title
 * @property {string} summary
 * @property {string | null} imageUrl
 * @property {string | null} url Link to the original.
 * @property {string | null} publisher
 * @property {string | null} publishedAt ISO 8601.
 */

/**
 * @template T
 * @typedef {object} PagedResult
 * @property {T[]} items
 * @property {number} page
 * @property {boolean} hasMore
 */

/** @returns {Promise<{ slug: string, name: string }[]>} */
export function getTopics(options) {
  return apiGet("/topics", {}, options);
}

/**
 * @param {{ topic?: string | null, page?: number }} query
 * @returns {Promise<PagedResult<ContentItem>>}
 */
export function getFeed({ topic = null, page = 1 } = {}, options) {
  return apiGet("/feed", { topic, page }, options);
}

/** @returns {Promise<ContentItem>} */
export function getContent(id, options) {
  return apiGet(`/content/${encodeURIComponent(id)}`, {}, options);
}

/**
 * @typedef {object} ReadableContent
 * @property {ContentItem} item
 * @property {string} html Sanitised article body.
 * @property {string | null} byline
 * @property {number} readingMinutes
 * @property {boolean} extracted False when only the summary was available.
 */

/** @returns {Promise<ReadableContent>} */
export function getReadable(id, options) {
  return apiGet(`/content/${encodeURIComponent(id)}/read`, {}, options);
}

/**
 * @param {string | null} [date] yyyy-MM-dd; latest when omitted.
 * @returns {Promise<ContentItem>}
 */
export function getApod(date = null, options) {
  return apiGet("/apod", { date }, options);
}

/**
 * Pictures of the Day, newest first, 24 per page.
 * @returns {Promise<PagedResult<ContentItem>>}
 */
export function getApodArchive({ page = 1 } = {}, options) {
  return apiGet("/apod/archive", { page }, options);
}

/** @returns {Promise<PagedResult<ContentItem>>} */
export function searchImages({ q, page = 1 }, options) {
  return apiGet("/images", { q, page }, options);
}
