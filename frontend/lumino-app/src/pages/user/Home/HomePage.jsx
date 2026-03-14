import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PATHS } from "../../../routes/paths.js";
import { authStorage } from "../../../services/authStorage.js";
import { useStageScale } from "../../../hooks/useStageScale.js";
import { onboardingService } from "../../../services/onboardingService.js";
import { coursesService } from "../../../services/coursesService.js";
import { learningService } from "../../../services/learningService.js";
import { userService } from "../../../services/userService.js";
import { streakService } from "../../../services/streakService.js";
import { scenesService } from "../../../services/scenesService.js";
import GlassModal from "../../../components/common/GlassModal/GlassModal.jsx";
import GlassLoading from "../../../components/common/GlassLoading/GlassLoading.jsx";
import styles from "./HomePage.module.css";

import FlagEn from "../../../assets/flags/flag-en.svg";
import FlagDe from "../../../assets/flags/flag-de.svg";
import FlagIt from "../../../assets/flags/flag-it.svg";
import FlagEs from "../../../assets/flags/flag-es.svg";
import FlagFr from "../../../assets/flags/flag-fr.svg";
import FlagPl from "../../../assets/flags/flag-pl.svg";
import FlagJa from "../../../assets/flags/flag-ja.svg";
import FlagKo from "../../../assets/flags/flag-ko.svg";
import FlagZh from "../../../assets/flags/flag-zn.svg";
import { HOME_BG_LEFT, HOME_HEADER_ICONS, HOME_NAV_ICONS, HOME_ORBIT_ASSETS, HOME_SHARED_ASSETS } from "./homeAssets.js";
import ArrowPrevious from "../../../assets/icons/arrow-previous.svg";
import ProgressTrackIcon from "../../../assets/home/shared/progress track.svg";
import MascotModal from "../../../assets/mascot/mascotmodal.svg";

const FLAG_MAP = {
  en: FlagEn,
  de: FlagDe,
  it: FlagIt,
  es: FlagEs,
  fr: FlagFr,
  pl: FlagPl,
  ja: FlagJa,
  ko: FlagKo,
  zh: FlagZh,
};

const LANGUAGE_LABELS = {
  en: "Англійська",
  de: "Німецька",
  it: "Італійська",
  es: "Іспанська",
  fr: "Французька",
  pl: "Польська",
  ja: "Японська",
  ko: "Корейська",
  zh: "Китайська",
};

const NAV_ITEMS = [
  { key: "learning", label: "НАВЧАННЯ" },
  { key: "achievements", label: "НАГОРОДИ" },
  { key: "dictionary", label: "СЛОВНИК" },
  { key: "profile", label: "ПРОФІЛЬ" },
];

const HEADER_COUNTERS = {
  flag: { x: 1228, y: 40, w: 98, h: 72 },
  star: { x: 1377, y: 39, w: 129, h: 66 },
  crystal: { x: 1572, y: 57, w: 114.34, h: 39.86 },
  energy: { x: 1732, y: 39, w: 149, h: 69 },
};

const ORBIT_SEQUENCE = [1, 2, 3, 1, 2, 3, 1, 2, 3, 1];
const SECTION_WIDTH = 879.47;
const FULL_PANEL_VIEWS = ["languages", "courses", "scenes", "lesson"];
const TOP_BAR_HIDDEN_VIEWS = ["languages", "lesson"];
const WEEK_DAYS = ["ПН", "ВТ", "СР", "ЧТ", "ПТ", "СБ", "НД"];
const LANGUAGE_ORDER = ["en", "de", "it", "es", "fr", "pl", "ja", "ko", "zh"];

const COURSE_TOPICS_COUNT = 10;
const COURSE_LESSONS_PER_TOPIC = 8;
const COURSE_EXERCISES_PER_LESSON = 9;
const COURSE_SCENES_PER_TOPIC = 1;

const ORBIT_LAYOUTS = {
  1: {
    lessons: [
      { left: 390, top: 180, width: 43, height: 43 },
      { left: 200, top: 290, width: 66, height: 66 },
      { left: 445, top: 420, width: 70, height: 68 },
      { left: 65, top: 375, width: 37, height: 37 },
      { left: 210, top: 520, width: 38, height: 38 },
      { left: 590, top: 510, width: 75, height: 75 },
      { left: 400, top: 630, width: 64, height: 64 },
      { left: 145, top: 685, width: 51, height: 48 },
    ],
    sun: { left: 297, top: 431, width: 107, height: 107 },
    decor: [
      { left: 225, top: 220, size: 15, accent: "#86A8D3" },
      { left: 527, top: 260, size: 11, accent: "#AACDF1" },
      { left: 302, top: 280, size: 16, accent: "#C38CC4" },
      { left: 391, top: 354, size: 12, accent: "#A8D4FF" },
      { left: 520, top: 385, size: 16, accent: "#D9A4D0" },
      { left: 278, top: 423, size: 10, accent: "#C18FCB" },
      { left: 64, top: 437, size: 13, accent: "#6D97C9" },
      { left: 210, top: 470, size: 16, accent: "#5A7D99" },
      { left: 391, top: 520, size: 15, accent: "#86B4E5" },
      { left: 306, top: 562, size: 10, accent: "#E4B7CF" },
      { left: 173, top: 584, size: 17, accent: "#E0B2CF" },
      { left: 422, top: 586, size: 14, accent: "#79A4DB" },
      { left: 378, top: 678, size: 9, accent: "#C792C4" },
      { left: 507, top: 707, size: 18, accent: "#74A4D7" },
      { left: 250, top: 747, size: 12, accent: "#B6D7FF" },
    ],
  },
  2: {
    lessons: [
      { left: 440, top: 200, width: 46, height: 46 },
      { left: 190, top: 270, width: 95, height: 95 },
      { left: 60, top: 350, width: 50, height: 50 },
      { left: 600, top: 390, width: 55, height: 55 },
      { left: 445, top: 410, width: 63, height: 63 },
      { left: 340, top: 600, width: 40, height: 40 },
      { left: 465, top: 590, width: 70, height: 70 },
      { left: 100, top: 640, width: 70, height: 70 },
    ],
    sun: { left: 297, top: 431, width: 107, height: 107 },
    decor: [
      { left: 225, top: 220, size: 15, accent: "#86A8D3" },
      { left: 527, top: 260, size: 11, accent: "#AACDF1" },
      { left: 302, top: 280, size: 16, accent: "#C38CC4" },
      { left: 391, top: 354, size: 12, accent: "#A8D4FF" },
      { left: 520, top: 385, size: 16, accent: "#D9A4D0" },
      { left: 278, top: 423, size: 10, accent: "#C18FCB" },
      { left: 64, top: 437, size: 13, accent: "#6D97C9" },
      { left: 210, top: 470, size: 16, accent: "#5A7D99" },
      { left: 391, top: 520, size: 15, accent: "#86B4E5" },
      { left: 306, top: 562, size: 10, accent: "#E4B7CF" },
      { left: 173, top: 584, size: 17, accent: "#E0B2CF" },
      { left: 422, top: 586, size: 14, accent: "#79A4DB" },
      { left: 378, top: 678, size: 9, accent: "#C792C4" },
      { left: 507, top: 707, size: 18, accent: "#74A4D7" },
      { left: 250, top: 747, size: 12, accent: "#B6D7FF" },
    ],
  },
  3: {
    lessons: [
      { left: 170, top: 230, width: 40, height: 40 },
      { left: 380, top: 240, width: 120, height: 120 },
      { left: 560, top: 300, width: 70, height: 70 },
      { left: 160, top: 350, width: 50, height: 50 },
      { left: 450, top: 420, width: 65, height: 65 },
      { left: 25, top: 520, width: 100, height: 100 },
      { left: 270, top: 650, width: 60, height: 60 },
      { left: 545, top: 636, width: 60, height: 60 },
    ],
    sun: { left: 297, top: 431, width: 107, height: 107 },
    decor: [
      { left: 225, top: 220, size: 15, accent: "#86A8D3" },
      { left: 527, top: 260, size: 11, accent: "#AACDF1" },
      { left: 302, top: 280, size: 16, accent: "#C38CC4" },
      { left: 391, top: 354, size: 12, accent: "#A8D4FF" },
      { left: 520, top: 385, size: 16, accent: "#D9A4D0" },
      { left: 278, top: 423, size: 10, accent: "#C18FCB" },
      { left: 64, top: 437, size: 13, accent: "#6D97C9" },
      { left: 210, top: 470, size: 16, accent: "#5A7D99" },
      { left: 391, top: 520, size: 15, accent: "#86B4E5" },
      { left: 306, top: 562, size: 10, accent: "#E4B7CF" },
      { left: 173, top: 584, size: 17, accent: "#E0B2CF" },
      { left: 422, top: 586, size: 14, accent: "#79A4DB" },
      { left: 378, top: 678, size: 9, accent: "#C792C4" },
      { left: 507, top: 707, size: 18, accent: "#74A4D7" },
      { left: 250, top: 747, size: 12, accent: "#B6D7FF" },
    ],
  },
};

const GUEST_USER = {
  hearts: 5,
  heartsMax: 5,
  crystals: 500,
  currentStreakDays: 15,
  bestStreakDays: 24,
  heartRegenMinutes: 30,
  crystalCostPerHeart: 15,
  nextHeartInSeconds: 0,
};

function normalizeCode(code) {
  return String(code || "").trim().toLowerCase();
}

function isUserNotFoundResponse(res) {
  if (!res || res.ok) return false;

  const type = normalizeCode(res.data?.type);
  const detail = normalizeCode(res.data?.detail || res.error);

  return res.status === 404 && type === "not_found" && detail === "user not found";
}

function isAuthExpiredResponse(res) {
  if (!res || res.ok) return false;

  return res.status === 401 || isUserNotFoundResponse(res);
}

function getFlagByCode(code) {
  return FLAG_MAP[normalizeCode(code)] || FlagEn;
}

function getLanguageLabel(code) {
  return LANGUAGE_LABELS[normalizeCode(code)] || String(code || "").toUpperCase();
}

function buildGuestTopics() {
  const names = [
    "Buy clothes",
    "Food",
    "Travel",
    "Family",
    "Work",
    "Hobbies",
    "Health",
    "Shopping",
    "Weather",
    "Daily life",
  ];

  return names.map((title, index) => ({
    id: index + 1,
    order: index + 1,
    title,
    lessons: Array.from({ length: 8 }, (_, lessonIndex) => ({
      id: index * 100 + lessonIndex + 1,
      order: lessonIndex + 1,
      title: `Lesson ${lessonIndex + 1}`,
      isUnlocked: index === 0 && lessonIndex === 0,
      isPassed: false,
    })),
    scene: {
      id: index + 1,
      order: index + 1,
      title: `Сцена ${index + 1}`,
      isUnlocked: false,
      isCompleted: false,
      unlockReason: "Щоб відкрити сцену, потрібно пройти всі 8 уроків цієї теми.",
    },
  }));
}

function getOrbitType(index) {
  return ORBIT_SEQUENCE[index] || 1;
}

function getOrbitBundle(index) {
  return HOME_ORBIT_ASSETS[getOrbitType(index)];
}

function formatCourseBadge(course, topicIndex) {
  const level = String(course?.level || course?.title || "A1").match(/A1|A2|B1|B2|C1|C2/i)?.[0]?.toUpperCase() || "A1";
  return `${level}, ТЕМА ${topicIndex + 1}`;
}
function getCourseLevel(course) {
  return String(course?.level || course?.title || "A1").match(/A1|A2|B1|B2|C1|C2/i)?.[0]?.toUpperCase() || "A1";
}

function getCourseTotals(pathData) {
  const topicsCount = Math.max(Number(pathData?.topics?.length || 0), COURSE_TOPICS_COUNT);
  const lessonsCount = Math.max(
    Array.isArray(pathData?.topics)
      ? pathData.topics.reduce((sum, topic) => sum + Number(topic?.lessons?.length || 0), 0)
      : 0,
    topicsCount * COURSE_LESSONS_PER_TOPIC
  );
  const scenesCount = Math.max(Number(pathData?.scenes?.length || 0), topicsCount * COURSE_SCENES_PER_TOPIC);
  const totalSteps = (lessonsCount * COURSE_EXERCISES_PER_LESSON) + scenesCount;

  return {
    topicsCount,
    lessonsCount,
    scenesCount,
    totalSteps,
  };
}

function getCourseProgressPercent(courseItem, pathData, isSelectedCourse) {
  if (isSelectedCourse && pathData) {
    const totals = getCourseTotals(pathData);
    const passedLessons = Array.isArray(pathData?.topics)
      ? pathData.topics.reduce((sum, topic) => sum + topic.lessons.filter((lesson) => lesson?.isPassed).length, 0)
      : 0;
    const completedScenes = Array.isArray(pathData?.scenes)
      ? pathData.scenes.filter((scene) => scene?.isCompleted).length
      : 0;
    const completedSteps = (passedLessons * COURSE_EXERCISES_PER_LESSON) + completedScenes;

    if (!totals.totalSteps) {
      return 0;
    }

    return Math.max(0, Math.min(100, (completedSteps / totals.totalSteps) * 100));
  }

  return Math.max(0, Math.min(100, Number(courseItem?.completionPercent || 0)));
}


function buildMonth(year, month, activeDates) {
  const first = new Date(year, month - 1, 1);
  const last = new Date(year, month, 0);
  const prevLast = new Date(year, month - 1, 0);
  const startWeekDay = (first.getDay() + 6) % 7;
  const totalDays = last.getDate();
  const activeSet = new Set(activeDates);
  const cells = [];

  for (let i = startWeekDay - 1; i >= 0; i -= 1) {
    cells.push({
      key: `prev-${i}`,
      day: prevLast.getDate() - i,
      active: false,
      muted: true,
      currentMonth: false,
    });
  }

  for (let day = 1; day <= totalDays; day += 1) {
    const iso = `${year}-${String(month).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
    cells.push({
      key: iso,
      day,
      active: activeSet.has(iso),
      muted: false,
      currentMonth: true,
    });
  }

  let nextDay = 1;

  while (cells.length % 7 !== 0) {
    cells.push({
      key: `next-${nextDay}`,
      day: nextDay,
      active: false,
      muted: true,
      currentMonth: false,
    });
    nextDay += 1;
  }

  return cells;
}

function extractCalendarDates(data) {
  if (!data) return [];

  if (Array.isArray(data.days)) {
    return data.days
      .filter((item) => item?.isCompleted || item?.completed || item?.active)
      .map((item) => String(item.date || item.day || ""))
      .filter(Boolean);
  }

  if (Array.isArray(data.items)) {
    return data.items
      .filter((item) => item?.isCompleted || item?.completed || item?.active)
      .map((item) => String(item.date || item.day || ""))
      .filter(Boolean);
  }

  return [];
}

function getWeekProgress(days) {
  const safeDays = Math.max(0, Number(days || 0));

  if (safeDays === 0) {
    return {
      filled: 0,
    };
  }

  const weekProgress = ((safeDays - 1) % 7) + 1;
  return {
    filled: weekProgress,
  };
}

function normalizeCoursePath(data) {
  if (!data) return null;

  return {
    topics: Array.isArray(data.topics) ? data.topics : [],
    scenes: Array.isArray(data.scenes) ? data.scenes : [],
  };
}

function resolveMediaUrl(value) {
  const src = String(value || "").trim();

  if (!src) {
    return "";
  }

  if (/^(https?:)?\/\//i.test(src) || src.startsWith("data:") || src.startsWith("blob:")) {
    return src;
  }

  const origin = typeof window !== "undefined" ? window.location.origin : "";

  if (src.startsWith("/")) {
    return `${origin}${src}`;
  }

  const apiBase = String(import.meta.env.VITE_API_BASE_URL || "/api").trim();

  if (/^https?:\/\//i.test(apiBase)) {
    const root = apiBase.replace(/\/api\/?$/i, "").replace(/\/$/, "");
    return `${root}/${src.replace(/^\/+/, "")}`;
  }

  return `${origin}/${src.replace(/^\/+/, "")}`;
}

function getSceneLockedMessage(scene) {
  const topicOrder = Number(scene?.topicOrder || scene?.order || 0);

  if (topicOrder > 0) {
    return `ПРОЙДІТЬ ТЕМУ ${topicOrder}, ЩОБ РОЗБЛОКУВАТИ СЦЕНУ`;
  }

  return "ПРОЙДІТЬ ТЕМУ, ЩОБ РОЗБЛОКУВАТИ СЦЕНУ";
}

function getTopicLockedMessage(topic, index) {
  const topicOrder = Number(index) + 1;
  const previousTopicOrder = topicOrder > 1 ? topicOrder - 1 : 1;

  if (topicOrder > 1) {
    return `ПРОЙДІТЬ ТЕМУ ${previousTopicOrder} ТА ЇЇ СЦЕНУ, ЩОБ ВІДКРИТИ ТЕМУ ${topicOrder}`;
  }

  return "ЦЯ ТЕМА ЩЕ ЗАКРИТА";
}

function getSceneCardLabel(scene, index) {
  const title = String(scene?.title || scene?.Title || "").trim();

  if (title) {
    return title;
  }

  const topicOrder = Number(scene?.topicOrder || scene?.order || index + 1);

  if (topicOrder > 0) {
    return `Сцена ${topicOrder}`;
  }

  return "Сцена";
}

function normalizeUser(data) {
  if (!data) return null;

  return {
    hearts: data.hearts ?? data.heartsCount ?? GUEST_USER.hearts,
    heartsMax: data.heartsMax ?? data.maxHearts ?? GUEST_USER.heartsMax,
    crystals: data.crystals ?? data.crystalsCount ?? GUEST_USER.crystals,
    currentStreakDays: data.currentStreakDays ?? GUEST_USER.currentStreakDays,
    bestStreakDays: data.bestStreakDays ?? GUEST_USER.bestStreakDays,
    heartRegenMinutes: data.heartRegenMinutes ?? GUEST_USER.heartRegenMinutes,
    crystalCostPerHeart: data.crystalCostPerHeart ?? GUEST_USER.crystalCostPerHeart,
    nextHeartInSeconds: data.nextHeartInSeconds ?? GUEST_USER.nextHeartInSeconds,
  };
}

function HeaderIcon({ src, alt = "" }) {
  return <img className={styles.headerSvg} src={src} alt={alt} aria-hidden="true" />;
}

function NavIcon({ type }) {
  return <img className={styles.navSvg} src={HOME_NAV_ICONS[type]} alt="" aria-hidden="true" />;
}

function OrbitSection({
  item,
  index,
  course,
  isGuest,
  onTitleClick,
  onSceneButtonClick,
  onSceneSunClick,
  onLessonClick,
}) {
  const orbitType = getOrbitType(index);
  const layout = ORBIT_LAYOUTS[orbitType];
  const bundle = getOrbitBundle(index);
  const dividerDone = Boolean(item?.scene?.isCompleted);
  const isTopicUnlocked = isGuest
    ? index === 0
    : Boolean(item?.lessons?.some((lesson) => lesson?.isUnlocked) || item?.scene?.isUnlocked);

  return (
    <section className={styles.section} style={{ left: `${SECTION_WIDTH * index}px` }}>
      <button
        type="button"
        className={`${styles.topicCard} ${isTopicUnlocked ? "" : styles.topicCardLocked}`}
        onMouseDown={(event) => event.stopPropagation()}
        onClick={() => onTitleClick(item, !isTopicUnlocked, index)}
      >
        <span className={styles.topicMeta}>{formatCourseBadge(course, index)}</span>
        <span className={styles.topicTitle}>{item?.title || `Topic ${index + 1}`}</span>
      </button>

      <div className={styles.orbitBox}>
        <div className={`${styles.ring} ${styles.ring1}`} />
        <div className={`${styles.ring} ${styles.ring2}`} />
        <div className={`${styles.ring} ${styles.ring3}`} />
        <div className={`${styles.ring} ${styles.ring4}`} />
        <div className={`${styles.ring} ${styles.ring5}`} />

        {layout.decor.map((decor) => (
          <span
            key={`${index}-${decor.left}-${decor.top}`}
            className={styles.decor}
            style={{
              left: `${decor.left}px`,
              top: `${decor.top}px`,
              width: `${decor.size}px`,
              height: `${decor.size}px`,
              background: decor.accent,
            }}
          />
        ))}

        {bundle.lessons.map((lessonImage, lessonIndex) => {
          const lesson = item?.lessons?.[lessonIndex];
          const isUnlocked = isGuest ? index === 0 && lessonIndex === 0 : Boolean(lesson?.isUnlocked);
          const isPassed = Boolean(lesson?.isPassed);
          const lessonLayout = layout.lessons[lessonIndex];

          return (
            <button
              key={`${item?.id || index}-lesson-${lessonIndex + 1}`}
              type="button"
              disabled={!isGuest && !isUnlocked}
              className={`${styles.lessonButton} ${isUnlocked ? styles.lessonButtonOpen : styles.lessonButtonLocked}`}
              style={{
                left: `${lessonLayout.left}px`,
                top: `${lessonLayout.top}px`,
                width: `${lessonLayout.width}px`,
                height: `${lessonLayout.height}px`,
              }}
              onMouseDown={(event) => event.stopPropagation()}
              onClick={() => onLessonClick(item, lesson, !isUnlocked)}
            >
              <img className={styles.lessonImage} src={lessonImage} alt="" aria-hidden="true" />
              {isPassed ? <span className={styles.lessonPassedDot} /> : null}
            </button>
          );
        })}

        <button
          type="button"
          disabled={!isGuest && !item?.scene?.isUnlocked}
          className={`${styles.sunButton} ${item?.scene?.isUnlocked ? styles.sunButtonOpen : styles.sunButtonLocked}`}
          style={{
            left: `${layout.sun.left}px`,
            top: `${layout.sun.top}px`,
            width: `${layout.sun.width}px`,
            height: `${layout.sun.height}px`,
          }}
          onMouseDown={(event) => event.stopPropagation()}
          onClick={() => onSceneSunClick(item, item?.scene, !item?.scene?.isUnlocked)}
        >
          <img className={styles.sunImage} src={bundle.sun} alt="" aria-hidden="true" />
        </button>
      </div>

      <button
        type="button"
        className={`${styles.sceneButton} ${isTopicUnlocked ? "" : styles.sceneButtonLockedState}`}
        onMouseDown={(event) => event.stopPropagation()}
        onClick={() => onSceneButtonClick(item, !isTopicUnlocked, index)}
      >
        <img
          className={styles.sceneButtonImage}
          src={HOME_SHARED_ASSETS.sceneButton}
          alt=""
          aria-hidden="true"
        />
      </button>

      {index < 9 ? (
        <div className={styles.sectionDivider}>
          <div className={styles.dividerTop} />
          <div className={styles.dividerIcon}>
            <img src={dividerDone ? HOME_SHARED_ASSETS.unlockedStar : HOME_SHARED_ASSETS.lock} alt="" aria-hidden="true" />
          </div>
          <div className={styles.dividerBottom} />
        </div>
      ) : null}
    </section>
  );
}

function FullPanel({ title, subtitle, onClose, children }) {
  return (
    <div className={styles.panelWrap}>
      <div className={styles.panelCard}>
        <button type="button" className={styles.panelClose} onClick={onClose}>
          ×
        </button>
        <h2 className={styles.panelTitle}>{title}</h2>
        <p className={styles.panelSubtitle}>{subtitle}</p>
        <div className={styles.panelContent}>{children}</div>
      </div>
    </div>
  );
}

export default function HomePage() {
  const navigate = useNavigate();
  const stageRef = useRef(null);
  const trackRef = useRef(null);
  const dragRef = useRef({ active: false, startX: 0, startLeft: 0 });
  const dropdownRef = useRef(null);

  useStageScale(stageRef, { mode: "absolute" });

  const [isSessionExpired, setIsSessionExpired] = useState(false);
  const isGuest = isSessionExpired || !authStorage.isAuthed();
  const [loading, setLoading] = useState(true);
  const [restoringHearts, setRestoringHearts] = useState(false);
  const [activeNav, setActiveNav] = useState("learning");
  const [bodyView, setBodyView] = useState("learning");
  const [openDropdown, setOpenDropdown] = useState("");
  const [showFullCalendar, setShowFullCalendar] = useState(false);
  const [modal, setModal] = useState({ open: false, title: "", message: "", primaryText: "Добре", secondaryText: "", onPrimary: null, onSecondary: null, variant: "default", illustrationSrc: "" });
  const [user, setUser] = useState(GUEST_USER);
  const [languageState, setLanguageState] = useState({ activeTargetLanguageCode: localStorage.getItem("targetLanguage") || "en", learningLanguages: [] });
  const [supportedLanguages, setSupportedLanguages] = useState([]);
  const [courses, setCourses] = useState([]);
  const [course, setCourse] = useState(null);
  const [path, setPath] = useState(null);
  const [selectedTopic, setSelectedTopic] = useState(null);
  const [selectedLesson, setSelectedLesson] = useState(null);
  const [scenesOverview, setScenesOverview] = useState([]);
  const [calendarDates, setCalendarDates] = useState([]);
  const [calendarMonth, setCalendarMonth] = useState({ year: new Date().getFullYear(), month: new Date().getMonth() + 1 });

  const topics = useMemo(() => {
    if (path?.topics?.length) {
      return path.topics.slice(0, 10).map((topic, index) => {
        const scene = path.scenes?.find((item) => item?.topicId === topic.id) || {
          id: index + 1,
          order: index + 1,
          title: `Сцена ${index + 1}`,
          isUnlocked: false,
          isCompleted: false,
          unlockReason: "Щоб відкрити сцену, потрібно пройти всі уроки цієї теми.",
        };

        return {
          ...topic,
          scene,
        };
      });
    }

    return buildGuestTopics();
  }, [path]);

  useEffect(() => {
    if (!showFullCalendar) {
      return;
    }

    const handleEscClose = (e) => {
      if (e.key === "Escape") {
        setShowFullCalendar(false);
      }
    };

    window.addEventListener("keydown", handleEscClose);

    return () => {
      window.removeEventListener("keydown", handleEscClose);
    };
  }, [showFullCalendar]);

  const activeLanguageCode = normalizeCode(languageState.activeTargetLanguageCode || course?.languageCode || localStorage.getItem("targetLanguage") || "en");
  const monthCells = useMemo(() => buildMonth(calendarMonth.year, calendarMonth.month, calendarDates), [calendarDates, calendarMonth]);
  const streakProgress = useMemo(() => getWeekProgress(user.currentStreakDays), [user.currentStreakDays]);
  const energySteps = useMemo(() => Math.max(1, Number(user.heartsMax || 5)), [user.heartsMax]);
  const isFullPanelView = FULL_PANEL_VIEWS.includes(bodyView);
  const isTopBarHidden = TOP_BAR_HIDDEN_VIEWS.includes(bodyView);

  const languageItems = useMemo(() => {
    if (Array.isArray(languageState.learningLanguages) && languageState.learningLanguages.length > 0) {
      return languageState.learningLanguages;
    }

    return [{ code: activeLanguageCode, title: getLanguageLabel(activeLanguageCode), isActive: true }];
  }, [activeLanguageCode, languageState.learningLanguages]);

  const availableLanguageItems = useMemo(() => {
    const sourceItems = supportedLanguages.length > 0
      ? supportedLanguages
      : Object.entries(LANGUAGE_LABELS).map(([code, title]) => ({ code, title }));

    return [...sourceItems].sort((a, b) => {
      const firstIndex = LANGUAGE_ORDER.indexOf(normalizeCode(a.code));
      const secondIndex = LANGUAGE_ORDER.indexOf(normalizeCode(b.code));
      const safeFirstIndex = firstIndex === -1 ? Number.MAX_SAFE_INTEGER : firstIndex;
      const safeSecondIndex = secondIndex === -1 ? Number.MAX_SAFE_INTEGER : secondIndex;

      return safeFirstIndex - safeSecondIndex;
    });
  }, [supportedLanguages]);

  const courseItemsForView = useMemo(() => {
    const items = courses.length ? courses : [course].filter(Boolean);

    return [...items].sort((a, b) => Number(a?.order || 0) - Number(b?.order || 0));
  }, [course, courses]);

  const guestPrompt = useCallback(() => {
    setModal({
      open: true,
      title: "Створіть профіль",
      message: "Щоб відкривати уроки, сцени та зберігати прогрес, потрібно увійти або створити профіль.",
      primaryText: "СТВОРИТИ",
      secondaryText: "ПІЗНІШЕ",
      onPrimary: () => navigate(PATHS.register),
      onSecondary: () => setModal((prev) => ({ ...prev, open: false })),
      variant: "default",
      illustrationSrc: "",
    });
  }, [navigate]);

  const closeModal = useCallback(() => {
    setModal((prev) => ({ ...prev, open: false }));
  }, []);

  const showInfo = useCallback((title, message) => {
    setModal({
      open: true,
      title,
      message,
      primaryText: "Добре",
      secondaryText: "",
      onPrimary: null,
      onSecondary: null,
      variant: "default",
      illustrationSrc: "",
    });
  }, []);

  const loadHome = useCallback(async (preferredLanguageCode = "") => {
    setLoading(true);

    try {
      const preferred = normalizeCode(preferredLanguageCode || localStorage.getItem("targetLanguage") || languageState.activeTargetLanguageCode || "en");
      const supportedRes = await onboardingService.getSupportedLanguages();

      if (supportedRes.ok) {
        setSupportedLanguages(supportedRes.items || []);
      }

      if (isGuest) {
        const publicCoursesRes = await coursesService.getPublishedCourses(preferred);
        const publicCourses = publicCoursesRes.items || [];
        const nextCourse = publicCourses[0] || null;
        const today = new Date();
        const todayIso = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;

        setCourses(publicCourses);
        setCourse(nextCourse);
        setPath(null);
        setUser(GUEST_USER);
        setLanguageState({
          activeTargetLanguageCode: preferred,
          learningLanguages: [{ code: preferred, title: getLanguageLabel(preferred), isActive: true }],
        });
        setCalendarMonth({ year: today.getFullYear(), month: today.getMonth() + 1 });
        setCalendarDates([todayIso]);
        return { hasCourses: publicCourses.length > 0, activeTargetLanguageCode: preferred };
      }

      const [userRes, languagesRes] = await Promise.all([userService.getMe(), onboardingService.getMyLanguages()]);

      if (isAuthExpiredResponse(userRes) || isAuthExpiredResponse(languagesRes)) {
        const publicCoursesRes = await coursesService.getPublishedCourses(preferred);
        const publicCourses = publicCoursesRes.items || [];
        const nextCourse = publicCourses[0] || null;
        const today = new Date();
        const todayIso = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}-${String(today.getDate()).padStart(2, "0")}`;

        authStorage.clearTokens();
        setIsSessionExpired(true);
        setCourses(publicCourses);
        setCourse(nextCourse);
        setPath(null);
        setUser(GUEST_USER);
        setLanguageState({
          activeTargetLanguageCode: preferred,
          learningLanguages: [{ code: preferred, title: getLanguageLabel(preferred), isActive: true }],
        });
        setCalendarMonth({ year: today.getFullYear(), month: today.getMonth() + 1 });
        setCalendarDates([todayIso]);
        return { hasCourses: publicCourses.length > 0, activeTargetLanguageCode: preferred };
      }

      const nextUser = normalizeUser(userRes.ok ? userRes.data : null) || GUEST_USER;
      const targetCode = normalizeCode(
        languagesRes?.activeTargetLanguageCode ||
          languagesRes?.data?.activeTargetLanguageCode ||
          preferred ||
          "en"
      );

      setIsSessionExpired(false);
      setUser(nextUser);
      setLanguageState(languagesRes?.ok ? {
        activeTargetLanguageCode: targetCode,
        learningLanguages: languagesRes.learningLanguages || [],
      } : {
        activeTargetLanguageCode: targetCode,
        learningLanguages: [{ code: targetCode, title: getLanguageLabel(targetCode), isActive: true }],
      });

      const myCoursesRes = await coursesService.getMyCourses(targetCode);
      const nextCourses = myCoursesRes.items || [];
      const activeCourse = nextCourses.find((item) => !item?.isLocked) || nextCourses[0] || null;
      setCourses(nextCourses);
      setCourse(activeCourse);
      setPath(null);

      if (activeCourse?.id) {
        const [pathRes, streakRes, calendarRes] = await Promise.all([
          learningService.getMyCoursePath(activeCourse.id),
          streakService.getMyStreak(),
          streakService.getMyCalendarMonth(calendarMonth.year, calendarMonth.month),
        ]);

        if (pathRes.ok) {
          setPath(normalizeCoursePath(pathRes.data));
        }

        if (streakRes.ok && streakRes.data) {
          setUser((prev) => ({
            ...prev,
            currentStreakDays: streakRes.data.currentStreakDays ?? prev.currentStreakDays,
            bestStreakDays: streakRes.data.bestStreakDays ?? prev.bestStreakDays,
          }));
        }

        if (calendarRes.ok) {
          setCalendarDates(extractCalendarDates(calendarRes.data));
        }
      }

      return { hasCourses: nextCourses.length > 0, activeTargetLanguageCode: targetCode };
    } finally {
      setLoading(false);
    }
  }, [calendarMonth.month, calendarMonth.year, isGuest, languageState.activeTargetLanguageCode]);

  useEffect(() => {
    loadHome();
  }, [loadHome]);

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (!dropdownRef.current) return;
      if (dropdownRef.current.contains(event.target)) return;
      setOpenDropdown("");
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleLanguageSwitch = useCallback(async (code) => {
    const nextCode = normalizeCode(code);

    if (!nextCode) return;

    if (isGuest) {
      guestPrompt();
      return;
    }

    if (nextCode === activeLanguageCode) {
      closeModal();
      setOpenDropdown("");
      setBodyView("learning");
      return;
    }

    closeModal();
    setLoading(true);

    try {
      const availableCoursesRes = await coursesService.getPublishedCourses(nextCode);
      const availableCourses = availableCoursesRes.items || [];

      if (!availableCourses.length) {
        showInfo("Для цієї мови поки немає курсів", "Цю мову поки не можна обрати, тому що для неї ще немає наповнення курсами.");
        return;
      }

      const res = await onboardingService.updateMyTargetLanguage(nextCode);

      if (!res.ok) {
        showInfo("Не вдалося змінити мову", res.error || "Спробуй ще раз трохи пізніше.");
        return;
      }

      setOpenDropdown("");
      setBodyView("learning");
      localStorage.setItem("targetLanguage", nextCode);
      await loadHome(nextCode);
    } finally {
      setLoading(false);
    }
  }, [activeLanguageCode, closeModal, guestPrompt, isGuest, loadHome, showInfo]);

  const handleLanguageCardClick = useCallback((code) => {
    const nextCode = normalizeCode(code);

    if (!nextCode || nextCode === activeLanguageCode) {
      return;
    }

    setModal({
      open: true,
      title: "Хочете закінчити\nвивчати цю мову?",
      message: "",
      primaryText: "ТАК",
      secondaryText: "НІ",
      onPrimary: () => handleLanguageSwitch(nextCode),
      onSecondary: () => setModal((prev) => ({ ...prev, open: false })),
      variant: "default",
      illustrationSrc: "",
    });
  }, [activeLanguageCode, handleLanguageSwitch]);

  const handleFullRestoreHearts = useCallback(async () => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    const heartsToRestore = Math.max(0, Number(user.heartsMax || 0) - Number(user.hearts || 0));

    if (!heartsToRestore) {
      showInfo("Енергія повна", "У тебе вже повністю відновлена енергія.");
      return;
    }

    setRestoringHearts(true);

    try {
      const res = await userService.restoreHearts(heartsToRestore);

      if (!res.ok) {
        showInfo("Не вдалося відновити енергію", res.error || "Спробуй ще раз трохи пізніше.");
        return;
      }

      setUser((prev) => ({ ...prev, ...normalizeUser(res.data) }));
      setOpenDropdown("");
    } finally {
      setRestoringHearts(false);
    }
  }, [guestPrompt, isGuest, showInfo, user.hearts, user.heartsMax]);

  const handleRestoreOneHeart = useCallback(async () => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    if (Number(user.hearts || 0) >= Number(user.heartsMax || 0)) {
      showInfo("Енергія повна", "Тобі зараз не потрібно відновлювати ще одну енергію.");
      return;
    }

    setRestoringHearts(true);

    try {
      const res = await userService.restoreHearts(1);

      if (!res.ok) {
        showInfo("Не вдалося додати +1", res.error || "Спробуй ще раз трохи пізніше.");
        return;
      }

      setUser((prev) => ({ ...prev, ...normalizeUser(res.data) }));
      setOpenDropdown("");
    } finally {
      setRestoringHearts(false);
    }
  }, [guestPrompt, isGuest, showInfo, user.hearts, user.heartsMax]);

  const handleNavClick = useCallback((key) => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    setActiveNav(key);
    setBodyView(key === "learning" ? "learning" : key);
    setOpenDropdown("");
    setShowFullCalendar(false);
  }, [guestPrompt, isGuest]);

  const handleCourseSelect = useCallback(async (item) => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    if (item?.isLocked) {
      showInfo("Курс закритий", "Цей курс відкриється після повного проходження попереднього курсу.");
      return;
    }

    setLoading(true);

    try {
      setCourse(item || null);
      setSelectedTopic(null);
      setSelectedLesson(null);
      setOpenDropdown("");

      if (item?.id) {
        const pathRes = await learningService.getMyCoursePath(item.id);

        if (pathRes.ok) {
          setPath(normalizeCoursePath(pathRes.data));
        } else {
          setPath(null);
        }
      } else {
        setPath(null);
      }

      setBodyView("learning");
    } finally {
      setLoading(false);
    }
  }, [guestPrompt, isGuest, showInfo]);

  const loadScenesOverview = useCallback(async () => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    const sourceCourses = [...courseItemsForView].sort((a, b) => Number(a?.order || 0) - Number(b?.order || 0));

    setLoading(true);

    try {
      const responses = await Promise.all(sourceCourses.map((item) => scenesService.getForMe(item?.id)));
      const nextScenes = [];

      sourceCourses.forEach((item, courseIndex) => {
        const res = responses[courseIndex];
        const courseScenes = Array.isArray(res?.data) ? res.data : [];

        courseScenes
          .slice()
          .sort((first, second) => Number(first?.order || 0) - Number(second?.order || 0))
          .forEach((scene, sceneIndex) => {
            nextScenes.push({
              ...scene,
              key: `${item?.id || courseIndex}-${scene?.id || sceneIndex}`,
              courseId: item?.id || null,
              courseLevel: getCourseLevel(item),
              courseOrder: Number(item?.order || courseIndex + 1),
              topicOrder: sceneIndex + 1,
              previewUrl: resolveMediaUrl(scene?.backgroundUrl || scene?.BackgroundUrl),
            });
          });
      });

      setScenesOverview(nextScenes);
      setSelectedTopic(null);
      setBodyView("scenes");
      setOpenDropdown("");
    } finally {
      setLoading(false);
    }
  }, [courseItemsForView, guestPrompt, isGuest]);

  const handleTitleClick = useCallback((topic, disabled = false, index = 0) => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    if (disabled) {
      setModal({
        open: true,
        title: "",
        message: getTopicLockedMessage(topic, index),
        primaryText: "",
        secondaryText: "",
        onPrimary: null,
        onSecondary: null,
        variant: "sceneLocked",
        illustrationSrc: MascotModal,
      });
      return;
    }

    setSelectedTopic(topic || null);
    setBodyView("courses");
    setOpenDropdown("");
  }, [guestPrompt, isGuest]);

  const handleSceneButtonClick = useCallback((topic, disabled = false, index = 0) => {
    if (disabled) {
      setModal({
        open: true,
        title: "",
        message: getTopicLockedMessage(topic, index),
        primaryText: "",
        secondaryText: "",
        onPrimary: null,
        onSecondary: null,
        variant: "sceneLocked",
        illustrationSrc: MascotModal,
      });
      return;
    }

    loadScenesOverview();
  }, [loadScenesOverview]);

  const handleSceneSunClick = useCallback((topic, scene, disabled) => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    if (disabled) {
      setModal({
        open: true,
        title: "",
        message: getSceneLockedMessage(scene),
        primaryText: "",
        secondaryText: "",
        onPrimary: null,
        onSecondary: null,
        variant: "sceneLocked",
        illustrationSrc: MascotModal,
      });
      return;
    }

    showInfo("Сцена буде наступною", "Контент сцени підв’яжемо на наступному кроці. Зараз зберіг її як готову точку входу.");
  }, [guestPrompt, isGuest, showInfo]);

  const handleLessonClick = useCallback((topic, lesson, disabled) => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    if (disabled) {
      showInfo("Урок закритий", "Щоб відкрити цей урок, потрібно повністю пройти попередній урок із 9 вправами.");
      return;
    }

    setSelectedTopic(topic || null);
    setSelectedLesson(lesson || null);
    setBodyView("lesson");
  }, [guestPrompt, isGuest, showInfo]);

  const handleTrackMouseDown = useCallback((event) => {
    const track = trackRef.current;
    if (!track) return;

    dragRef.current = {
      active: true,
      startX: event.clientX,
      startLeft: track.scrollLeft,
    };

    track.classList.add(styles.dragging);
  }, []);

  const handleTrackMouseMove = useCallback((event) => {
    const track = trackRef.current;
    if (!track || !dragRef.current.active) return;

    const dx = event.clientX - dragRef.current.startX;
    track.scrollLeft = dragRef.current.startLeft - dx;
  }, []);

  const handleTrackMouseUp = useCallback(() => {
    const track = trackRef.current;
    dragRef.current.active = false;
    if (track) {
      track.classList.remove(styles.dragging);
    }
  }, []);

  const handleOpenFullCalendar = useCallback(async () => {
    if (isGuest) {
      guestPrompt();
      return;
    }

    setShowFullCalendar(true);
    setOpenDropdown("");

    const res = await streakService.getMyCalendarMonth(calendarMonth.year, calendarMonth.month);

    if (res.ok) {
      setCalendarDates(extractCalendarDates(res.data));
    }
  }, [calendarMonth.month, calendarMonth.year, guestPrompt, isGuest]);

  const handleChangeCalendarMonth = useCallback(async (direction) => {
    const current = new Date(calendarMonth.year, calendarMonth.month - 1, 1);
    current.setMonth(current.getMonth() + direction);
    const next = { year: current.getFullYear(), month: current.getMonth() + 1 };
    setCalendarMonth(next);

    if (isGuest) {
      return;
    }

    const res = await streakService.getMyCalendarMonth(next.year, next.month);

    if (res.ok) {
      setCalendarDates(extractCalendarDates(res.data));
    }
  }, [calendarMonth.month, calendarMonth.year, isGuest]);

  const renderLearningView = () => (
    <div
      ref={trackRef}
      className={styles.learningTrackViewport}
      onMouseDown={handleTrackMouseDown}
      onMouseMove={handleTrackMouseMove}
      onMouseUp={handleTrackMouseUp}
      onMouseLeave={handleTrackMouseUp}
    >
      <div className={styles.learningTrack}>
        {topics.map((item, index) => (
          <OrbitSection
            key={item.id || index}
            item={item}
            index={index}
            course={course}
            isGuest={isGuest}
            onTitleClick={handleTitleClick}
            onSceneButtonClick={handleSceneButtonClick}
            onSceneSunClick={handleSceneSunClick}
            onLessonClick={handleLessonClick}
          />
        ))}
      </div>
    </div>
  );

  const renderCoursesView = () => (
    <div className={styles.coursesPage}>
      <button type="button" className={styles.coursesBackButton} onClick={() => setBodyView("learning")}>
        <img src={ArrowPrevious} alt="Назад" />
      </button>

      <div className={styles.coursesDivider} />

      <div className={styles.coursesListWrap}>
        <div className={styles.coursesList}>
          {courseItemsForView.map((item, index) => {
            const isSelectedCourse = Number(item?.id || 0) === Number(course?.id || 0);
            const isUnlocked = isGuest ? index === 0 : !item?.isLocked;
            const progressPercent = getCourseProgressPercent(item, path, isSelectedCourse);

            return (
              <button
                key={item?.id || `${getCourseLevel(item)}-${index}`}
                type="button"
                className={styles.courseStageCard}
                onClick={() => handleCourseSelect(item)}
              >
                <div className={styles.courseStageLevel}>{getCourseLevel(item)}</div>

                <div className={styles.courseStageTrack}>
                  <div className={styles.courseStageTrackBase} />
                  <div className={styles.courseStageTrackFill} style={{ width: `${progressPercent}%` }} />
                  {!isUnlocked ? <div className={styles.courseStageTrackCutout} /> : null}
                </div>

                <img
                  className={`${styles.courseStageIcon} ${isUnlocked ? styles.courseStageIconProgress : styles.courseStageIconLock}`}
                  src={isUnlocked ? ProgressTrackIcon : HOME_SHARED_ASSETS.lock}
                  alt=""
                  aria-hidden="true"
                />
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );

  const renderLanguagesView = () => (
    <div className={styles.languagePage}>
      <button type="button" className={styles.languageBackButton} onClick={() => setBodyView("learning")}>
        <img src={ArrowPrevious} alt="Назад" />
      </button>

      <div className={styles.languagePageHeader}>
        <h1 className={styles.languagePageTitle}>Вибір мови для вивчення</h1>
        <div className={styles.languagePageDivider} />
      </div>

      <div className={styles.languagePageGrid}>
        {availableLanguageItems.map((item) => {
          const code = normalizeCode(item.code);
          const isActive = code === activeLanguageCode;

          return (
            <button
              key={code}
              type="button"
              className={`${styles.languagePageCard} ${isActive ? styles.languagePageCardActive : ""}`}
              onClick={() => handleLanguageCardClick(code)}
            >
              {isActive ? (
                <span className={styles.languagePageCheckBadge} aria-hidden="true">
                  <span className={styles.languagePageCheckBadgeInner}>
                    <svg viewBox="0 0 20 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M2.5 8.5L7.4 13L17.5 3" stroke="#FFFFFF" strokeWidth="3.2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </span>
                </span>
              ) : null}

              <img className={styles.languagePageFlag} src={getFlagByCode(code)} alt="" aria-hidden="true" />
              <span className={styles.languagePageCardLabel}>{getLanguageLabel(code)}</span>
            </button>
          );
        })}
      </div>
    </div>
  );

  const renderScenesView = () => (
    <div className={styles.scenesPage}>
      <button type="button" className={styles.scenesBackButton} onClick={() => setBodyView("learning")}>
        <img src={ArrowPrevious} alt="Назад" />
      </button>

      <div className={styles.scenesPageHeader}>
        <h1 className={styles.scenesPageTitle}>Сцени</h1>
        <div className={styles.scenesPageDivider} />
      </div>

      <div className={styles.scenesGridWrap}>
        <div className={styles.scenesGrid}>
          {scenesOverview.map((scene, index) => {
            const locked = !scene?.isUnlocked;
            const previewStyle = !locked && scene?.previewUrl
              ? { backgroundImage: `url(${scene.previewUrl})` }
              : undefined;

            return (
              <button
                key={scene.key || scene.id || index}
                type="button"
                className={`${styles.scenesCard} ${locked ? styles.scenesCardLocked : styles.scenesCardOpen}`}
                onClick={() => {
                  if (locked) {
                    setModal({
                      open: true,
                      title: "",
                      message: getSceneLockedMessage(scene),
                      primaryText: "",
                      secondaryText: "",
                      onPrimary: null,
                      onSecondary: null,
                      variant: "sceneLocked",
                      illustrationSrc: MascotModal,
                    });
                    return;
                  }

                  showInfo("Сцена відкрита", "Цю сцену вже можна проходити або перепроходити. Окрему сторінку сцени підв’яжемо наступним кроком, не ламаючи цю логіку.");
                }}
              >
                <span
                  className={`${styles.scenesCardPreview} ${locked ? styles.scenesCardPreviewLocked : styles.scenesCardPreviewOpen}`}
                  style={previewStyle}
                />
                <span className={styles.scenesCardLabel}>{getSceneCardLabel(scene, index)}</span>
              </button>
            );
          })}
        </div>
      </div>

      <div className={styles.scenesBottomDivider} />
    </div>
  );

  const renderLessonView = () => (
    <FullPanel
      title={selectedLesson?.title || "Урок"}
      subtitle="Тут поки що стоїть заглушка під окрему сторінку уроку з 3 типами вправ та хрестиком зверху зліва."
      onClose={() => setBodyView("learning")}
    >
      <div className={styles.lessonStubGrid}>
        <div className={styles.lessonStubCard}>Вправа 1 — словникова</div>
        <div className={styles.lessonStubCard}>Вправа 2 — вибір правильної відповіді</div>
        <div className={styles.lessonStubCard}>Вправа 3 — складання речення</div>
      </div>
      <div className={styles.lessonStubHint}>
        У кожному уроці буде 9 вправ: по 3 різні типи, які повторюються всередині обраного уроку.
      </div>
    </FullPanel>
  );

  const renderStubView = (title, subtitle) => (
    <FullPanel title={title} subtitle={subtitle} onClose={() => { setActiveNav("learning"); setBodyView("learning"); }}>
      <div className={styles.stubText}>Тут підключимо цей розділ на наступних етапах, не ламаючи готову Home-сторінку.</div>
    </FullPanel>
  );

  const renderBody = () => {
    if (bodyView === "achievements") {
      return renderStubView("Нагороди", "Тут буде сторінка з усіма нагородами та досягненнями користувача.");
    }

    if (bodyView === "dictionary") {
      return renderStubView("Словник", "Тут буде персональний словник користувача з вивченими словами.");
    }

    if (bodyView === "profile") {
      return renderStubView("Профіль", "Тут буде сторінка профілю користувача з аватаром та статистикою.");
    }

    if (bodyView === "languages") {
      return renderLanguagesView();
    }

    if (bodyView === "courses") {
      return renderCoursesView();
    }

    if (bodyView === "scenes") {
      return renderScenesView();
    }

    if (bodyView === "lesson") {
      return renderLessonView();
    }

    return renderLearningView();
  };

  return (
    <div className={styles.viewport}>
      <GlassLoading open={loading || restoringHearts} text={restoringHearts ? "Відновлюємо енергію..." : "Завантажуємо Home..."} />
      <GlassModal
        open={modal.open}
        title={modal.title}
        message={modal.message}
        onClose={closeModal}
        primaryText={modal.primaryText}
        secondaryText={modal.secondaryText}
        onPrimary={modal.onPrimary}
        onSecondary={modal.onSecondary}
        variant={modal.variant || (bodyView === "languages" ? "languageWarning" : "default")}
        illustrationSrc={modal.illustrationSrc}
      />

      <div ref={stageRef} className={styles.stage}>
        <img className={styles.bgLeft} src={HOME_BG_LEFT} alt="" aria-hidden="true" />

        <aside className={styles.sidebar}>
          <div className={styles.logo}>LUMINO</div>

          <nav className={styles.nav}>
            {NAV_ITEMS.map((item) => {
              const active = activeNav === item.key;

              return (
                <button
                  key={item.key}
                  type="button"
                  className={`${styles.navButton} ${active ? styles.navButtonActive : ""}`}
                  onClick={() => handleNavClick(item.key)}
                >
                  <NavIcon type={item.key} />
                  <span>{item.label}</span>
                </button>
              );
            })}
          </nav>
        </aside>

        <div className={styles.sidebarDivider} />

        {!isTopBarHidden ? (
          <div ref={dropdownRef} className={styles.topBar}>
            <button
              type="button"
              className={styles.flagButton}
              style={{ left: `${HEADER_COUNTERS.flag.x}px`, top: `${HEADER_COUNTERS.flag.y}px`, width: `${HEADER_COUNTERS.flag.w}px`, height: `${HEADER_COUNTERS.flag.h}px` }}
              onClick={() => {
                if (isGuest) {
                  guestPrompt();
                  return;
                }

                setOpenDropdown((prev) => (prev === "language" ? "" : "language"));
              }}
            >
              <img className={styles.flagIcon} src={getFlagByCode(activeLanguageCode)} alt="" aria-hidden="true" />
            </button>

            <button
              type="button"
              className={styles.counterButton}
              style={{ left: `${HEADER_COUNTERS.star.x}px`, top: `${HEADER_COUNTERS.star.y}px`, width: `${HEADER_COUNTERS.star.w}px`, height: `${HEADER_COUNTERS.star.h}px` }}
              onClick={() => {
                if (isGuest) {
                  guestPrompt();
                  return;
                }

                setOpenDropdown((prev) => (prev === "streak" ? "" : "streak"));
              }}
            >
              <HeaderIcon src={HOME_HEADER_ICONS.streak} />
              <span>{Number(user.currentStreakDays || 0)}</span>
            </button>

            <div
              className={styles.counterStatic}
              style={{ left: `${HEADER_COUNTERS.crystal.x}px`, top: `${HEADER_COUNTERS.crystal.y}px`, width: `${HEADER_COUNTERS.crystal.w}px`, height: `${HEADER_COUNTERS.crystal.h}px` }}
            >
              <HeaderIcon src={HOME_HEADER_ICONS.crystal} />
              <span>{Number(user.crystals || 0)}</span>
            </div>

            <button
              type="button"
              className={styles.counterButton}
              style={{ left: `${HEADER_COUNTERS.energy.x}px`, top: `${HEADER_COUNTERS.energy.y}px`, width: `${HEADER_COUNTERS.energy.w}px`, height: `${HEADER_COUNTERS.energy.h}px` }}
              onClick={() => {
                if (isGuest) {
                  guestPrompt();
                  return;
                }

                setOpenDropdown((prev) => (prev === "energy" ? "" : "energy"));
              }}
            >
              <img className={`${styles.headerSvg} ${styles.headerEnergySvg}`} src={HOME_HEADER_ICONS.energy} alt="" aria-hidden="true" />
              <span>{Number(user.hearts || 0)}</span>
            </button>

            {openDropdown === "language" ? (
              <div className={`${styles.dropdownCard} ${styles.languageDropdownCard}`} style={{ left: "1102px", top: "136px", width: "402px", minHeight: "180px" }}>
                <div className={styles.languageDropdownTitle}>КУРСИ</div>
                <div className={styles.languageDropdownDivider} />
                <div className={styles.languageDropdownActions}>
                  {languageItems.slice(0, 1).map((item) => (
                    <button key={item.code} type="button" className={styles.languageDropdownAction} onClick={() => handleLanguageSwitch(item.code)}>
                      <div className={styles.languageDropdownFlagWrap}>
                        <img src={getFlagByCode(item.code)} alt="" aria-hidden="true" />
                      </div>
                      <span>{getLanguageLabel(item.code)}</span>
                    </button>
                  ))}
                  <button
                    type="button"
                    className={styles.languageDropdownAction}
                    onClick={() => {
                      if (isGuest) {
                        guestPrompt();
                        return;
                      }

                      setBodyView("languages");
                      setOpenDropdown("");
                    }}
                  >
                    <div className={`${styles.languageDropdownFlagWrap} ${styles.languageDropdownPlusWrap}`}>+</div>
                    <span>Мова</span>
                  </button>
                </div>
              </div>
            ) : null}

            {openDropdown === "streak" ? (
              <div className={`${styles.dropdownCard} ${styles.streakDropdownCard}`} style={{ left: "1240px", top: "125px", width: "402px", minHeight: "390px" }}>
                <div className={styles.streakDropdownTop}>
                  <div className={styles.streakHeroContent}>
                    <div className={styles.streakHeroDays}>{Number(user.currentStreakDays || 0)}</div>
                    <div className={styles.streakHeroLabel}>-денний відрізок!</div>
                  </div>
                  <img className={styles.streakHeroIcon} src={HOME_HEADER_ICONS.streak} alt="" aria-hidden="true" />
                </div>
                <div className={styles.streakDropdownBottom}>
                  <div className={styles.streakCalendarCard}>
                    <div className={styles.streakProgressDays}>
                      {WEEK_DAYS.map((item) => (
                        <span key={item}>{item}</span>
                      ))}
                    </div>
                    <div className={styles.streakProgressBar}>
                      <div
                        className={styles.streakProgressFill}
                        style={{ width: `${Math.max((streakProgress.filled / 7) * 100, streakProgress.filled > 0 ? 18 : 0)}%` }}
                      >
                        {streakProgress.filled > 0 ? (
                          <img
                            className={styles.streakProgressStar}
                            src={HOME_HEADER_ICONS.streak}
                            alt=""
                            aria-hidden="true"
                            style={{ left: "calc(100% - 26px)" }}
                          />
                        ) : null}
                      </div>
                    </div>
                  </div>
                  <button type="button" className={`${styles.dropdownPrimaryLink} ${styles.streakPrimaryLink}`} onClick={handleOpenFullCalendar}>
                    ДОКЛАДНІШЕ
                  </button>
                </div>
              </div>
            ) : null}

            {openDropdown === "energy" ? (
              <div className={`${styles.dropdownCard} ${styles.energyDropdownCard}`} style={{ left: "1518px", top: "135px", width: "402px", minHeight: "293px" }}>
                <div className={styles.energyDropdownTitle}>Енергія</div>
                <div className={styles.energyChargeLabel}>ЗАРЯДЖЕННЯ</div>
                <div className={styles.energyScale}>
                  <div className={styles.energyScaleTrack} />
                  <div className={styles.energyScaleFill} style={{ width: `${(Number(user.hearts || 0) / energySteps) * 100}%` }}>
                    <span className={styles.energyScaleValue}>
                      {Number(user.hearts || 0)}/{energySteps}
                    </span>
                  </div>
                  <span className={styles.energyScaleIconWrap} aria-hidden="true" style={{ left: `calc(${Math.max(0, Math.min(100, (Number(user.hearts || 0) / energySteps) * 100))}% - 18px)` }}>
                    <img className={styles.energyScaleIcon} src={HOME_HEADER_ICONS.energyScale} alt="" />
                  </span>
                </div>
                <button type="button" className={styles.energyAction} onClick={handleFullRestoreHearts}>
                  <div className={styles.energyActionLeft}>
                    <img className={`${styles.energyActionSun} ${styles.energyActionSunPrimary}`} src={HOME_HEADER_ICONS.energy} alt="" aria-hidden="true" />
                    <span className={styles.energyActionText}>Відновити енергію</span>
                  </div>
                  <div className={styles.energyActionRight}>
                    <img className={styles.energyActionCrystal} src={HOME_HEADER_ICONS.crystal} alt="" aria-hidden="true" />
                    <span>{energySteps * 100}</span>
                  </div>
                </button>
                <button type="button" className={`${styles.energyAction} ${styles.energyActionSecondary}`} onClick={handleRestoreOneHeart}>
                  <div className={styles.energyActionLeft}>
                    <span className={styles.energyActionSunBadge} aria-hidden="true">
                      <img className={styles.energyActionSun} src={HOME_SHARED_ASSETS.energySmall} alt="" />
                      <span className={styles.energyActionSunBadgeValue}>1</span>
                    </span>
                    <span className={styles.energyActionText}>+1 одиниця енергії</span>
                  </div>
                  <div className={styles.energyActionRight}>
                    <img className={styles.energyActionCrystal} src={HOME_HEADER_ICONS.crystal} alt="" aria-hidden="true" />
                    <span>100</span>
                  </div>
                </button>
              </div>
            ) : null}
          </div>
        ) : null}

        {showFullCalendar ? (
          <div className={styles.calendarOverlay}>
            <div className={styles.calendarModal}>
              <div className={styles.calendarTop}>
                <button type="button" className={styles.calendarClose} onClick={() => setShowFullCalendar(false)}>
                  ×
                </button>
                <div className={styles.calendarTopTitle}>Відрізок</div>
              </div>
              <div className={styles.calendarTopDivider} />
              <div className={styles.calendarHero}>
                <div className={styles.calendarHeroCard}>
                  <div className={styles.calendarHeroDays}>{Number(user.currentStreakDays || 0)}</div>
                  <div className={styles.calendarHeroLabel}>-денний відрізок!</div>
                </div>
                <img className={styles.calendarHeroIcon} src={HOME_HEADER_ICONS.streak} alt="" aria-hidden="true" />
              </div>
              <div className={styles.calendarSectionTitle}>Календар</div>
              <div className={styles.calendarMonthCard}>
                <div className={styles.calendarHeader}>
                  <span>{new Date(calendarMonth.year, calendarMonth.month - 1).toLocaleString("uk-UA", { month: "long", year: "numeric" })}</span>
                  <div className={styles.calendarHeaderActions}>
                    <button type="button" onClick={() => handleChangeCalendarMonth(-1)}>‹</button>
                    <button type="button" onClick={() => handleChangeCalendarMonth(1)}>›</button>
                  </div>
                </div>
                <div className={styles.calendarGridCard}>
                  <div className={styles.calendarWeekdays}>
                    {WEEK_DAYS.map((item) => (
                      <span key={item}>{item}</span>
                    ))}
                  </div>
                  <div className={styles.calendarGrid}>
                    {monthCells.map((cell) => (
                      <span
                        key={cell.key}
                        className={`${styles.calendarCell} ${cell.active ? styles.calendarCellActive : ""} ${cell.muted ? styles.calendarCellMuted : ""}`}
                      >
                        {cell.active ? <img className={styles.calendarCellStar} src={HOME_HEADER_ICONS.streak} alt="" aria-hidden="true" /> : null}
                        <span className={styles.calendarCellText}>{cell.day}</span>
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        ) : null}

        <main className={`${styles.body} ${isFullPanelView ? styles.bodyFull : ""}`}>{renderBody()}</main>
      </div>
    </div>
  );
}
