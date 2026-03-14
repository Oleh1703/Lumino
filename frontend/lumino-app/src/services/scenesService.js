import { apiClient } from "./apiClient.js";

export const scenesService = {
  getForMe(courseId) {
    const qs = courseId ? `?courseId=${encodeURIComponent(courseId)}` : "";
    return apiClient.get(`/scenes/me${qs}`);
  },

  getById(id) {
    return apiClient.get(`/scenes/${id}`);
  },
};
