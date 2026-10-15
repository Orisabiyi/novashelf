
export class ApiError extends Error {
  constructor(message, status, fieldErrors = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

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

export function apiGet(path, params = {}, { signal } = {}) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  }
  return request("GET", `${path}${query.size ? `?${query}` : ""}`, { signal });
}

export function apiPost(path, body) {
  return request("POST", path, { body });
}

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
