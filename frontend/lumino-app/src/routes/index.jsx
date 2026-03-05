import { Navigate, Route, Routes } from "react-router-dom";
import { PATHS } from "./paths.js";
import UserLayout from "../layouts/UserLayout.jsx";
import AdminLayout from "../layouts/AdminLayout.jsx";

import StartPage from "../pages/public/Start/StartPage.jsx";
import LoginPage from "../pages/public/Login/LoginPage.jsx";
import RegisterPage from "../pages/public/Register/RegisterPage.jsx";
import ForgotPasswordPage from "../pages/public/ForgotPassword/ForgotPasswordPage.jsx";
import VerifyEmailPage from "../pages/public/VerifyEmail/VerifyEmailPage.jsx";
import OnboardingPage from "../pages/public/Onboarding/OnboardingPage.jsx";
import OnboardingLevelPage from "../pages/public/OnboardingLevel/OnboardingLevelPage.jsx";
import HomePage from "../pages/user/Home/HomePage.jsx";

export default function AppRoutes() {
  const isAuthed = false;
  const isAdmin = false;

  return (
    <Routes>
      <Route path={PATHS.start} element={<StartPage />} />
      <Route path={PATHS.login} element={<LoginPage />} />
      <Route path={PATHS.register} element={<RegisterPage />} />
      <Route path={PATHS.forgotPassword} element={<ForgotPasswordPage />} />
      <Route path={PATHS.verifyEmail} element={<VerifyEmailPage />} />
      <Route path={PATHS.onboarding} element={<OnboardingPage />} />
      <Route path={PATHS.onboardingLevel} element={<OnboardingLevelPage />} />

      <Route element={<UserLayout />}>
        <Route path={PATHS.home} element={isAuthed ? <HomePage /> : <Navigate to={PATHS.login} replace />} />
      </Route>

      <Route element={<AdminLayout />}>
        <Route path={PATHS.admin} element={isAdmin ? <div>Admin</div> : <Navigate to={PATHS.home} replace />} />
      </Route>

      <Route path="*" element={<Navigate to={PATHS.start} replace />} />
    </Routes>
  );
}
