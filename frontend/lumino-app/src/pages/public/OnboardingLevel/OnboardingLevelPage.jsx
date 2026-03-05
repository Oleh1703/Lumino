import { useEffect, useRef } from "react";
import styles from "./OnboardingLevelPage.module.css";

import BgLeft from "../../../assets/backgrounds/bg-left@2x.png";
import BgRight from "../../../assets/backgrounds/bg-right@2x.png";

export default function OnboardingLevelPage() {
  const stageRef = useRef(null);

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;

    const resize = () => {
      const w = 1920;
      const h = 1080;

      const sx = window.innerWidth / w;
      const sy = window.innerHeight / h;
      const s = Math.min(sx, sy);

      stage.style.transform = `
        translate(${Math.round((window.innerWidth - w * s) / 2)}px, ${Math.round((window.innerHeight - h * s) / 2)}px)
        scale(${s})
      `;
    };

    resize();
    window.addEventListener("resize", resize);

    return () => window.removeEventListener("resize", resize);
  }, []);

  return (
    <div className={styles.viewport}>
      <div ref={stageRef} className={styles.stage}>
        <img className={styles.bgLeft} src={BgLeft} alt="" />
        <img className={styles.bgRight} src={BgRight} alt="" />

        <p className={styles.title}>Onboarding Level (next step)</p>
      </div>
    </div>
  );
}
