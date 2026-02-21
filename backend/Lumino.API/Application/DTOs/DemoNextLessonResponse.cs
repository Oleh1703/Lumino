namespace Lumino.Api.Application.DTOs
{
    public class DemoNextLessonResponse
    {
        public int Step { get; set; }

        // зручніше для UI (1..N)
        public int StepNumber { get; set; }

        public int Total { get; set; }

        public bool IsLast { get; set; }

        // текст для CTA на останньому кроці
        public string CtaText { get; set; } = string.Empty;

        // щоб фронт не думав сам
        public bool ShowRegisterCta { get; set; }

        // готовий текст для UI ("Урок 2 з 3")
        public string LessonNumberText { get; set; } = string.Empty;

        public LessonResponse Lesson { get; set; } = null!;
    }
}
