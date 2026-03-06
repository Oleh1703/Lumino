import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import styles from "./OnboardingQuestionGoalPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg1-left.png";
import BgRight from "../../../../assets/backgrounds/bg1-right.png";

import ArrowPrev from "../../../../assets/icons/arrow-previous.svg";
import Bubble from "../../../../assets/onboarding/bubble4.svg";
import Mascot from "../../../../assets/mascot/mascot5.svg";

import TravelIcon from "../../../../assets/icons/goal/travel.png";
import TeachingIcon from "../../../../assets/icons/goal/teaching.png";
import CareerIcon from "../../../../assets/icons/goal/career.png";
import CommunionIcon from "../../../../assets/icons/goal/communion.png";
import DevelopmentIcon from "../../../../assets/icons/goal/development.png";
import EntertainmentIcon from "../../../../assets/icons/goal/entertainment.png";
import OtherIcon from "../../../../assets/icons/goal/other.png";

const STORAGE_KEY = "lumino_goal";

export default function OnboardingQuestionGoalPage() {
  const navigate = useNavigate();
  const stageRef = useRef(null);

  const [goal, setGoal] = useState("");

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
    if (!goal) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }

    localStorage.setItem(STORAGE_KEY, goal);
  }, [goal]);

  const goals = useMemo(
    () => [
      { key: "travel", title: "подорожі", icon: TravelIcon, x: 484, y: 584, w: 214 },
      { key: "teaching", title: "навчання", icon: TeachingIcon, x: 728, y: 584, w: 214 },
      { key: "career", title: "кар’єра", icon: CareerIcon, x: 972, y: 584, w: 216 },
      { key: "communion", title: "спілкування", icon: CommunionIcon, x: 1218, y: 584, w: 249 },
      { key: "development", title: "саморозвиток", icon: DevelopmentIcon, x: 618, y: 684, w: 278 },
      { key: "entertainment", title: "розваги", icon: EntertainmentIcon, x: 926, y: 684, w: 194 },
      { key: "other", title: "інше", icon: OtherIcon, x: 1150, y: 684, w: 153 },
    ],
    []
  );

  const handleBack = () => {
    navigate(PATHS.onboardingLevelQuestionFly);
  };

  const handleContinue = () => {};

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
          <span>Обери свою мету —</span>
          <span>і вперед!</span>
        </p>

        <h1 className={styles.title}>З якою метою ви вивчаєте англійську?</h1>

        {goals.map((it) => (
          <button
            key={it.key}
            type="button"
            className={`${styles.goalBtn} ${goal === it.key ? styles.goalBtnActive : ""}`}
            style={{ left: `${it.x}px`, top: `${it.y}px`, width: `${it.w}px` }}
            onClick={() => setGoal(it.key)}
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
