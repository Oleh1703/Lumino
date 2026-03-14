import { apiClient } from "./apiClient.js";

export const userService = {
  getMe() {
    return apiClient.get("/user/me");
  },

  restoreHearts(heartsCount = 5) {
    return apiClient.post("/user/restore-hearts", { heartsCount });
  },
};
