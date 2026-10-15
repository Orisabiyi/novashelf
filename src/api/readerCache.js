import { getReadable } from "@/api/content";
import { imageAt } from "@/utils/images";

// Prefetch starts loading an article on hover, focus or touch. Preview keeps the card's item and
// the image the browser already has, so the reader can show the headline and picture at once.

const MAX_ENTRIES = 40;
export const READER_HERO_WIDTH = 1280;

const readables = new Map();
const previews = new Map();

function remember(map, key, value) {
  map.delete(key);
  map.set(key, value);
  if (map.size > MAX_ENTRIES) map.delete(map.keys().next().value);
}

export function loadReadable(id) {
  let request = readables.get(id);
  if (!request) {
    request = getReadable(id);
    request.catch(() => readables.delete(id));
    remember(readables, id, request);
  }
  return request;
}

export function rememberPreview(item, imageSrc = null) {
  remember(previews, item.id, { item, imageSrc: imageSrc || previews.get(item.id)?.imageSrc || null });
}

export function getPreview(id) {
  return previews.get(id) ?? null;
}

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
