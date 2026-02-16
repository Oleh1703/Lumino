diff --git a/docs/LearningFlow.md b/docs/LearningFlow.md
new file mode 100644
index 0000000..b73c9e1
--- /dev/null
+++ b/docs/LearningFlow.md
@@ -0,0 +1,221 @@
+# Lumino API — навчальний флоу (клон Duolingo)
+
+> Документ для фронтенду: які ендпоінти викликати, у якому порядку, і як інтерпретувати стани `locked/unlocked/completed` та `NextActivity`.
+
+## 0) Авторизація (перед будь-яким навчанням)
+1) Реєстрація / логін → отримати JWT токен.
+2) Усі навчальні ендпоінти з `[Authorize]` викликати з заголовком:
+- `Authorization: Bearer <token>`
+
+> В бекенді userId береться з JWT (`ClaimsUtils.GetUserIdOrThrow(User)`).
+
+---
+
+## 1) Вибір курсу (екран “Courses / Оберіть курс”)
+
+### 1.1 Отримати список курсів
+- `GET /api/courses`
+- Повертає тільки опубліковані курси.
+
+### 1.2 Старт / активація курсу (обовʼязково перед навчанням)
+- `POST /api/learning/courses/{courseId}/start`
+- Робить курс активним для користувача:
+  - створює `UserCourse` якщо його не було
+  - виставляє `IsActive = true`, оновлює `StartedAt`, `LastOpenedAt`
+  - синхронізує `UserLessonProgresses` для всіх уроків курсу (locked/unlocked/completed)
+  - гарантує, що є хоча б один `unlocked` урок для старту/продовження
+- Відповідь: `ActiveCourseResponse`
+```json
+{
+  "courseId": 1,
+  "startedAt": "2026-02-16T12:00:00Z",
+  "lastLessonId": 10,
+  "lastOpenedAt": "2026-02-16T12:00:00Z"
+}
+```
+
+### 1.3 Отримати активний курс (опційно, при старті застосунку)
+- `GET /api/learning/courses/active`
+- Якщо активного курсу немає → `404`.
+
+---
+
+## 2) Learning Path (екран “Доріжка навчання”)
+
+### 2.1 Отримати path по курсу для поточного користувача
+- `GET /api/learning/courses/{courseId}/path/me`
+- Відповідь: `LearningPathResponse`
+  - `Topics[]` та `Lessons[]` відсортовані по правилу: `Order якщо > 0`, інакше `Id`.
+  - `Lessons` містять стани, які фронт використовує для UI (кнопка, замок, прогрес).
+```json
+{
+  "courseId": 1,
+  "courseTitle": "English A1",
+  "topics": [
+    {
+      "id": 1,
+      "title": "Basics",
+      "order": 1,
+      "lessons": [
+        {
+          "id": 10,
+          "title": "Greetings",
+          "order": 1,
+          "isUnlocked": true,
+          "isPassed": true,
+          "bestScore": 8,
+          "totalQuestions": 10,
+          "bestPercent": 80
+        }
+      ]
+    }
+  ]
+}
+```
+
+### 2.2 Значення станів уроків
+- `isUnlocked = true` → урок доступний (можна заходити, робити вправи).
+- `isPassed = true` → урок пройдено (score >= passing threshold).
+- “Completed” у path по суті = `isPassed` (для уроків).
+
+> Якщо урок `locked` і фронт все ж викличе `GET /api/lessons/{id}` або `GET /api/lessons/{id}/exercises` — бекенд поверне `403` з помилкою.
+
+---
+
+## 3) NextActivity (кнопка “Continue / Продовжити”)
+
+### 3.1 Отримати наступну активність
+- `GET /api/next/me`
+  - (є також alias: `GET /api/learning/next`)
+- Відповідь: `NextActivityResponse` або `204 No Content` якщо нема що робити.
+
+### 3.2 Пріоритети NextActivity (важливо для Duolingo-логіки)
+1) **VocabularyReview**, якщо є слово `due` (`NextReviewAt <= now`)
+2) **Lesson**, якщо є `unlocked` і ще не `passed`
+3) **Scene**, якщо є `uncompleted` і вона вже `unlocked` по правилу сцен
+
+```json
+// варіант: VocabularyReview
+{
+  "type": "VocabularyReview",
+  "userVocabularyId": 123,
+  "vocabularyItemId": 77,
+  "word": "hello",
+  "translation": "привіт"
+}
+```
+
+```json
+// варіант: Lesson
+{
+  "type": "Lesson",
+  "lessonId": 10,
+  "topicId": 1,
+  "lessonTitle": "Greetings"
+}
+```
+
+```json
+// варіант: Scene
+{
+  "type": "Scene",
+  "sceneId": 5,
+  "sceneTitle": "Cafe dialog"
+}
+```
+
+---
+
+## 4) Уроки (Lesson flow)
+
+### 4.1 Відкрити урок
+- `GET /api/lessons/{lessonId}`
+- Повертає: `LessonResponse` (теорія + метадані).
+
+### 4.2 Отримати вправи уроку
+- `GET /api/lessons/{lessonId}/exercises`
+- Повертає список `ExerciseResponse`.
+
+> Кожна вправа має `id` — фронт надсилає відповіді саме по `id`, порядок на фронті може бути по `order`.
+
+### 4.3 Надіслати відповіді (submit)
+- `POST /api/lesson-submit`
+- Body: `SubmitLessonRequest`
+```json
+{
+  "lessonId": 10,
+  "answers": [
+    { "exerciseId": 1001, "answer": "hello" },
+    { "exerciseId": 1002, "answer": "goodbye" }
+  ]
+}
+```
+
+- Відповідь: `SubmitLessonResponse`
+  - `isPassed` — пройдено чи ні
+  - `mistakeExerciseIds` — вправи з помилками (для “повторити помилки”)
+  - `answers[]` — деталізація (правильна/користувацька відповідь)
+```json
+{
+  "totalExercises": 10,
+  "correctAnswers": 8,
+  "isPassed": true,
+  "mistakeExerciseIds": [1003, 1007],
+  "answers": [
+    { "exerciseId": 1001, "userAnswer": "hello", "correctAnswer": "hello", "isCorrect": true }
+  ]
+}
+```
+
+### 4.4 Що відбувається в бекенді після submit уроку
+Якщо `isPassed = true`:
+- зберігається `LessonResult` (score/total/mistakes)
+- оновлюється `UserLessonProgress`:
+  - поточний урок → `IsCompleted = true`
+  - наступний урок у курсі → `IsUnlocked = true`
+- оновлюється `UserCourse.LastLessonId` (щоб “продовжити” з нього)
+- оновлюється `UserProgress` (score, completed lessons, streak)
+- додаються слова у словник користувача (див. розділ Vocabulary)
+- перевіряються досягнення (achievements)
+
+Якщо `isPassed = false`:
+- результат зберігається, але **unlock наступного уроку не відбувається**.
+
+---
+
+## 5) Сцени (Scene flow — “історії” як у Duolingo)
+
+### 5.1 Список сцен
+- `GET /api/scenes`
+- Повертає загальний список сцен (метадані).
+
+### 5.2 Деталі сцени (включно з locked/unlocked)
+- `GET /api/scenes/{sceneId}`
+- Повертає `SceneDetailsResponse`:
+  - `isUnlocked`
+  - `unlockReason` (що показати на UI коли locked)
+  - `passedLessons` / `requiredPassedLessons`
+
+### 5.3 Контент сцени
+- `GET /api/scenes/{sceneId}/content`
+- Повертає `SceneContentResponse`
+  - якщо `isUnlocked = false`, `steps` буде порожній список.
+
+### 5.4 Submit сцени (відповіді)
+- `POST /api/scenes/{sceneId}/submit`
+- Body: `SubmitSceneRequest`
+```json
+{
+  "answers": [
+    { "stepId": 501, "answer": "A" },
+    { "stepId": 502, "answer": "B" }
+  ]
+}
+```
+
+- Відповідь: `SubmitSceneResponse`
+  - `isCompleted = true`, якщо `correctAnswers == totalQuestions`
+  - `mistakeStepIds` + `answers[]` для UI помилок
+
+> Якщо в сцені **немає питань** (тільки діалоги), submit одразу завершує сцену як completed.
+
+### 5.5 “Повторити помилки” по сцені
+- `GET /api/scenes/{sceneId}/mistakes`
+  - повертає `SceneMistakesResponse` (кроки з помилками)
+- `POST /api/scenes/{sceneId}/mistakes/submit`
+  - працює як retry: перераховує тільки кроки, які були в помилках, і оновлює attempt
+
+### 5.6 Правило unlock сцен
+Сцени відкриваються по кількості **passed уроків** (в рамках курсу сцени):
+- `SceneUnlockEveryLessons` (налаштування) визначає, після скількох passed уроків відкривається наступна сцена.
+
+---
+
+## 6) Vocabulary (слова + повторення)
+
+### 6.1 Як слова попадають у vocabulary
+Після **успішного** проходження уроку (`isPassed = true`) бекенд:
+- бере слова з `LessonVocabularies` (якщо привʼязка є),
+- і/або парсить `lesson.theory` (fallback),
+- додатково бере слова з помилкових вправ (`ExerciseVocabularies` або fallback з відповідей),
+- записує в `UserVocabularies` з `NextReviewAt`:
+  - якщо слово було в помилках → `NextReviewAt = now` (тобто “due” одразу)
+  - інакше → `now + 1 day`
+
+### 6.2 Список всіх моїх слів
+- `GET /api/vocabulary/me`
+
+### 6.3 Скільки “due” прямо зараз
+- `GET /api/vocabulary/due`
+- Повертає список слів, де `NextReviewAt <= now`.
+
+### 6.4 Взяти 1 наступне слово на повторення
+- `GET /api/vocabulary/review/next`
+- Якщо нема due → `204 No Content`.
+
+### 6.5 Відмітити повторення
+- `POST /api/vocabulary/{userVocabularyId}/review`
+- Body:
+```json
+{ "isCorrect": true }
+```
+- Якщо `isCorrect=true` → `reviewCount++`, `nextReviewAt` ставиться за інтервалами (`VocabularyReviewIntervalsDays`).
+- Якщо `isCorrect=false` → `reviewCount=0`, `nextReviewAt = now + VocabularyWrongDelayHours`.
+
+---
+
+## 7) Прогрес і результати
+
+### 7.1 Загальний прогрес користувача
+- `GET /api/progress/me`
+- Повертає `UserProgressResponse`:
+  - completed lessons / completion percent
+  - streak (рахується по датах успішних уроків та completed сцен)
+
+### 7.2 Результати уроків (історія)
+- `GET /api/results/me` — список результатів
+- `GET /api/results/me/{resultId}` — деталі конкретної спроби (з правильними відповідями)
+
+### 7.3 Completion по курсу
+- `GET /api/learning/courses/{courseId}/completion/me`
+- Повертає `CourseCompletionResponse` (уроки + сцени, якщо вони привʼязані до курсу).
+
+---
+
+## 8) Контракт помилок (для фронтенду)
+Будь-яка помилка повертається у форматі:
+```json
+{
+  "statusCode": 403,
+  "type": "forbidden",
+  "message": "Lesson is locked",
+  "traceId": "....",
+  "path": "/api/lessons/10",
+  "timestampUtc": "2026-02-16T12:00:00Z"
+}
+```
+
+Типові статуси:
+- `400 bad_request` — неправильний request / дублікати id у answers
+- `401 unauthorized` — нема/некоректний токен
+- `403 forbidden` — locked урок/сцена або заборонена дія
+- `404 not_found` — не знайдено ресурс
+
+---
+
+## 9) Мінімальний “happy path” (повна послідовність викликів)
+1) `GET /api/courses`
+2) `POST /api/learning/courses/{courseId}/start`
+3) `GET /api/learning/courses/{courseId}/path/me`
+4) `GET /api/next/me`
+5) якщо `Lesson`:
+   - `GET /api/lessons/{lessonId}`
+   - `GET /api/lessons/{lessonId}/exercises`
+   - `POST /api/lesson-submit`
+   - (опційно) `GET /api/next/me` (щоб перейти далі)
+6) якщо `Scene`:
+   - `GET /api/scenes/{sceneId}`
+   - `GET /api/scenes/{sceneId}/content`
+   - `POST /api/scenes/{sceneId}/submit`
+   - якщо є помилки → `GET /api/scenes/{sceneId}/mistakes` + `POST /api/scenes/{sceneId}/mistakes/submit`
+7) якщо `VocabularyReview`:
+   - `POST /api/vocabulary/{userVocabularyId}/review`
+   - `GET /api/next/me`
+
+---
+
+## Перевірка + коміт (після додавання документа)
+
+1) Запустити тести:
+```bash
+dotnet test .\Lumino.Tests\Lumino.Tests.csproj
+```
+
+2) Додати файл і закомітити:
+```bash
+git add .\docs\LearningFlow.md
+git commit -m "docs: add LearningFlow API documentation for frontend"
+```
