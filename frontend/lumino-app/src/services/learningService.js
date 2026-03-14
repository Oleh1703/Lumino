import { apiClient } from "./apiClient.js";

export const learningService = {
  getMyCoursePath(courseId) {
    return apiClient.get(`/learning/courses/${courseId}/path/me`);
  },
};
