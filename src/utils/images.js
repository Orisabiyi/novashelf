// Image URLs from the API point at its resizing proxy (/api/img?src=…&sig=…); the width is ours
// to choose. Anything else (rare: a non-http source) is used as is.
const PROXY_PREFIX = "/api/img?";

export function isProxied(url) {
  return typeof url === "string" && url.startsWith(PROXY_PREFIX);
}

/** The image at a given width (the server snaps it to its standard sizes). */
export function imageAt(url, width) {
  return isProxied(url) ? `${url}&w=${width}` : url;
}

/** A srcset so the browser picks the right size for the screen; undefined if not proxied. */
export function imageSrcset(url, widths) {
  return isProxied(url) ? widths.map((w) => `${url}&w=${w} ${w}w`).join(", ") : undefined;
}

/** The original image address inside a proxied URL (or null if it isn't one). */
export function originalSource(url) {
  if (!isProxied(url)) return null;
  return new URLSearchParams(url.slice(PROXY_PREFIX.length)).get("src");
}
