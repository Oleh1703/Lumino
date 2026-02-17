using System;
using System.Collections.Generic;
using System.Linq;

namespace Lumino.Api.Utils
{
    public static class TheoryVocabularyExtractor
    {
        private static readonly char[] SplitChars = new[]
        {
            '\r', '\n', ',', ';', '|', '\t'
        };

        private static readonly char[] TrimChars = new[]
        {
            ' ', '.', '!', '?', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '–', '—', '•'
        };

        public static List<string> ExtractNonPairWords(string theory)
        {
            if (string.IsNullOrWhiteSpace(theory))
            {
                return new List<string>();
            }

            // Якщо Theory містить "=", це вже "word = translation" — такі уроки обробляються іншим кодом.
            if (theory.Contains('='))
            {
                return new List<string>();
            }

            // Підтримуємо простий формат: "One, Two, Three" або по рядках/булітах.
            // Нічого не вигадуємо: повертаємо лише нормалізовані токени.
            var rawParts = theory
                .Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var result = new List<string>();

            foreach (var part in rawParts)
            {
                var cleaned = part.Trim(TrimChars);

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    continue;
                }

                // Прибираємо зайві пробіли всередині
                cleaned = string.Join(' ', cleaned
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    continue;
                }

                var normalized = cleaned.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (!result.Contains(normalized))
                {
                    result.Add(normalized);
                }
            }

            return result;
        }
    }
}
