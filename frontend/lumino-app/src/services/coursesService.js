import { apiClient } from "./apiClient.js";

function normalizeItems(data) {
  if (Array.isArray(data)) {
    return data;
  }

  if (Array.isArray(data?.items)) {
    return data.items;
  }

  return [];
}

export const coursesService = {
  async getPublishedCourses(languageCode) {
    const query = languageCode ? `?languageCode=${encodeURIComponent(languageCode)}` : "";
    const res = await apiClient.get(`/courses${query}`);

    return {
      ok: res.ok,
      status: res.status,
      items: res.ok ? normalizeItems(res.data) : [],
      error: res.error || "",
    };
  },

  async getMyCourses(languageCode) {
    const query = languageCode ? `?languageCode=${encodeURIComponent(languageCode)}` : "";
    const res = await apiClient.get(`/courses/me${query}`);

    return {
      ok: res.ok,
      status: res.status,
      items: res.ok ? normalizeItems(res.data) : [],
      error: res.error || "",
    };
  },
};
