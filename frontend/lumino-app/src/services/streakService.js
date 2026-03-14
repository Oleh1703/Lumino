import { apiClient } from "./apiClient.js";

export const streakService = {
  async getMyStreak() {
    const res = await apiClient.get("/streak/me");

    return {
      ok: res.ok,
      status: res.status,
      data: res.ok ? res.data || null : null,
      error: res.error || "",
    };
  },

  async getMyCalendarMonth(year, month) {
    if (!year || !month) {
      return { ok: false, data: null, error: "Year and month are required" };
    }

    const res = await apiClient.get(`/streak/calendar?year=${Number(year)}&month=${Number(month)}`);

    return {
      ok: res.ok,
      status: res.status,
      data: res.ok ? res.data || null : null,
      error: res.error || "",
    };
  },
};
