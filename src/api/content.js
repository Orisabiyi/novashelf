import { apiGet } from "@/api/client";

export function getTopics(options) {
  return apiGet("/topics", {}, options);
}

export function getFeed({ topic = null, page = 1 } = {}, options) {
  return apiGet("/feed", { topic, page }, options);
}

export function getContent(id, options) {
  return apiGet(`/content/${encodeURIComponent(id)}`, {}, options);
}

export function getReadable(id, options) {
  return apiGet(`/content/${encodeURIComponent(id)}/read`, {}, options);
}

export function getApod(date = null, options) {
  return apiGet("/apod", { date }, options);
}

export function getApodArchive({ page = 1 } = {}, options) {
  return apiGet("/apod/archive", { page }, options);
}

export function searchImages({ q, page = 1 }, options) {
  return apiGet("/images", { q, page }, options);
}
