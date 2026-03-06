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
import OnboardingLevelPage from "../pages/public/Onboarding/OnboardingLevel/OnboardingLevelPage.jsx";
import OnboardingLevelQuestionPage from "../pages/public/Onboarding/OnboardingLevelQuestion/OnboardingLevelQuestionPage.jsx";
import OnboardingLevelQuestionFlyPage from "../pages/public/Onboarding/OnboardinFly/OnboardingLevelQuestionFlyPage.jsx";
import OnboardingQuestionGoalPage from "../pages/public/Onboarding/OnboardingQuestionGoal/OnboardingQuestionGoalPage.jsx";
import OnboardingResultsPage from "../pages/public/Onboarding/OnboardingResults/OnboardingResultsPage.jsx";
import OnboardingDailyGoalPage from "../pages/public/Onboarding/OnboardingDailyGoal/OnboardingDailyGoalPage.jsx";
import OnboardingTrialPage from "../pages/public/Onboarding/OnboardingTrial/OnboardingTrialPage.jsx";
import OnboardingRunLessonPage from "../pages/public/Onboarding/OnboardingRunLesson/OnboardingRunLessonPage.jsx";
import OnboardingPreCreateProfPage from "../pages/public/Onboarding/OnboardingPreCreateProf/OnboardingPreCreateProfPage.jsx";
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
      <Route path={PATHS.onboardingLevelQuestion} element={<OnboardingLevelQuestionPage />} />
      <Route path={PATHS.onboardingLevelQuestionFly} element={<OnboardingLevelQuestionFlyPage />} />
      <Route path={PATHS.onboardingQuestionGoal} element={<OnboardingQuestionGoalPage />} />
      <Route path={PATHS.onboardingResults} element={<OnboardingResultsPage />} />
      <Route path={PATHS.onboardingDailyGoal} element={<OnboardingDailyGoalPage />} />
      <Route path={PATHS.onboardingTrial} element={<OnboardingTrialPage />} />
      <Route path={PATHS.onboardingRunLesson} element={<OnboardingRunLessonPage />} />
      <Route path={PATHS.onboardingPreCreateProf} element={<OnboardingPreCreateProfPage />} />

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
