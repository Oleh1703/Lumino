const BASE_URL = import.meta.env.VITE_API_BASE_URL || "/api";

const buildUrl = (path) => {
  if (!path) return BASE_URL;
  if (path.startsWith("http")) return path;
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

export const apiClient = {
  async request(path, options = {}) {
    const url = buildUrl(path);

    const headers = {
      "Content-Type": "application/json",
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
      (data && (data.message || data.error || data.title)) ||
      `HTTP ${res.status}`;

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
