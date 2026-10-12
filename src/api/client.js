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
   * @param {Record<string, string[]>} [fieldErrors] per-field messages from a validation problem,
   *   keyed by field name (e.g. { email: ["Enter a valid email address."] }).
   */
  constructor(message, status, fieldErrors = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  /** The API is up but a content source (NASA, Spaceflight News) is not. */
  get isUpstream() {
    return this.status === 502;
  }

  get isNotFound() {
    return this.status === 404;
  }

  get isUnauthorized() {
    return this.status === 401;
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
export function apiGet(path, params = {}, { signal } = {}) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  return request("GET", `${path}${query.size ? `?${query}` : ""}`, { signal });
}

/**
 * POST a JSON body to /api{path}; returns the parsed response, or null when there's none.
 *
 * @param {string} path
 * @param {unknown} [body]
 */
export function apiPost(path, body) {
  return request("POST", path, { body });
}

/**
 * Any method with an optional JSON body (PUT, PATCH, DELETE…).
 *
 * @param {"POST" | "PUT" | "PATCH" | "DELETE"} method
 * @param {string} path
 * @param {unknown} [body]
 */
export function apiSend(method, path, body) {
  return request(method, path, { body });
}

async function request(method, path, { body, signal } = {}) {
  let response;
  try {
    response = await fetch(`/api${path}`, {
      method,
      headers: {
        Accept: "application/json",
        ...(body === undefined ? {} : { "Content-Type": "application/json" }),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: "include",
      signal,
    });
  } catch (error) {
    if (error.name === "AbortError") throw error;
    throw new ApiError("Can't reach Novashelf. Check your connection and try again.", 0);
  }

  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return null;
  return response.json();
}

/** Build an ApiError from an RFC 9457 ProblemDetails body, falling back to the status. */
async function toApiError(response) {
  let problem = null;
  try {
    problem = await response.json();
  } catch {
    // No JSON body (e.g. the rate limiter's bare 429).
  }

  const fieldErrors = problem?.errors && typeof problem.errors === "object" ? problem.errors : {};
  return new ApiError(messageFor(response.status, problem), response.status, fieldErrors);
}

function messageFor(status, problem) {
  if (problem?.detail) return problem.detail;
  if (status === 404) return "We couldn't find that.";
  if (status === 429) return "Too many attempts. Wait a minute and try again.";
  // Server-side titles for crashes ("An error occurred while processing your request") aren't helpful.
  if (status >= 500) return "Something went wrong on our side. Please try again.";
  if (problem?.title) return problem.title;
  return `Request failed (${status}).`;
}
