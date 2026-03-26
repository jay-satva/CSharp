const AUTH_STORAGE_KEY = "final_exam_auth";

export const getAuth = () => {
  const raw = window.localStorage.getItem(AUTH_STORAGE_KEY);
  if (!raw) return null;

  try {
    return JSON.parse(raw);
  } catch {
    window.localStorage.removeItem(AUTH_STORAGE_KEY);
    return null;
  }
};

export const setAuth = (auth) => {
  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(auth));
};

export const clearAuth = () => {
  window.localStorage.removeItem(AUTH_STORAGE_KEY);
};

export const getAccessToken = () => getAuth()?.accessToken ?? null;
