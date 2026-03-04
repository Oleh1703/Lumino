import React, { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { PATHS } from "../../../routes/paths";
import { authService } from "../../../services/authService";

export default React.memo(function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [status, setStatus] = useState({ type: "idle", message: "" });

  const isValid = useMemo(() => email.trim().length > 3 && email.includes("@"), [email]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!isValid) return;

    setStatus({ type: "loading", message: "" });

    const res = await authService.forgotPassword({ email: email.trim() });
    if (res.ok) {
      setStatus({ type: "success", message: "Лист для відновлення надіслано (якщо пошта існує)." });
      return;
    }

    setStatus({ type: "error", message: res.error || "Помилка. Спробуй ще раз." });
  };

  return (
    <div className="page-center">
      <div className="card">
        <h1 className="h1">Відновлення пароля</h1>

        <form onSubmit={handleSubmit} className="form">
          <label className="label">
            Email
            <input
              className="input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@email.com"
              type="email"
            />
          </label>

          <button className="btn" disabled={!isValid || status.type === "loading"} type="submit">
            Надіслати
          </button>

          {status.type !== "idle" && <div className={`alert ${status.type}`}>{status.message}</div>}
        </form>

        <div className="links">
          <Link to={PATHS.login}>Назад до входу</Link>
        </div>
      </div>
    </div>
  );
});
