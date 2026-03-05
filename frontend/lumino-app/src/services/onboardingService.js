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
};
