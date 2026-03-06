import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import styles from "./OnboardingDailyGoalPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg1-left.png";
import BgRight from "../../../../assets/backgrounds/bg1-right.png";

import ArrowPrev from "../../../../assets/icons/arrow-previous.svg";
import Bubble from "../../../../assets/onboarding/bubble5.svg";
import Mascot from "../../../../assets/mascot/mascot3.svg";

import FiveMinIcon from "../../../../assets/icons/goal/daygoal/5min.png";
import TenMinIcon from "../../../../assets/icons/goal/daygoal/10min.png";
import TwentyMinIcon from "../../../../assets/icons/goal/daygoal/20min.png";

const STORAGE_KEY = "lumino_daily_goal";

export default function OnboardingDailyGoalPage() {
  const navigate = useNavigate();
  const stageRef = useRef(null);

  const [dailyGoal, setDailyGoal] = useState("");

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

  useEffect(() => {
    if (!dailyGoal) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }

    localStorage.setItem(STORAGE_KEY, dailyGoal);
  }, [dailyGoal]);

  const goals = useMemo(
    () => [
      { key: "5min", title: "5 хв/день", icon: FiveMinIcon, x: 598, y: 585, w: 213 },
      { key: "10min", title: "10 хв/день", icon: TenMinIcon, x: 841, y: 585, w: 225 },
      { key: "20min", title: "20 хв/день", icon: TwentyMinIcon, x: 1096, y: 585, w: 226 },
    ],
    []
  );

  const handleBack = () => {
    navigate(PATHS.onboardingResults);
  };

  const handleContinue = () => {
    navigate(PATHS.onboardingTrial);
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

        <img className={styles.mascot} src={Mascot} alt="" />
        <img className={styles.bubble} src={Bubble} alt="" />

        <p className={styles.bubbleText}>
          <span>Регулярність</span>
          <span>творить магію!</span>
        </p>

        <h1 className={styles.title}>Яку щоденну ціль оберемо?</h1>

        {goals.map((it) => (
          <button
            key={it.key}
            type="button"
            className={`${styles.goalBtn} ${dailyGoal === it.key ? styles.goalBtnActive : ""}`}
            style={{ left: `${it.x}px`, top: `${it.y}px`, width: `${it.w}px` }}
            onClick={() => setDailyGoal(it.key)}
          >
            <img className={styles.goalIcon} src={it.icon} alt="" />
            {it.title}
          </button>
        ))}

        <button className={styles.continueBtn} type="button" onClick={handleContinue}>
          ПРОДОВЖИТИ
        </button>
      </div>
    </div>
  );
}
