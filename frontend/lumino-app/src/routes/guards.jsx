import { Navigate } from "react-router-dom";
import { PATHS } from "./paths.js";

export function AuthGuard({ isAuthed, children }) {
  if (!isAuthed) return <Navigate to={PATHS.login} replace />;
  return children;
}

export function AdminGuard({ isAdmin, children }) {
  if (!isAdmin) return <Navigate to={PATHS.home} replace />;
  return children;
}
