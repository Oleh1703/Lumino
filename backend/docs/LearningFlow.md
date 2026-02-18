# Lumino Backend — Learning Flow (шлях навчання користувача)

> Документ сформований на основі поточного бекенду з `backend.zip`.  
> Мета: **чітко і покроково** описати, як працює навчальний процес — від реєстрації до завершення курсу — так, як це реально реалізовано в контролерах/сервісах/БД.

---

## 0) Базові поняття (що “бачить” бекенд)

### Основні сутності
- **Course** → містить **Topics**
- **Topic** → містить **Lessons**
- **Lesson** → містить **Exercises**
- **Exercise** → одне завдання/питання всередині уроку  
  Типи: `MultipleChoice`, `Input`, `Match`
- **UserCourse** → зв’язок користувача з курсом (активний курс + “останній урок”)
- **UserLessonProgress** → прогрес користувача по уроку: `IsUnlocked`, `IsCompleted`, `BestScore`
- **LessonResult** → одна спроба здачі уроку (score, mistakes json, дата)
- **Scene** → “сцена/історія” (діалог/міні-сюжет), відсортована по порядку
- **SceneStep** → крок у сцені (репліка або квіз)
- **SceneAttempt** → прогрес/спроба користувача по сцені (score, details json, completed)
- **VocabularyItem** → словникова одиниця (Word, Translation, Example)
- **UserVocabulary** → слово в SRS у користувача (next review, review count, тощо)
- **UserProgress** → агреговані метрики: `CompletedLessons`, `TotalScore`, streak/goal рахується на основі LessonResult + SceneAttempt
- **Achievement / UserAchievement** → нагороди (за уроки/сцени/серії/очки)

### Важливі налаштування (appsettings.json → секція `Learning`)
- `PassingScorePercent` = **80** (поріг “урок пройдено”, у відсотках)
- `ScenePassingPercent` = **100** за замовчуванням (поріг “сцена пройдена”, 100 = без помилок)
- `SceneCompletionScore` = **5** (скільки “очок” дає 1 завершена сцена в TotalScore)
- `DailyGoalScoreTarget` = **20** (ціль на день)
- `SceneUnlockEveryLessons` = **1** (як часто відкривати нову сцену, якщо не задано — береться дефолт з `LearningSettings`)
- SRS (словник) дефолти:
  - `VocabularyWrongDelayHours` = **12**
  - `VocabularyReviewIntervalsDays` = **[1, 2, 4, 7, 14, 30, 60]**

> Пороги нормалізуються в helper’ах (щоб не було 0% або >100% у логіці).

---

## 1) Авторизація

### 1.1 Реєстрація
`POST /api/auth/register`

- Створює `User`
- Повертає `AuthResponse`:
  - `token` (JWT)
  - `refreshToken`

### 1.2 Логін
`POST /api/auth/login` → той самий формат відповіді.

### 1.3 Refresh / Logout
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

Після логіну клієнт передає: `Authorization: Bearer <token>` для всіх `[Authorize]` endpoint’ів.

---

## 2) Вибір курсу і старт навчання

### 2.1 Отримати список курсів
`GET /api/courses`

Повертає тільки опубліковані курси.

### 2.2 Старт курсу (ключова точка входу в навчання)
`POST /api/learning/courses/{courseId}/start` *(Authorize)*

Цей endpoint робить **все необхідне**, щоб користувач міг почати навчатися:

1) Перевіряє, що курс існує і `IsPublished=true`.  
2) Забирає всі уроки курсу у правильному порядку (Topic order → Lesson order).  
3) Вимикає попередній активний курс користувача (`UserCourse.IsActive=false`).  
4) Створює/оновлює `UserCourse` для цього курсу:
   - `IsActive = true`
   - `StartedAt` (якщо перший раз)
   - `LastOpenedAt = now`
   - `LastLessonId = перший урок` (або буде “підправлено” sync-логікою)
5) Гарантує, що існує `UserLessonProgress` для **кожного** уроку курсу і відкриває перший:
   - перший урок: `IsUnlocked=true`
   - інші: locked
6) Гарантує, що `LastLessonId` вказує на урок, який:
   - unlocked
   - ще не completed  
   (інакше зміщує на перший доступний)

Відповідь: `ActiveCourseResponse`.

### 2.3 Отримати активний курс
`GET /api/learning/courses/active` *(Authorize)*  
Повертає активний курс або 404, якщо його нема.

---

## 3) Побудова “карти навчання” (темы → уроки, плюс сцени)

### 3.1 Повна карта для курсу (для UI)
`GET /api/learning-path/courses/{courseId}/me` *(Authorize)*

Повертає:
- `Topics[]` → `Lessons[]` з:
  - `IsUnlocked`
  - `IsPassed` (це `UserLessonProgress.IsCompleted`)
  - `BestScore`, `BestPercent`, `TotalQuestions`
- `Scenes[]`:
  - `IsUnlocked` / `UnlockReason`
  - `IsCompleted`
  - `PassedLessons`, `RequiredPassedLessons`
- `NextPointers`:
  - `NextLessonId` (перший unlocked і не completed)
  - `NextSceneId` (перший unlocked і не completed)

**Дуже важливо:** перед формуванням відповіді сервіс робить sync:
- рахує “passed lessons” через `LessonResults` (>= 80%)
- оновлює/створює `UserLessonProgress`
- відкриває наступний урок, якщо попередній став completed

Це означає, що прогрес може “догнатися” навіть якщо раніше щось не оновили вручну.

### 3.2 Уроки теми
`GET /api/topics/{topicId}/lessons`  
Повертає уроки теми (без перевірки lock).

### 3.3 Урок по Id (з перевіркою lock)
`GET /api/lesson-by-id/{lessonId}` *(Authorize)*

- Якщо `UserLessonProgress.IsUnlocked=false` → Forbidden з повідомленням, який урок треба пройти.

---

## 4) Як бекенд вибирає “що робити далі” (Next Activity)

### 4.1 Next preview (для Home)
`GET /api/next/me` *(Authorize)*

Відповідь: `NextPreviewResponse`:
- `Next` (NextActivityResponse)
- `Progress` (UserProgressResponse)
- `GeneratedAt`

### 4.2 Пріоритети (важливо для логіки UI)
Бекенд вибирає наступну активність в такому порядку:

1) **VocabularyReview** (якщо є слова, які вже “due”)
2) **Lesson** (активний курс; якщо активного нема — перший опублікований курс)
3) **Scene** (відкрита за кількістю пройдених уроків)

Якщо нічого нема — 204 No Content.

### 4.3 Як вибирається слово на повторення
- Береться перше слово, де `NextReviewAt <= now`, сортоване за `NextReviewAt`, потім `AddedAt`.

### 4.4 Як вибирається урок
- Синхронізується прогрес.
- Якщо `UserCourse.LastLessonId` існує і цей урок unlocked та ще не passed → він і повертається.
- Інакше повертається перший unlocked + не passed урок по порядку.

### 4.5 Як вибирається сцена
- Рахується кількість passed уроків у курсі (>=80%).
- Сцени беруться:
  - або прив’язані до курсу (`CourseId == courseId`)
  - або legacy (`CourseId == null`)
- Відкриття сцени:
  - `required = (позиція_сцени - 1) * SceneUnlockEveryLessons`
  - сцена unlocked, якщо `passedLessons >= required`

---

## 5) Уроки: вправи, відповіді, перевірка, прогрес

### 5.1 Відкрити урок
Типовий порядок на фронті:

1) переконатися, що курс стартований
2) отримати `Next` через `/api/next/me` або через learning-path
3) `GET /api/lesson-by-id/{lessonId}` — перевірка доступу + теорія

### 5.2 Завантажити вправи
`GET /api/exercises/by-lesson/{lessonId}` *(Authorize)*

Повертає вправи у порядку `Order → Id`.

`ExerciseResponse`:
- `Id`
- `Type` (string)
- `Question`
- `Data` (JSON string)
- `Order`

#### Формати Data (реально використані в seed + логіці перевірки)

**MultipleChoice**  
`Data` = JSON array рядків:
```json
["Coffee", "Tea", "Water"]
```
Відповідь користувача = один із рядків.

**Input**  
`Data` у seed зазвичай `"{}"` і не використовується для перевірки.  
Перевірка йде порівнянням з `CorrectAnswer` (нормалізація пробілів/регістру).

**Match**  
`Data` = JSON array пар:
```json
[
  {"left":"Hello","right":"Привіт"},
  {"left":"Goodbye","right":"До побачення"}
]
```
Відповідь користувача теж має бути JSON array у цьому форматі.  
Правило: мапа `left -> right` повинна повністю збігатися.

### 5.3 Здати урок
`POST /api/lesson-submit` *(Authorize)*

Тіло: `SubmitLessonRequest`
- `LessonId`
- `IdempotencyKey` (опційно, max 64)
- `Answers[]`:
  - `ExerciseId`
  - `Answer` (string)

Бекенд робить (по порядку):
1) Валідація request
2) Перевірка уроку/курсу (урок має бути в published course)
3) Перевірка unlocked
4) Idempotency (якщо такий ключ вже був — повертає попередню відповідь)
5) Вивантажує всі вправи уроку
6) Перевіряє дублікати ExerciseId
7) Перевіряє кожну вправу і рахує score
8) Формує `LessonResultDetailsJson` + список помилок
9) `IsPassed = (score/total) >= 80%`
10) Зберігає `LessonResult`
11) Якщо урок став passed:
    - `UserLessonProgress.IsCompleted=true`
    - unlock next lesson
    - рухає `UserCourse.LastLessonId` на наступний доступний
    - якщо всі уроки пройдені → `UserCourse.IsCompleted=true`
12) Оновлює `UserProgress`:
    - TotalScore = сума **best score по кожному уроку**
13) Додає словник по уроку (UserVocabulary)
14) Перевіряє і видає Achievements

---

## 6) Повторення помилок по уроку (Mistakes)

### 6.1 Отримати помилки останньої спроби
`GET /api/lesson-mistakes/{lessonId}` *(Authorize)*

- Береться **останній** `LessonResult` (CompletedAt desc).
- Парситься MistakesJson і повертаються тільки “помилкові” вправи.

### 6.2 Здати виправлені помилки
`POST /api/lesson-mistakes/{lessonId}/submit` *(Authorize)*

Оновлює **той самий** останній `LessonResult`:
- перераховує correctness тільки для mistake-вправ
- оновлює `Score`, `TotalQuestions`, `MistakesJson`

**Важлива деталь:** цей endpoint **напряму не відкриває** наступний урок.  
Але він може зробити `LessonResult` “passed” (80%), і тоді наступний виклик:
- `/api/next/me` або `/api/learning-path/.../me`  
зробить sync і відкриє наступний урок.

---

## 7) Сцени: кроки, квізи, здача

### 7.1 Де брати сцени
Сцени приходять у `LearningPathResponse.Scenes[]`.

Unlock залежить від кількості **passed** уроків у курсі.

### 7.2 Деталі сцени
`GET /api/scenes/{sceneId}` *(Authorize)*

### 7.3 Контент сцени (кроки)
`GET /api/scenes/{sceneId}/content` *(Authorize)*

`SceneStep`:
- `StepType`:
  - `"Line"` — репліка (не оцінюється)
  - `"Choice"` — вибір (оцінюється)
  - `"Input"` — введення (оцінюється)
- `ChoicesJson` використовується для Choice/Input

### 7.4 Здати сцену
`POST /api/scenes/{sceneId}/submit` *(Authorize)*

Тіло: `SubmitSceneRequest`
- `IdempotencyKey` (опційно)
- `Answers[]`:
  - `StepId`
  - `Answer`

Логіка:
1) Валідація
2) Перевірка unlock
3) Визначає question-steps (Choice/Input)
4) Порівнює відповіді з `ChoicesJson`:
   - Choice: список об’єктів з `text` + `isCorrect`
   - Input: об’єкт з `correctAnswer` + `acceptableAnswers[]`
5) Рахує `CorrectAnswers`, `TotalQuestions`
6) `IsCompleted = (correct/total) >= ScenePassingPercent` (зазвичай 100%)
7) Зберігає/оновлює `SceneAttempt`
   - якщо вже completed раніше — не видає нагороди вдруге, але details/score може оновити
   - якщо completed вперше:
     - додає vocabulary із сцени
     - оновлює UserProgress.TotalScore:
       - total = best lesson scores + completedScenesCount * SceneCompletionScore
     - дає achievements

---

## 8) Scene mistakes (виправити помилки по сцені)
- `GET /api/scenes/{sceneId}/mistakes`
- `POST /api/scenes/{sceneId}/mistakes/submit`

Аналогічно урокам: оновлюється attempt details/score, completion може настати після виправлень.

---

## 9) Словник (SRS)

Слова потрапляють у UserVocabulary з:
1) уроків (lesson-vocabulary links)
2) сцен (витягуються з текстів кроків при завершенні сцени)
3) ручного додавання

### 9.1 Мій словник
`GET /api/vocabulary/me`

### 9.2 Додати слово вручну
`POST /api/vocabulary/add`

### 9.3 Відповісти на повторення
`POST /api/vocabulary/{userVocabularyId}/review`  
Body: `{ "isCorrect": true/false }`

Після цього:
- якщо правильно → збільшує reviewCount і ставить наступну дату (інтервали)
- якщо неправильно → ставить повторення через `VocabularyWrongDelayHours`

---

## 10) Прогрес, streak, daily goal

### 10.1 Мій прогрес
`GET /api/progress/me`

Рахує:
- completed lessons (по 80% правилу)
- completed scenes
- completion percent
- streak (на основі UTC дат активності)

### 10.2 Моя ціль на день
`GET /api/progress/daily-goal/me`

Сьогоднішні очки (UTC):
- уроки: sum LessonResult.Score, але тільки ті спроби, що passed сьогодні
- сцени: sum SceneAttempt.Score (кількість правильних відповідей), якщо сцена completed сьогодні

Ціль: `DailyGoalScoreTarget`.

---

## 11) Що означає “завершено”

### 11.1 Урок завершений
Є хоча б один `LessonResult` із:
- `TotalQuestions>0`
- `Score/TotalQuestions >= PassingScorePercent`

### 11.2 Курс завершений
Усі уроки курсу passed → `UserCourse.IsCompleted=true`.

### 11.3 Сцена завершена
`Correct/Total >= ScenePassingPercent` (часто 100%).

---

## 12) Важливі нюанси для стабільного фронта

1) **IdempotencyKey** у lesson/scene submit — обов’язково для ретраїв.
2) **Sync** робиться в `/api/next/me` і learning-path — це “вирівнює” прогрес.
3) Більшість дат/логіки goal/streak — **UTC**.

---

## 13) Місця, які варто підсилити (якщо хочете “ближче до Duolingo”)

1) **Пріоритет Next:** Vocabulary → Lesson → Scene  
   Якщо хочете частіше сцени — можна змінити правила або чергувати.

2) **Різні моделі очок для сцен**
   - `TotalScore`: completedScenesCount * SceneCompletionScore
   - `DailyGoal`: SceneAttempt.Score (правильні відповіді)
   Краще уніфікувати XP.

3) **Lesson mistakes не відкриває одразу наступний урок**
   Прогрес відкривається через sync на наступному запиті.  
   Якщо треба “одразу” — можна викликати ту ж логіку, що й у lesson submit.

4) **ScenePassingPercent = 100** — дуже жорстко
   Можна зменшити до 80–90, а “Perfect scene” залишити як achievement.

5) **SceneAttempt зберігається як 1 запис на сцену**
   Якщо потрібна історія спроб — зробити append-only attempts.

6) **Витяг словника зі сцен з тексту**
   Для реального контенту краще явні links scene↔vocab.

7) **UTC day boundary**
   Для користувачів часто потрібно “локальний день” (timezone).

---

## 14) Короткий “happy path” для фронта

1) Login/Register → JWT
2) `GET /api/courses`
3) `POST /api/learning/courses/{courseId}/start`
4) Home: `GET /api/next/me`
5) Якщо Lesson:
   - `GET /api/lesson-by-id/{lessonId}`
   - `GET /api/exercises/by-lesson/{lessonId}`
   - `POST /api/lesson-submit`
   - (опційно) mistakes endpoints
   - `GET /api/next/me`
6) Якщо Scene:
   - `GET /api/scenes/{sceneId}/content`
   - `POST /api/scenes/{sceneId}/submit`
   - (опційно) mistakes endpoints
   - `GET /api/next/me`
7) Якщо VocabularyReview:
   - `POST /api/vocabulary/{userVocabularyId}/review`
   - `GET /api/next/me`

---

*Generated: 2026-02-18 14:16 UTC*
