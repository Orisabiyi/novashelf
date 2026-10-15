const PROXY_PREFIX = "/api/img?";

export function isProxied(url) {
  return typeof url === "string" && url.startsWith(PROXY_PREFIX);
}

export function imageAt(url, width) {
  return isProxied(url) ? `${url}&w=${width}` : url;
}

export function imageSrcset(url, widths) {
  return isProxied(url) ? widths.map((w) => `${url}&w=${w} ${w}w`).join(", ") : undefined;
}

export function originalSource(url) {
  if (!isProxied(url)) return null;
  return new URLSearchParams(url.slice(PROXY_PREFIX.length)).get("src");
}
