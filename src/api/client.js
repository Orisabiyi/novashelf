/**
 * Thin fetch wrapper for the Novashelf API (server/Novashelf.Api).
 *
 * Requests go to the same origin under /api — in development Vite proxies
 * them to the .NET API, in production the host does — so cookies just work.
 */

export class ApiError extends Error {
  /**
   * @param {string} message Human-readable message, safe to show in the UI.
   * @param {number} status HTTP status, or 0 when the request never got a response.
   */
  constructor(message, status) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }

  /** The API is up but a content source (NASA, Spaceflight News) is not. */
  get isUpstream() {
    return this.status === 502;
  }

  get isNotFound() {
    return this.status === 404;
  }
}

/**
 * GET /api{path} and return the parsed JSON body.
 *
 * @param {string} path e.g. "/feed"
 * @param {Record<string, string | number | null | undefined>} [params] query
 *   parameters; null/undefined/"" values are left out.
 * @param {{ signal?: AbortSignal }} [options]
 */
export async function apiGet(path, params = {}, { signal } = {}) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  const url = `/api${path}${query.size ? `?${query}` : ""}`;

  let response;
  try {
    response = await fetch(url, {
      headers: { Accept: "application/json" },
      credentials: "include",
      signal,
    });
  } catch (error) {
    if (error.name === "AbortError") throw error;
    throw new ApiError("Can't reach Novashelf. Check your connection and try again.", 0);
  }

  if (!response.ok) {
    throw new ApiError(await problemMessage(response), response.status);
  }

  return response.json();
}

/** Pull a message out of an RFC 9457 ProblemDetails body, falling back to the status. */
async function problemMessage(response) {
  try {
    const problem = await response.json();
    if (problem.detail || problem.title) return problem.detail || problem.title;
  } catch {
    // Not JSON; fall through.
  }

  if (response.status === 404) return "We couldn't find that.";
  if (response.status >= 500) return "Something went wrong on our side. Please try again.";
  return `Request failed (${response.status}).`;
}
