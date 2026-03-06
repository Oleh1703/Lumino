import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import styles from "./OnboardingResultsPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg1-left.png";
import BgRight from "../../../../assets/backgrounds/bg1-right.png";

import ArrowPrev from "../../../../assets/icons/arrow-previous.svg";
import Bubble from "../../../../assets/onboarding/bubble3.svg";
import Mascot from "../../../../assets/mascot/mascot6.svg";

export default function OnboardingResultsPage() {
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

  const handleBack = () => {
    navigate(PATHS.onboardingQuestionGoal);
  };

  const handleContinue = () => {
    navigate(PATHS.onboardingDailyGoal);
  };

  return (
    <div className={styles.viewport}>
      <div ref={stageRef} className={styles.stage}>
        <div
          className={styles.bg}
          style={{
            backgroundImage: `url(${BgLeft}), url(${BgRight})`,
          }}
        />

        <div className={styles.bottomShade} />

        <button className={styles.backBtn} type="button" onClick={handleBack}>
          <img className={styles.backIcon} src={ArrowPrev} alt="back" />
        </button>

        <div className={styles.progressTrack}>
          <div className={styles.progressFill} />
        </div>

        <img className={styles.bubble} src={Bubble} alt="" />
        <p className={styles.bubbleText}>
          <span>Трішки щодня — і</span>
          <span>буде результат!</span>
        </p>

        <img className={styles.mascot} src={Mascot} alt="" />

        <button className={styles.continueBtn} type="button" onClick={handleContinue}>
          ПРОДОВЖИТИ
        </button>
      </div>
    </div>
  );
}
