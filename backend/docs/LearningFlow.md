# Lumino Backend — Learning Flow (реальний шлях навчання користувача)

> Цей документ **переписаний під поточний бекенд з `backend.zip`** (Lumino.API).  
> Ціль: описати **саме те, що реально зроблено в коді** (контролери → сервіси → БД), без вигадок.

---

## 0) Модель системи (що саме “живе” в бекенді)

### 0.1. Навчальна структура
- **Course** → курс (мовний напрямок), може бути `IsPublished`.
- **Topic** → тема всередині курсу (має `Order`).
- **Lesson** → урок всередині теми (має `Order`).
- **Exercise** → завдання в уроці (має `Order`, `Type`, `Question`, `Data`, `CorrectAnswer`).

### 0.2. Те, що тримає прогрес користувача
- **UserCourse** → зв’язок користувача з курсом:
  - який курс активний (`IsActive`), коли стартував,
  - `LastLessonId` (урок, який бекенд вважає “поточним”).
- **UserLessonProgress** → стан уроку в користувача:
  - `IsUnlocked` / `IsCompleted` / `BestScore` / `LastAttemptAt`.
- **LessonResult** → **кожна спроба** здати урок:
  - `Score`, `TotalQuestions`, `CompletedAt`, `MistakesJson`, `IdempotencyKey`.

### 0.3. Сцени (story/діалоги)
- **Scene** → сцена з порядком `Order`, може мати `CourseId` і/або `TopicId`.
- **SceneStep** → крок сцени (репліка або питання/квіз).
- **SceneAttempt** → прогрес користувача по сцені:
  - `IsCompleted`, `Score`, `TotalQuestions`, `DetailsJson`, `CompletedAt`, `IdempotencyKey`.

### 0.4. Vocabulary (SRS)
- **VocabularyItem** → словникова одиниця (word/translation/приклад тощо).
- **UserVocabulary** → запис у словнику користувача:
  - `NextReviewAt`, `ReviewCount`, `AddedAt` (SRS-логіка).

### 0.5. Досягнення / streak / профіль
- **Achievement / UserAchievement** → нагороди.
- **Streak** → серія днів з активністю (оновлюється після **passed** уроків).
- **UserProgress** (у відповіді API) → агрегований стан (XP/ціль/пройдено тощо) формується сервісом.

### 0.6. “Економіка” як у Duolingo (hearts + crystals)
- У **User** зберігаються:
  - `Hearts`, `HeartsUpdatedAtUtc`, `Crystals`.
- **Hearts** списуються за помилки у вправах уроку.
- **Crystals** видаються як нагорода за **перше** passed уроку / completed сцени.
- Є відновлення hearts по таймеру + платне відновлення за crystals.

---

## 1) Налаштування, які впливають на навчання (appsettings → секція `Learning`)

Фактичні поля `LearningSettings` (див. `Utils/LearningSettings.cs`):

### 1.1. Правила проходження
- `PassingScorePercent` (дефолт **80**) — поріг “урок пройдено”.
- `ScenePassingPercent` (дефолт **100**) — поріг “сцена завершена”.
- `SceneUnlockEveryLessons` (дефолт **1**) — як часто відкривати **legacy-сцени**.

### 1.2. Daily goal
- `DailyGoalScoreTarget` (дефолт **20**) — ціль на день.

### 1.3. Vocabulary SRS
- `VocabularyWrongDelayHours` (дефолт **12**) — відкладення після помилки.
- `VocabularySkipDelayMinutes` (дефолт **10**) — відкладення для “skip”.
- `VocabularyReviewIntervalsDays` (дефолт `[1,2,4,7,14,30,60]`) — інтервали після правильних.

### 1.4. Hearts / crystals
- `HeartsMax` (дефолт **5**)
- `HeartsCostPerMistake` (дефолт **1**)
- `CrystalCostPerHeart` (дефолт **10**)
- `HeartRegenMinutes` (дефолт **30**)
- `CrystalsRewardPerPassedLesson` (дефолт **1**)
- `CrystalsRewardPerCompletedScene` (дефолт **1**)

> У сервісах використовується нормалізація значень (наприклад, passing% не може бути 0 або >100; unlockEvery не може бути <=0).

---

## 2) Авторизація та акаунт (повний список)

База: JWT Bearer (`Authorization: Bearer <token>`).

### 2.1. Реєстрація / логін / refresh / logout
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout` → `204 No Content`

### 2.2. Відновлення доступу та верифікація email
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password` → `204 No Content`
- `POST /api/auth/verify-email`
- `POST /api/auth/resend-verification`

> Для verify/resend/forgot бекенд бере `ip` та `User-Agent` з HTTP-запиту.

### 2.3. OAuth логін
- `POST /api/auth/oauth/google`

### 2.4. Профіль користувача (Authorize)
- `GET /api/user/me` — повертає поточного користувача (перед відповіддю викликається `RefreshHearts`).
- `PUT /api/user/profile` — оновити профіль.
- `POST /api/user/change-password` → `204`
- `POST /api/user/delete-account` → `204`

### 2.5. Прив’язка/відв’язка соц. логінів (Authorize)
- `GET /api/user/external-logins`
- `POST /api/user/external-logins/unlink` → `204`
- `POST /api/user/external-logins/link/google` → `204`

### 2.6. Відновлення hearts за crystals (Authorize)
- `POST /api/user/restore-hearts`
  - якщо `HeartsToRestore == 0` → повертає “інфо-статус” (скільки сердець/ціна/таймер) без витрат.

---

## 3) Onboarding (мови користувача)

- `GET /api/onboarding/languages` *(AllowAnonymous)* — список підтримуваних мов.
- `GET /api/onboarding/languages/{languageCode}/availability` *(AllowAnonymous)* — доступність мови.
- `GET /api/onboarding/languages/me` *(Authorize)* — вибрані мови користувача.
- `PUT /api/onboarding/languages/me` *(Authorize)* — оновити список мов користувача.
- `PUT /api/onboarding/target-language/me` *(Authorize)* — оновити target language.

---

## 4) Каталог курсів і старт навчання

### 4.1. Отримати курси
- `GET /api/courses?languageCode=...` — публічні (тільки published).
- `GET /api/courses/me?languageCode=...` *(Authorize)* — “мої” курси (в т.ч. активний/історія).

### 4.2. Отримати теми та уроки (публічні)
- `GET /api/courses/{courseId}/topics`
- `GET /api/topics/{topicId}/lessons`

> Ці endpoint’и віддають структуру, але **не перевіряють unlock**.

### 4.3. Старт курсу (ключова точка входу)
- `POST /api/learning/courses/{courseId}/start` *(Authorize)*

Що робить бекенд (логіка в `CourseProgressService.StartCourse` + пов’язані синки):
1) Перевіряє, що курс існує і доступний (публікація/структура).
2) Знімає `IsActive` з попереднього активного курсу користувача (якщо був).
3) Створює або оновлює `UserCourse` для цього курсу:
   - `IsActive = true`
   - `LastOpenedAt = now`
   - `LastLessonId` виставляється на перший доступний урок (або коригується синком).
4) Гарантує прогрес для уроків: створює `UserLessonProgress` для уроків курсу.
5) Відкриває перший урок (`IsUnlocked=true`), інші лишаються locked.

Відповідь: “активний курс” (DTO сервісу прогресу).

### 4.4. Отримати активний курс
- `GET /api/learning/courses/active` *(Authorize)*
  - `404 NotFound`, якщо активного курсу нема.

### 4.5. Прогрес уроків по курсу
- `GET /api/learning/courses/{courseId}/lessons/progress` *(Authorize)*

### 4.6. Completion курсу (по уроках)
- `GET /api/learning/courses/{courseId}/completion/me` *(Authorize)*

---

## 5) Карта навчання для UI (Learning Path)

### 5.1. Отримати “path” курсу для себе
- `GET /api/learning/courses/{courseId}/path/me` *(Authorize)*

Відповідь: `LearningPathResponse`, де є:
- `Topics[]` → `Lessons[]` (unlocked/completed/bestScore тощо)
- `Scenes[]` (unlocked/completed + причина/статистика)
- `NextPointers` (бекенд підказує, що наступне: lessonId/sceneId)

### 5.2. Важливо про sync
Бекенд перед формуванням path **підтягує реальний стан** на основі фактів у БД:
- passed уроки визначаються по `LessonResults` (поріг `PassingScorePercent`).
- `UserLessonProgress.IsCompleted` може “доганятися” від результатів.
- unlock наступного уроку виконується, коли попередній став completed.

Це означає:
- якщо щось змінилось у `LessonResults`, то наступні виклики path/next приведуть стан до консистентного.

---

## 6) Next Activity (що робити далі)

Є два способи:
- `GET /api/next/me` *(Authorize)*
- `GET /api/learning/next` *(Authorize)* — **alias** до того ж самого.

Також є preview:
- `GET /api/next/me/preview` *(Authorize)* → повертає `{ next, progress, generatedAt }`.

### 6.1. Пріоритет вибору (реально в `NextActivityService`)
1) **VocabularyReview** — якщо є `UserVocabulary.NextReviewAt <= now`.
2) **Lesson** — наступний урок активного курсу (або першого published курсу, якщо активного нема).
3) **Scene** — наступна відкрита незавершена сцена.
4) Якщо нічого з (1–3) не знайдено, але курс існує → **CourseComplete**.

### 6.2. Як визначається “наступний урок”
- Бекенд формує уроки курсу в правильному порядку:
  - `Topic.Order` (<=0 переноситься в кінець), потім `Topic.Id`,
  - `Lesson.Order` (<=0 переноситься в кінець), потім `Lesson.Id`.
- passed уроки: будь-який `LessonResult` з `TotalQuestions > 0` і `Score% >= PassingScorePercent`.
- перед вибором робиться `EnsureUserLessonProgressForCourse(...)`:
  - створює прогрес, ставить `IsUnlocked` (послідовно), оновлює `IsCompleted`, оновлює `BestScore`.
- вибір:
  1) якщо в активному курсі є `LastLessonId`, і він **ще не passed** та **unlocked** → повертається він.
  2) інакше — перший unlocked урок, який ще не passed.

### 6.3. Як визначається “наступна сцена”
- Сцени беруться:
  - якщо є сцени з `CourseId == activeCourseId` → беруться **тільки вони**,
  - інакше працює legacy-режим: беруться сцени з `CourseId == null`.
- Порядок: `Scene.Order` (<=0 в кінець), потім `Scene.Id`.
- Completed визначається по `SceneAttempts.IsCompleted`.

**Unlock правила:**
- Якщо у сцени задано `TopicId`:
  - сцена unlocked тоді, коли `passedLessonsInTopic >= totalLessonsInTopic`.
- Якщо `TopicId == null` (legacy):
  - `requiredLessons = (scenePosition - 1) * SceneUnlockEveryLessons`,
  - unlocked, якщо `passedLessonsInCourse >= requiredLessons`.

---

## 7) Уроки — відкриття, вправи, submit, mistakes

### 7.1. Отримати урок (з перевіркою lock)
- `GET /api/lessons/{id}` *(Authorize)*

Це **саме той endpoint**, який треба викликати перед початком уроку на фронті: він не дасть відкрити locked.

### 7.2. Отримати вправи уроку
- `GET /api/lessons/{lessonId}/exercises` *(Authorize)*

Повертає вправи у порядку `Order` → `Id`.

#### 7.2.1. Типи вправ (що реально перевіряється в бекенді)
- `MultipleChoice`
  - правильність визначається порівнянням `CorrectAnswer` з відповіддю користувача (нормалізація рядка).
- `Input`
  - підтримує кілька правильних варіантів (парсер `CorrectAnswer` розбиває альтернативи), є нормалізація.
- `Match`
  - правильність визначається порівнянням “мапи” відповідностей (використовується `exercise.Data` як еталон).

> Формат `Data` для Match — JSON з парами `left/right`, а відповідь користувача очікується в тому ж форматі.

### 7.3. Submit уроку
Є 2 еквівалентні маршрути:
- `POST /api/lesson-submit` *(Authorize)* — основний
- `POST /api/lessons/{id}/submit` *(Authorize)* — alias (пріоритет має id з route)

Тіло: `SubmitLessonRequest`
- `LessonId` *(для alias виставляється автоматично)*
- `IdempotencyKey` *(опційно)*
- `Answers[]`:
  - `ExerciseId`
  - `Answer` (string)

Що реально робить бекенд (логіка в `LessonResultService.SubmitLesson`):
1) Валідує request (в т.ч. наявність відповідей/обмеження ключа і т.д.).
2) Перевіряє, що урок існує.
3) Перевіряє, що урок unlocked (`UserLessonProgress` існує і `IsUnlocked=true`).
4) Idempotency:
   - якщо `IdempotencyKey` вже використовувався для цього уроку користувачем → повертає відповідь, побудовану з існуючого `LessonResult`.
5) Завантажує всі вправи уроку, перевіряє кожну відповідь, рахує `correct` і збирає `mistakeExerciseIds`.
6) Обчислює `IsPassed` за правилом `PassingScorePercent`.
7) Пише новий `LessonResult` (і `MistakesJson`, де зберігаються як id помилок, так і деталізація по відповідях).
8) Економіка:
   - списує hearts за кількість помилок (`ConsumeHeartsForMistakes`).
9) Прогрес:
   - оновлює агрегований прогрес користувача;
   - якщо урок пройдено **вперше** (перший passed) → нараховує crystals (за налаштуваннями).
10) Vocabulary:
   - після passed бекенд може додавати слова у словник (логіка автододавання).
11) Course progress:
   - оновлює `UserLessonProgress` (completed/bestScore), unlock наступного уроку, коригує `UserCourse.LastLessonId`.
12) Streak:
   - при passed викликається `RegisterLessonActivity`.
13) Achievements:
   - перевірка та видача досягнень.

Відповідь: `SubmitLessonResponse`:
- `TotalExercises`, `CorrectAnswers`, `IsPassed`
- `EarnedCrystals`
- `MistakeExerciseIds`
- `Answers[]` (з correctAnswer, щоб фронт міг показати фідбек)

### 7.4. Повторення помилок уроку (Duolingo “Repeat mistakes”)
- `GET /api/lessons/{id}/mistakes` *(Authorize)*
  - повертає помилки **останньої** спроби з деталями.
- `POST /api/lessons/{id}/mistakes/submit` *(Authorize)*
  - приймає `SubmitLessonMistakesRequest` і перераховує correctness тільки для “mistake-вправ”.

Важлива поведінка:
- після submit mistakes урок може стати passed, але unlock наступного уроку найнадійніше ловити через **sync** (наприклад, наступним `GET /api/next/me` або `GET /api/learning/.../path/me`).

### 7.5. Історія результатів уроків
- `GET /api/results/me` *(Authorize)* — список спроб.
- `GET /api/results/me/{resultId}` *(Authorize)* — деталі конкретної спроби (включно з відповідями і правильними відповідями).

---

## 8) Сцени — деталі, контент, submit, mistakes

### 8.1. Список/деталі сцен
- `GET /api/scenes` — всі сцени (без auth).
- `GET /api/scenes/me?courseId=...` *(Authorize)* — сцени “для мене” з locked/completed.
- `GET /api/scenes/completed` *(Authorize)* — завершені сцени.
- `GET /api/scenes/{id}` *(Authorize)* — деталі сцени (включає locked/completed у контексті користувача).

### 8.2. Контент сцени (кроки)
- `GET /api/scenes/{id}/content` *(Authorize)*

Бекенд віддає кроки сцени (репліки + питання). Якщо сцена locked — цей endpoint має відпрацювати відповідно до правил доступу сервісу.

### 8.3. Submit сцени
- `POST /api/scenes/{id}/submit` *(Authorize)*

Тіло: `SubmitSceneRequest`:
- `IdempotencyKey` *(опційно)*
- `Answers[]`: `{ stepId, answer }`

Логіка (в `SceneService.SubmitScene`):
- визначає питання (оцінювані кроки), рахує `CorrectAnswers/TotalQuestions`.
- completion настає, якщо `Correct% >= ScenePassingPercent`.
- зберігає/оновлює `SceneAttempt`.
- при першому completed:
  - нараховує crystals (якщо налаштовано),
  - оновлює прогрес,
  - може додавати vocabulary,
  - перевіряє achievements.

### 8.4. Scene mistakes (Repeat mistakes)
- `GET /api/scenes/{id}/mistakes` *(Authorize)*
- `POST /api/scenes/{id}/mistakes/submit` *(Authorize)*

### 8.5. Явне “позначити як completed”
- `POST /api/scenes/complete` *(Authorize)*

> Використовується, коли фронт хоче зафіксувати завершення окремим кроком (на додачу до submit).

---

## 9) Vocabulary (SRS) — як працює повторення

### 9.1. Список словника
- `GET /api/vocabulary/me` *(Authorize)*

### 9.2. Деталі словникового item
- `GET /api/vocabulary/items/{id:int}` *(Authorize)*

### 9.3. Що “due”
- `GET /api/vocabulary/due` *(Authorize)*

### 9.4. Наступне слово на повторення
- `GET /api/vocabulary/review/next` *(Authorize)*
  - `204 No Content`, якщо нічого не треба повторювати.

### 9.5. Додати слово вручну
- `POST /api/vocabulary` *(Authorize)* → `204 No Content`

### 9.6. Відповісти на повторення
- `POST /api/vocabulary/{id}/review` *(Authorize)*

`id` — це **UserVocabularyId**.

Поведінка (SRS):
- при правильній відповіді — збільшується `ReviewCount`, `NextReviewAt` ставиться по інтервалах.
- при помилці — `NextReviewAt = now + VocabularyWrongDelayHours`.
- при “skip” — коротке відкладення (`VocabularySkipDelayMinutes`).

---

## 10) Прогрес, streak, daily goal, профіль

### 10.1. Прогрес
- `GET /api/progress/me` *(Authorize)*

### 10.2. Daily goal
- `GET /api/progress/daily-goal` *(Authorize)*

> Сервіс рахує “очки за день” і порівнює з `DailyGoalScoreTarget`. Межа дня — UTC.

### 10.3. Streak
- `GET /api/streak/me` *(Authorize)*
- `GET /api/streak/calendar?days=30` *(Authorize)*
- `GET /api/streak/calendar?year=YYYY&month=MM` *(Authorize)*

### 10.4. Achievements
- `GET /api/achievements/me` *(Authorize)*

### 10.5. Weekly progress (профіль)
- `GET /api/profile/weekly-progress` *(Authorize)*

---

## 11) Коли бекенд вважає, що “все пройдено”

### 11.1. Урок пройдений (passed)
Є хоча б один `LessonResult`, де:
- `TotalQuestions > 0`
- `Score * 100 >= TotalQuestions * PassingScorePercent`

### 11.2. Сцена завершена
`SceneAttempt.IsCompleted == true`, що настає при `Correct% >= ScenePassingPercent`.

### 11.3. Курс завершено
Коли **немає**:
- due-vocabulary,
- next-lesson,
- next-scene,

…бекенд в `GET /api/next/me` повертає:
- `Type = "CourseComplete"` і `CourseId`.

> Тобто “завершення” тут визначається як “нема наступних активностей за правилами next”.

---

## 12) Admin API (адмінка контенту)

Усі admin endpoint’и мають `[Authorize(Roles = "Admin")]`.

### 12.1. Курси / теми / уроки / вправи
- Курси: `GET/POST /api/admin/courses`, `GET/PUT/DELETE /api/admin/courses/{id}`
- Теми: `GET/POST /api/admin/topics`, `GET/PUT/DELETE /api/admin/topics/{id}`
- Уроки: `GET/POST /api/admin/lessons`, `GET/PUT/DELETE /api/admin/lessons/{id}`
- Вправи: `GET/POST /api/admin/exercises`, `GET/PUT/DELETE /api/admin/exercises/{id}`

### 12.2. Scenes / Vocabulary / Achievements
- Scenes: `GET/POST /api/admin/scenes`, `GET/PUT/DELETE /api/admin/scenes/{id}`
- Vocabulary: `GET/POST /api/admin/vocabulary`, `GET/PUT/DELETE /api/admin/vocabulary/{id}`
- Achievements: `GET/POST /api/admin/achievements`, `GET/PUT/DELETE /api/admin/achievements/{id}`

### 12.3. Користувачі
- `GET /api/admin/users`
- `GET /api/admin/users/{id}`
- `PUT /api/admin/users/{id}`
- `DELETE /api/admin/users/{id}`

### 12.4. Media (upload/list)
- `POST /api/media/upload` *(multipart/form-data)*
- `GET /api/media/list?query=...&skip=0&take=100`

### 12.5. Refresh tokens cleanup
- `POST /api/admin/tokens/cleanup` → повертає `{ deleted: N }`.

---

## 13) Мінімальний “happy path” для фронтенду (як це правильно викликати)

1) Onboarding (опційно):
   - `GET /api/onboarding/languages`
2) Auth:
   - `POST /api/auth/register` або `POST /api/auth/login`
3) Курс:
   - `GET /api/courses`
   - `POST /api/learning/courses/{courseId}/start`
4) Home:
   - `GET /api/next/me/preview` (або `GET /api/next/me`)
5) Якщо `Type == Lesson`:
   - `GET /api/lessons/{lessonId}`
   - `GET /api/lessons/{lessonId}/exercises`
   - `POST /api/lessons/{lessonId}/submit` (або `POST /api/lesson-submit`)
   - (опційно) `GET /api/lessons/{lessonId}/mistakes` → `POST /api/lessons/{lessonId}/mistakes/submit`
   - `GET /api/next/me`
6) Якщо `Type == VocabularyReview`:
   - `GET /api/vocabulary/review/next`
   - `POST /api/vocabulary/{userVocabularyId}/review`
   - `GET /api/next/me`
7) Якщо `Type == Scene`:
   - `GET /api/scenes/{sceneId}` (деталі)
   - `GET /api/scenes/{sceneId}/content`
   - `POST /api/scenes/{sceneId}/submit`
   - (опційно) `GET /api/scenes/{sceneId}/mistakes` → `POST /api/scenes/{sceneId}/mistakes/submit`
   - `GET /api/next/me`
8) Якщо `Type == CourseComplete`:
   - показати “курс завершено” + запропонувати інший курс (каталог).

---

