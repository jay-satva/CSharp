import { clearAuth, getAccessToken } from "./authStorage";

export const API_BASE = "http://localhost:5130";

export const authorizedFetch = async (path, options = {}) => {
  const token = getAccessToken();
  const headers = new Headers(options.headers || {});

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    clearAuth();
  }

  return response;
};
