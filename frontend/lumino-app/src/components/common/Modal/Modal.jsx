import { useEffect, useRef } from "react";
import styles from "./Modal.module.css";

export default function Modal({ open, title, message, onClose }) {
  const okBtnRef = useRef(null);

  useEffect(() => {
    if (!open) return;

    const prevFocus = document.activeElement;

    const onKeyDown = (e) => {
      if (e.key === "Escape") onClose?.();
    };

    document.addEventListener("keydown", onKeyDown);

    // focus OK for keyboard users
    setTimeout(() => okBtnRef.current?.focus(), 0);

    return () => {
      document.removeEventListener("keydown", onKeyDown);
      prevFocus?.focus?.();
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className={styles.backdrop} role="presentation" onMouseDown={onClose}>
      <div
        className={styles.modal}
        role="dialog"
        aria-modal="true"
        aria-label={title || "Повідомлення"}
        onMouseDown={(e) => e.stopPropagation()}
      >
        {!!title && <div className={styles.title}>{title}</div>}
        {!!message && <div className={styles.message}>{message}</div>}

        <button
          ref={okBtnRef}
          className={styles.okBtn}
          type="button"
          onClick={onClose}
        >
          OK
        </button>
      </div>
    </div>
  );
}
