import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../../routes/paths.js";
import styles from "./OnboardingTrialPage.module.css";

import BgLeft from "../../../../assets/backgrounds/bg1-left.png";
import BgRight from "../../../../assets/backgrounds/bg1-right.png";

import ArrowPrev from "../../../../assets/icons/arrow-previous.svg";
import Bubble from "../../../../assets/onboarding/bubble6.svg";
import Mascot from "../../../../assets/mascot/mascot5.svg";

import BooksIcon from "../../../../assets/icons/goal/baselesson/books.png";
import SaturnIcon from "../../../../assets/icons/goal/baselesson/saturn.png";

const STORAGE_KEY = "lumino_trial_choice";

const LANG_TITLES = {
  en: "англійської",
  de: "німецької",
  it: "італійської",
  es: "іспанської",
  fr: "французької",
  pl: "польської",
  ja: "японської",
  ko: "корейської",
  zh: "китайської",
};

export default function OnboardingTrialPage() {
  const navigate = useNavigate();
  const stageRef = useRef(null);

  const [selected, setSelected] = useState("");
  const [hovered, setHovered] = useState("");

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
    if (!selected) {
      localStorage.removeItem(STORAGE_KEY);
      return;
    }

    localStorage.setItem(STORAGE_KEY, selected);
  }, [selected]);

  const selectedLanguageTitle = useMemo(() => {
    const languageCode = localStorage.getItem("targetLanguage") || "en";
    return LANG_TITLES[languageCode] || "англійської";
  }, []);

  const cards = useMemo(
    () => [
      {
        key: "recommended",
        title: "Розпочати з основ",
        description: `Пройдіть найлегший урок курсу ${selectedLanguageTitle} мови.`,
        icon: BooksIcon,
      },
      {
        key: "withoutRecommendation",
        title: "Продовжити без рекомендації",
        description: "",
        icon: SaturnIcon,
      },
    ],
    [selectedLanguageTitle]
  );

  const handleBack = () => {
    navigate(PATHS.onboardingDailyGoal);
  };

  const handleContinue = () => {
    if (selected === "recommended") {
      navigate(PATHS.onboardingRunLesson);
      return;
    }

    if (selected === "withoutRecommendation") {
      navigate(PATHS.onboardingPreCreateProf);
    }
  };

  const isRecommendedHighlighted = selected === "recommended" || hovered === "recommended";
  const isWithoutRecommendationHighlighted = selected === "withoutRecommendation" || hovered === "withoutRecommendation";
  const showRecommendedBadge = !isRecommendedHighlighted;

  return (
    <div className={styles.viewport}>
      <div ref={stageRef} className={styles.stage} onClick={() => setSelected("")}>
        <div
          className={styles.bg}
          style={{
            backgroundImage: `url(${BgLeft}), url(${BgRight})`,
          }}
        />

        <div className={styles.bottomShade} />

        <button className={styles.backBtn} type="button" onClick={(e) => { e.stopPropagation(); handleBack(); }}>
          <img className={styles.backIcon} src={ArrowPrev} alt="back" />
        </button>

        <div className={styles.progressTrack}>
          <div className={styles.progressFill} />
        </div>

        <img className={styles.mascot} src={Mascot} alt="" />
        <img className={styles.bubble} src={Bubble} alt="" />

        <p className={styles.bubbleText}>
          <span>Спробуй базовий урок</span>
          <span>— це лише хвилинка!</span>
        </p>

        <div className={styles.cards}>
          <button
            type="button"
            className={`${styles.cardBtn} ${isRecommendedHighlighted ? styles.cardBtnActive : ""}`}
            onClick={(e) => {
              e.stopPropagation();
              setSelected((prev) => prev === "recommended" ? "" : "recommended");
            }}
            onMouseEnter={() => setHovered("recommended")}
            onMouseLeave={() => setHovered("")}
          >
            {showRecommendedBadge ? <div className={styles.recommendedBadge}>РЕКОМЕНДОВАНО</div> : null}

            <div className={styles.cardInner}>
              <img className={styles.cardIconBooks} src={cards[0].icon} alt="" />

              <div className={styles.cardTextBlock}>
                <div className={styles.cardTitle}>{cards[0].title}</div>
                <div className={styles.cardDescription}>{cards[0].description}</div>
              </div>
            </div>
          </button>

          <button
            type="button"
            className={`${styles.cardBtn} ${styles.cardBtnSecondary} ${isWithoutRecommendationHighlighted ? styles.cardBtnActive : ""}`}
            onClick={(e) => {
              e.stopPropagation();
              setSelected((prev) => prev === "withoutRecommendation" ? "" : "withoutRecommendation");
            }}
            onMouseEnter={() => setHovered("withoutRecommendation")}
            onMouseLeave={() => setHovered("")}
          >
            <div className={styles.cardInner}>
              <img className={styles.cardIconSaturn} src={cards[1].icon} alt="" />
              <div className={styles.cardTitle}>{cards[1].title}</div>
            </div>
          </button>
        </div>

        <button
          className={styles.continueBtn}
          type="button"
          disabled={!selected}
          onClick={(e) => { e.stopPropagation(); handleContinue(); }}
        >
          ПРОДОВЖИТИ
        </button>
      </div>
    </div>
  );
}
