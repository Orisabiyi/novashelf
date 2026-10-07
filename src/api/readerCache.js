import { getReadable } from "@/api/content";
import { imageAt } from "@/utils/images";

// Makes opening an article feel instant:
// - prefetch: when a card is hovered, focused or touched, start loading the article (and its
//   hero image) before the click lands.
// - preview: remember the card's item and the exact image the browser already shows, so the
//   reader can render the headline and a placeholder picture immediately.

const MAX_ENTRIES = 40;
export const READER_HERO_WIDTH = 1280;

/** @type {Map<string, Promise<import("@/api/content").ReadableContent>>} */
const readables = new Map();
/** @type {Map<string, { item: import("@/api/content").ContentItem, imageSrc: string | null }>} */
const previews = new Map();

function remember(map, key, value) {
  map.delete(key);
  map.set(key, value);
  if (map.size > MAX_ENTRIES) map.delete(map.keys().next().value);
}

/** The article, from cache when it was prefetched. Failed loads aren't cached. */
export function loadReadable(id) {
  let request = readables.get(id);
  if (!request) {
    request = getReadable(id);
    request.catch(() => readables.delete(id));
    remember(readables, id, request);
  }
  return request;
}

/**
 * @param {import("@/api/content").ContentItem} item
 * @param {string | null} [imageSrc] the image URL the card is currently showing
 */
export function rememberPreview(item, imageSrc = null) {
  remember(previews, item.id, { item, imageSrc: imageSrc || previews.get(item.id)?.imageSrc || null });
}

export function getPreview(id) {
  return previews.get(id) ?? null;
}

/** Called on hover/focus/touch: warm the article and its hero image. */
export function prefetchReadable(item, imageSrc = null) {
  rememberPreview(item, imageSrc);
  if (readables.has(item.id)) return;
  loadReadable(item.id).catch(() => {});
  if (item.imageUrl) {
    const image = new Image();
    image.decoding = "async";
    image.src = imageAt(item.imageUrl, READER_HERO_WIDTH);
  }
}
