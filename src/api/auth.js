import { apiGet, apiPost, apiSend } from "@/api/client";

/**
 * @typedef {object} Account
 * @property {string} email
 * @property {string | null} firstName
 */

/**
 * Create an account; the API signs the reader in (HttpOnly session cookie).
 * @param {{ email: string, password: string, firstName?: string, ageConfirmed: boolean }} details
 * @returns {Promise<Account>}
 */
export function register(details) {
  return apiPost("/auth/register", details);
}

/**
 * @param {{ email: string, password: string, remember: boolean }} credentials
 * @returns {Promise<Account>}
 */
export function login(credentials) {
  return apiPost("/auth/login", credentials);
}

export function logout() {
  return apiPost("/auth/logout");
}

/** The signed-in reader; rejects with a 401 ApiError when signed out. @returns {Promise<Account>} */
export function getMe() {
  return apiGet("/me");
}

/** @param {{ firstName: string | null }} details @returns {Promise<Account>} */
export function updateAccount(details) {
  return apiSend("PATCH", "/me", details);
}

export function changePassword({ currentPassword, newPassword }) {
  return apiPost("/me/password", { currentPassword, newPassword });
}

/** Deletes the account and everything stored for it, then signs out. */
export function deleteAccount(password) {
  return apiSend("DELETE", "/me", { password });
}
