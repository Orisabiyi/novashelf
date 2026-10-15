import { apiGet, apiPost, apiSend } from "@/api/client";

export function register(details) {
  return apiPost("/auth/register", details);
}

export function login(credentials) {
  return apiPost("/auth/login", credentials);
}

export function logout() {
  return apiPost("/auth/logout");
}

export function getMe() {
  return apiGet("/me");
}

export function updateAccount(details) {
  return apiSend("PATCH", "/me", details);
}

export function changePassword({ currentPassword, newPassword }) {
  return apiPost("/me/password", { currentPassword, newPassword });
}

export function deleteAccount(password) {
  return apiSend("DELETE", "/me", { password });
}
