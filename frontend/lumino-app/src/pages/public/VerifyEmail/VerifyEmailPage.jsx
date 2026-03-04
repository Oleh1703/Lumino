import React, { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { PATHS } from "../../../routes/paths";
import { authService } from "../../../services/authService";

export default React.memo(function VerifyEmailPage() {
  const [params] = useSearchParams();
  const token = params.get("token") || "";
  const email = params.get("email") || "";

  const [status, setStatus] = useState({ type: "idle", message: "" });

  const canVerify = useMemo(() => token.trim().length > 0, [token]);

  const handleVerify = async () => {
    if (!canVerify) return;

    setStatus({ type: "loading", message: "" });

    const dto = email.trim().length > 0 ? { token: token.trim(), email: email.trim() } : { token: token.trim() };
    const res = await authService.verifyEmail(dto);

    if (res.ok) {
      setStatus({ type: "success", message: "Пошта підтверджена. Можеш увійти." });
      return;
    }

    setStatus({ type: "error", message: res.error || "Не вдалося підтвердити пошту." });
  };

  return (
    <div className="page-center">
      <div className="card">
        <h1 className="h1">Підтвердження пошти</h1>

        <p className="p">Якщо токен є в URL — натисни кнопку нижче.</p>

        <button className="btn" disabled={!canVerify || status.type === "loading"} onClick={handleVerify} type="button">
          Підтвердити
        </button>

        {status.type !== "idle" && <div className={`alert ${status.type}`}>{status.message}</div>}

        <div className="links">
          <Link to={PATHS.login}>Перейти до входу</Link>
        </div>
      </div>
    </div>
  );
});
