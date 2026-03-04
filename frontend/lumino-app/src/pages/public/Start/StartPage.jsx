import { useEffect, useRef, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../routes/paths.js";
import styles from "./StartPage.module.css";

import BgLeft from "../../../assets/backgrounds/bg-left@2x.png";
import BgRight from "../../../assets/backgrounds/bg-right@2x.png";

import Mascot from "../../../assets/mascot/mascot.svg";
import ArrowLeft from "../../../assets/icons/arrow-left.svg";
import ArrowRight from "../../../assets/icons/arrow-right.svg";

import FlagEn from "../../../assets/flags/flag-en.svg";
import FlagDe from "../../../assets/flags/flag-de.svg";
import FlagIt from "../../../assets/flags/flag-it.svg";
import FlagEs from "../../../assets/flags/flag-es.svg";
import FlagFr from "../../../assets/flags/flag-fr.svg";
import FlagPl from "../../../assets/flags/flag-pl.svg";
import FlagJa from "../../../assets/flags/flag-ja.svg";
import FlagKo from "../../../assets/flags/flag-ko.svg";
import FlagZn from "../../../assets/flags/flag-zn.svg";

const LANGS = [
  { code: "en", label: "АНГЛІЙСЬКА", src: FlagEn },
  { code: "de", label: "НІМЕЦЬКА", src: FlagDe },
  { code: "it", label: "ІТАЛІЙСЬКА", src: FlagIt },
  { code: "es", label: "ІСПАНСЬКА", src: FlagEs },
  { code: "fr", label: "ФРАНЦУЗЬКА", src: FlagFr },
  { code: "pl", label: "ПОЛЬСЬКА", src: FlagPl },
  { code: "ja", label: "ЯПОНСЬКА", src: FlagJa },
  { code: "ko", label: "КОРЕЙСЬКА", src: FlagKo },
  { code: "zn", label: "КИТАЙСЬКА", src: FlagZn },
];

export default function Start() {
  const navigate = useNavigate();
  const stageRef = useRef(null);

  const itemsRef = useRef(null);
  const itemRefs = useRef([]);
  const activeIndexRef = useRef(0);
  const rafRef = useRef(0);

  const [activeIndex, setActiveIndex] = useState(0);
  const [selected, setSelected] = useState(null);

  useEffect(() => {
    activeIndexRef.current = activeIndex;
  }, [activeIndex]);

  useEffect(() => {
    const stage = stageRef.current;
    if (!stage) return;

    const resize = () => {
      const w = 1920;
      const h = 1080;

      const sx = window.innerWidth / w;
      const sy = window.innerHeight / h;
      const s = Math.min(sx, sy);

      stage.style.transform = `scale(${s})`;
      stage.style.left = `${(window.innerWidth - w * s) / 2}px`;
      stage.style.top = `${(window.innerHeight - h * s) / 2}px`;
      stage.style.position = "absolute";
    };

    resize();
    window.addEventListener("resize", resize);

    return () => window.removeEventListener("resize", resize);
  }, []);

  const scrollToIndex = useCallback((idx, behavior = "smooth") => {
    const el = itemsRef.current;
    if (!el) return;
    const item = itemRefs.current[idx];
    if (!item) return;

    const max = Math.max(0, el.scrollWidth - el.clientWidth);
    const viewportCenter = el.clientWidth / 2;
    const itemCenter = item.offsetLeft + item.offsetWidth / 2;
    const left = Math.max(0, Math.min(max, itemCenter - viewportCenter));

    el.scrollTo({ left, behavior });
  }, []);

  const onPrev = useCallback(() => {
    const cur = activeIndexRef.current;
    const el = itemsRef.current;
    const atStart = !el || el.scrollLeft <= 2;
    const next = atStart || cur === 0 ? LANGS.length - 1 : cur - 1;
    setActiveIndex(next);
    scrollToIndex(next, "smooth");
  }, [scrollToIndex]);

  const onNext = useCallback(() => {
    const cur = activeIndexRef.current;
    const el = itemsRef.current;
    const max = el ? Math.max(0, el.scrollWidth - el.clientWidth) : 0;
    const atEnd = !el || el.scrollLeft >= max - 2;
    const next = atEnd || cur === LANGS.length - 1 ? 0 : cur + 1;
    setActiveIndex(next);
    scrollToIndex(next, "smooth");
  }, [scrollToIndex]);

  const onStart = () => {
    if (!selected) return;
    try {
      localStorage.setItem("lumino.language", selected);
    } catch {
      // ignore
    }
    navigate(PATHS.onboarding);
  };

  useEffect(() => {
    scrollToIndex(0, "auto");
  }, [scrollToIndex]);

  const onScroll = useCallback(() => {
    const el = itemsRef.current;
    if (!el) return;

    if (rafRef.current) cancelAnimationFrame(rafRef.current);
    rafRef.current = requestAnimationFrame(() => {
      const centerX = el.scrollLeft + el.clientWidth / 2;
      let bestIdx = 0;
      let bestDist = Number.POSITIVE_INFINITY;

      for (let i = 0; i < LANGS.length; i++) {
        const item = itemRefs.current[i];
        if (!item) continue;

        const itemCenter = item.offsetLeft + item.offsetWidth / 2;
        const d = Math.abs(itemCenter - centerX);
        if (d < bestDist) {
          bestDist = d;
          bestIdx = i;
        }
      }

      setActiveIndex(bestIdx);
    });
  }, []);

  return (
    <div className={styles.viewport}>
      <div className={styles.stage} ref={stageRef}>
        <img className={styles.bgLeft} src={BgLeft} alt="" aria-hidden="true" />
        <img className={styles.bgRight} src={BgRight} alt="" aria-hidden="true" />

        <div className={styles.logo} aria-label="LUMINO">
          LUMINO
        </div>

        <img className={styles.mascot} src={Mascot} alt="" aria-hidden="true" />

        <div className={styles.content}>
          <div className={styles.title}>Навчайся легко та цікаво!</div>
          <div className={styles.subtitle}>Щоб розпочати оберіть мову</div>

          <button className={styles.startBtn} type="button" disabled={!selected} onClick={onStart}>
            РОЗПОЧАТИ
          </button>

          <button className={styles.accountBtn} type="button" onClick={() => navigate(PATHS.login)}>
            УЖЕ МАЮ ОБЛІКОВИЙ ЗАПИС
          </button>
        </div>

        <div className={styles.carousel}>
          <button
            className={`${styles.arrowBtn} ${styles.arrowLeft}`}
            type="button"
            onClick={onPrev}
            aria-label="Попередні мови"
          >
            <img src={ArrowLeft} alt="" aria-hidden="true" />
          </button>

          <div className={styles.itemsViewport}>
            <div className={styles.items} role="list" ref={itemsRef} onScroll={onScroll}>
              {LANGS.map((l, idx) => (
                <button
                  key={l.code}
                  ref={(el) => (itemRefs.current[idx] = el)}
                  type="button"
                  className={`${styles.item} ${selected === l.code ? styles.itemActive : ""}`}
                  onClick={() => setSelected(l.code)}
                  role="listitem"
                >
                  <img className={styles.flag} src={l.src} alt="" aria-hidden="true" />
                  <span className={styles.langLabel}>{l.label}</span>
                </button>
              ))}
            </div>
          </div>

          <button
            className={`${styles.arrowBtn} ${styles.arrowRight}`}
            type="button"
            onClick={onNext}
            aria-label="Наступні мови"
          >
            <img src={ArrowRight} alt="" aria-hidden="true" />
          </button>
        </div>
      </div>
    </div>
  );
}
