import { apiClient } from "./apiClient.js";

export const progressService = {
  getMyProgress() {
    return apiClient.get("/progress/me");
  },

  getDailyGoal() {
    return apiClient.get("/progress/daily-goal");
  },

  getMe() {
    return apiClient.get("/user/me");
  },

  getStreak() {
    return apiClient.get("/streak/me");
  },

  getStreakCalendar(year, month) {
    const params = [];

    if (year) {
      params.push(`year=${encodeURIComponent(year)}`);
    }

    if (month) {
      params.push(`month=${encodeURIComponent(month)}`);
    }

    const qs = params.length ? `?${params.join("&")}` : "";
    return apiClient.get(`/streak/calendar${qs}`);
  },

  restoreHearts(heartsToRestore = 5) {
    return apiClient.post("/user/restore-hearts", { heartsToRestore });
  },
};
