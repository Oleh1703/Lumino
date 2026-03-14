import { apiClient } from "./apiClient.js";

const normalizeLanguagesPayload = (data, fallbackCode = "en") => {
  const activeTargetLanguageCode = data?.activeTargetLanguageCode || data?.targetLanguageCode || fallbackCode;
  const learningLanguages = Array.isArray(data?.learningLanguages) ? data.learningLanguages : [];

  return {
    ok: true,
    activeTargetLanguageCode,
    learningLanguages,
    data,
  };
};

export const onboardingService = {
  async getSupportedLanguages() {
    const res = await apiClient.get("/onboarding/languages");

    return {
      ok: res.ok,
      status: res.status,
      items: res.ok ? (Array.isArray(res.data) ? res.data : []) : [],
      error: res.error || "",
    };
  },

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

  async getMyLanguages() {
    const res = await apiClient.get("/onboarding/languages/me");

    if (!res.ok) {
      return { ok: false, data: null, error: res.error || "" };
    }

    return normalizeLanguagesPayload(res.data, "en");
  },

  async updateMyLanguages(dto) {
    const res = await apiClient.put("/onboarding/languages/me", dto);

    return {
      ok: res.ok,
      status: res.status,
      error: res.error || "",
    };
  },

  async updateMyTargetLanguage(targetLanguageCode) {
    if (!targetLanguageCode) {
      return { ok: false, error: "TargetLanguageCode is required" };
    }

    const code = String(targetLanguageCode).trim().toLowerCase();
    const res = await apiClient.put("/onboarding/target-language/me", { targetLanguageCode: code });

    return {
      ok: res.ok,
      status: res.status,
      error: res.error || "",
    };
  },
};
