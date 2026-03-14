import { useEffect, useState } from "react";
import styles from "./GlassModal.module.css";

export default function GlassModal({
  open,
  title,
  message,
  onClose,
  primaryText = "Добре",
  secondaryText = "",
  onPrimary,
  onSecondary,
  variant = "default",
  illustrationSrc = "",
}) {
  const [stageScale, setStageScale] = useState(1);

  useEffect(() => {
    const updateStageScale = () => {
      const sx = window.innerWidth / 1920;
      const sy = window.innerHeight / 1080;
      setStageScale(Math.min(sx, sy));
    };

    updateStageScale();
    window.addEventListener("resize", updateStageScale);

    return () => {
      window.removeEventListener("resize", updateStageScale);
    };
  }, []);

  useEffect(() => {
    if (!open) return undefined;

    const handleKeyDown = (event) => {
      if (event.key === "Escape" && onClose) {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onClose]);

  if (!open) return null;

  const isLanguageWarning = variant === "languageWarning";
  const isSceneLocked = variant === "sceneLocked";

  const getButtonText = (text) => String(text || "").toUpperCase();

  const handlePrimary = () => {
    if (onPrimary) {
      onPrimary();
      return;
    }

    if (onClose) {
      onClose();
    }
  };

  const handleSecondary = () => {
    if (onSecondary) {
      onSecondary();
      return;
    }

    if (onClose) {
      onClose();
    }
  };

  return (
    <div className={styles.overlayRoot}>
      <div
        className={styles.stageFrame}
        style={{ transform: `translate(-50%, -50%) scale(${stageScale})` }}
        role="presentation"
        onClick={onClose}
      >
        <div className={`${styles.backdrop} ${isLanguageWarning ? styles.languageWarningBackdrop : ""} ${isSceneLocked ? styles.sceneLockedBackdrop : ""}`} />

        <div
          className={`${styles.modal} ${isLanguageWarning ? styles.languageWarningModal : ""} ${isSceneLocked ? styles.sceneLockedModal : ""}`}
          role="dialog"
          aria-modal="true"
          aria-labelledby="glass-modal-title"
          aria-describedby="glass-modal-message"
          onClick={(e) => e.stopPropagation()}
        >
        <button
          type="button"
          className={`${styles.closeButton} ${isLanguageWarning ? styles.languageWarningCloseButton : ""} ${isSceneLocked ? styles.sceneLockedCloseButton : ""}`}
          onClick={onClose}
          aria-label="Закрити"
        />

        <div className={`${styles.contentBox} ${isLanguageWarning ? styles.languageWarningContentBox : ""} ${isSceneLocked ? styles.sceneLockedContentBox : ""}`}>
          {illustrationSrc ? (
            <img className={`${styles.illustration} ${isSceneLocked ? styles.sceneLockedIllustration : ""}`} src={illustrationSrc} alt="" aria-hidden="true" />
          ) : null}

          {title ? (
            <h2 id="glass-modal-title" className={`${styles.title} ${isLanguageWarning ? styles.languageWarningTitle : ""} ${isSceneLocked ? styles.sceneLockedTitle : ""}`}>
              {title}
            </h2>
          ) : null}

          <p id="glass-modal-message" className={`${styles.message} ${isLanguageWarning ? styles.languageWarningMessage : ""} ${isSceneLocked ? styles.sceneLockedMessage : ""}`}>
            {message}
          </p>
        </div>

        {!isSceneLocked ? (
        <div className={`${styles.actions} ${isLanguageWarning ? styles.languageWarningActions : ""}`}>
          {isLanguageWarning ? (
            <>
              <button
                className={`${styles.btn} ${styles.secondaryBtn} ${styles.languageWarningSecondaryBtn}`}
                type="button"
                onClick={handlePrimary}
              >
                {getButtonText(primaryText)}
              </button>

              {!!secondaryText && (
                <button
                  className={`${styles.btn} ${styles.primaryBtn} ${styles.languageWarningPrimaryBtn}`}
                  type="button"
                  onClick={handleSecondary}
                >
                  {getButtonText(secondaryText)}
                </button>
              )}
            </>
          ) : (
            <>
              {!!secondaryText && (
                <button className={`${styles.btn} ${styles.secondaryBtn}`} type="button" onClick={handleSecondary}>
                  {getButtonText(secondaryText)}
                </button>
              )}

              <button className={`${styles.btn} ${styles.primaryBtn}`} type="button" onClick={handlePrimary}>
                {getButtonText(primaryText)}
              </button>
            </>
          )}
        </div>
        ) : null}
        </div>
      </div>
    </div>
  );
}
