import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import { onboardingService } from "../../../../services/onboardingService.js";
import styles from "./OnboardingLevelQuestionPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg1-left.png";
import BgRight from "../../../../assets/backgrounds/bg1-right.png";

import ArrowPrev from "../../../../assets/icons/arrow-previous.svg";
import Bubble from "../../../../assets/onboarding/bubble2.svg";
import Mascot from "../../../../assets/mascot/mascot3.svg";

import A1Icon from "../../../../assets/icons/levels/a1.png";
import A2Icon from "../../../../assets/icons/levels/a2.png";
import B1Icon from "../../../../assets/icons/levels/b1.png";
import B2Icon from "../../../../assets/icons/levels/b2.png";
import C1Icon from "../../../../assets/icons/levels/c1.png";

const STORAGE_KEY = "lumino_course_level";
const DEMO_EXERCISES_KEY = "lumino_demo_exercises";

export default function OnboardingLevelQuestionPage() {
  const navigate = useNavigate();
  const stageRef = useRef(null);

  const [level, setLevel] = useState("");
  const [loading, setLoading] = useState(false);

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
    if (!level) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }

    localStorage.setItem(STORAGE_KEY, level);
  }, [level]);

const levels = useMemo(
    () => [
      { key: "a1", title: "новачок", icon: A1Icon, x: 600, y: 584, w: 198 },
      { key: "a2", title: "початківець", icon: A2Icon, x: 828, y: 583.71, w: 246 },
      { key: "b1", title: "впевнено", icon: B1Icon, x: 1104, y: 583.71, w: 216 },
      { key: "b2", title: "просунуто", icon: B2Icon, x: 746, y: 683.71, w: 225 },
      { key: "c1", title: "вільно", icon: C1Icon, x: 1001, y: 683.71, w: 174 },
    ],
    []
  );

  const handleBack = () => {
    navigate(PATHS.onboardingLevel);
  };

  const handleContinue = async () => {
    if (!level || loading) return;

    setLoading(true);

    try {
      const languageCode = localStorage.getItem("targetLanguage") || "en";
      const res = await onboardingService.getDemoExercises(languageCode, level);

      if (res.ok) {
        localStorage.setItem(DEMO_EXERCISES_KEY, JSON.stringify(res.items || []));
      } else {
        localStorage.removeItem(DEMO_EXERCISES_KEY);
      }
    } catch (e) {
      localStorage.removeItem(DEMO_EXERCISES_KEY);
    } finally {
      setLoading(false);
      navigate(PATHS.onboardingLevelQuestionFly || "/onboarding/level/question/fly");
    }
  };


  const handleStageMouseDown = (e) => {
    if (e.button !== 0) return;

    const t = e.target;

    if (t.closest(`.${styles.backBtn}`)) return;
    if (t.closest(`.${styles.levelBtn}`)) return;
    if (t.closest(`.${styles.continueBtn}`)) return;

    setLevel("");
  };
  return (
    <div className={styles.viewport}>
      <div ref={stageRef} className={styles.stage} onMouseDown={handleStageMouseDown}>
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
        <p className={styles.bubbleText}>Це займе лише секунду!</p>

        <h1 className={styles.title}>Наскільки добре ви знаєте англійську?</h1>

        {levels.map((it) => (
          <button
            key={it.key}
            type="button"
            className={`${styles.levelBtn} ${level === it.key ? styles.levelBtnActive : ""}`}
            style={{ left: `${it.x}px`, top: `${it.y}px`, width: `${it.w}px` }}
            onClick={() => setLevel(it.key)}
          >
            <img className={styles.levelIcon} src={it.icon} alt={it.key} />
            {it.title}
          </button>
        ))}

        <button
          className={styles.continueBtn}
          type="button"
          onClick={handleContinue}
          disabled={!level || loading}
        >
          {loading ? "ЗАВАНТАЖЕННЯ..." : "ПРОДОВЖИТИ"}
        </button>
      </div>
    </div>
  );
}
