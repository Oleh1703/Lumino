import { useEffect, useState } from "react";
import SolarLoading from "../SolarLoading/SolarLoading.jsx";
import styles from "./GlassLoading.module.css";

export default function GlassLoading({ open, text = "Завантаження..." }) {
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

  if (!open) return null;

  return (
    <div className={styles.overlayRoot}>
      <div
        className={styles.stageFrame}
        style={{ transform: `translate(-50%, -50%) scale(${stageScale})` }}
        role="presentation"
      >
        <div className={styles.backdrop} />

        <div className={styles.box} role="status" aria-live="polite">
          <SolarLoading />
          <p className={styles.text}>{text}</p>
        </div>
      </div>
    </div>
  );
}
