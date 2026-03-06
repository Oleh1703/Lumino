import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import styles from "./OnboardingLevelPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg-left.png";
import BgRight from "../../../../assets/backgrounds/bg-right.png";

import Bubble from "../../../../assets/onboarding/bubble1.svg";
import Mascot from "../../../../assets/mascot/mascot2.svg";

export default function OnboardingLevelPage() {
  const navigate = useNavigate();
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

  const handleContinue = () => {
    navigate(PATHS.onboardingLevelQuestion);
  };

  return (
    <div className={styles.viewport}>
      <div ref={stageRef} className={styles.stage}>
        <img className={styles.bgLeft} src={BgLeft} alt="" />
        <img className={styles.bgRight} src={BgRight} alt="" />

        <img className={styles.bubble} src={Bubble} alt="" />
        <p className={styles.bubbleText}>
          Дайте відповідь всього на<br />
          <strong>декілька коротких питань</strong> — і<br />
          ми розпочнемо перший урок!
        </p>

        <img className={styles.mascot} src={Mascot} alt="" />

        <button className={styles.continueBtn} onClick={handleContinue}>
          ПРОДОВЖИТИ
        </button>
      </div>
    </div>
  );
}
