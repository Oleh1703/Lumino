import { apiClient } from "./apiClient";

export const authService = {
  login(dto) {
    return apiClient.post("/api/auth/login", dto);
  },

  register(dto) {
    return apiClient.post("/api/auth/register", dto);
  },

  refresh(dto) {
    return apiClient.post("/api/auth/refresh", dto);
  },

  logout() {
    return apiClient.post("/api/auth/logout");
  },

  forgotPassword(dto) {
    return apiClient.post("/api/auth/forgot-password", dto);
  },

  resetPassword(dto) {
    return apiClient.post("/api/auth/reset-password", dto);
  },

  verifyEmail(dto) {
    return apiClient.post("/api/auth/verify-email", dto);
  },

  resendVerification(dto) {
    return apiClient.post("/api/auth/resend-verification", dto);
  },

  oauthGoogle(dto) {
    return apiClient.post("/api/auth/oauth/google", dto);
  },
};
