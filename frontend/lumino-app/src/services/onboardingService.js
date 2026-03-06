import { apiClient } from "./apiClient.js";

export const onboardingService = {
  async getLanguageAvailability(languageCode) {
    if (!languageCode) return { ok: false };

    const code = String(languageCode).trim().toLowerCase();
    const res = await apiClient.get(`/onboarding/languages/${code}/availability`);

    if (!res.ok) {
      return { ok: false };
    }

    return {
      ok: true,
      languageCode: res.data?.languageCode || code,
      hasPublishedCourses: Boolean(res.data?.hasPublishedCourses),
    };
  },

  async getDemoExercises(languageCode, level) {
    if (!languageCode || !level) return { ok: false, items: [] };

    const code = String(languageCode).trim().toLowerCase();
    const courseLevel = String(level).trim().toLowerCase();

    const paths = [
      `/onboarding/demo-exercises?languageCode=${encodeURIComponent(code)}&level=${encodeURIComponent(courseLevel)}`,
      `/onboarding/demo-exercises/${encodeURIComponent(code)}/${encodeURIComponent(courseLevel)}`,
      `/learning/demo-exercises?languageCode=${encodeURIComponent(code)}&level=${encodeURIComponent(courseLevel)}`,
      `/exercises/demo?languageCode=${encodeURIComponent(code)}&level=${encodeURIComponent(courseLevel)}`,
      `/demo-exercises?languageCode=${encodeURIComponent(code)}&level=${encodeURIComponent(courseLevel)}`,
    ];

    for (const path of paths) {
      const res = await apiClient.get(path);

      if (!res.ok) {
        continue;
      }

      const items = Array.isArray(res.data)
        ? res.data
        : Array.isArray(res.data?.items)
          ? res.data.items
          : Array.isArray(res.data?.exercises)
            ? res.data.exercises
            : [];

      return {
        ok: true,
        languageCode: code,
        level: courseLevel,
        items,
      };
    }

    return {
      ok: false,
      languageCode: code,
      level: courseLevel,
      items: [],
    };
  },
};
