import { authStorage } from "./authStorage.js";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

const buildUrl = (path) => {
  if (!path) return BASE_URL;
  if (path.startsWith("http")) return path;
  if (path.startsWith("/api/") && BASE_URL === "/api") return path;
  if (path.startsWith("/")) return `${BASE_URL}${path}`;
  return `${BASE_URL}/${path}`;
};

const safeJson = async (res) => {
  try {
    return await res.json();
  } catch {
    return null;
  }
};

const shouldClearTokens = (res, data, error) => {
  if (res.status === 401) {
    return true;
  }

  if (res.status !== 404) {
    return false;
  }

  const type = String(data?.type || "").trim().toLowerCase();
  const detail = String(data?.detail || error || "").trim().toLowerCase();

  return type === "not_found" && detail === "user not found";
};

export const apiClient = {
  async request(path, options = {}) {
    const url = buildUrl(path);
    const accessToken = authStorage.getAccessToken();

    const headers = {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...(options.headers || {}),
    };

    const res = await fetch(url, {
      ...options,
      headers,
    });

    const data = await safeJson(res);

    if (res.ok) {
      return { ok: true, status: res.status, data };
    }

    const error =
      (data && (data.detail || data.message || data.error || data.title)) ||
      `HTTP ${res.status}`;

    if (shouldClearTokens(res, data, error)) {
      authStorage.clearTokens();
    }

    return { ok: false, status: res.status, data, error };
  },

  get(path, options = {}) {
    return this.request(path, { ...options, method: "GET" });
  },

  post(path, body, options = {}) {
    return this.request(path, {
      ...options,
      method: "POST",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  },

  put(path, body, options = {}) {
    return this.request(path, {
      ...options,
      method: "PUT",
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  },

  del(path, options = {}) {
    return this.request(path, { ...options, method: "DELETE" });
  },
};
