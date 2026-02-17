using Lumino.Api.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Lumino.Api.Utils
{
    public static class SceneVocabularyExtractor
    {
        public static HashSet<string> ExtractVocabularyKeys(List<SceneStep> steps)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (steps == null || steps.Count == 0)
            {
                return result;
            }

            foreach (var step in steps)
            {
                if (step == null)
                {
                    continue;
                }

                // 1) From dialog text
                AddFromText(result, step.Text);

                // 2) From correct answers (Choice/Input)
                if (!string.IsNullOrWhiteSpace(step.ChoicesJson))
                {
                    AddFromChoicesJson(result, step.StepType, step.ChoicesJson!);
                }
            }

            return result;
        }

        private static void AddFromText(HashSet<string> set, string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var tokens = Tokenize(text);

            // unigrams
            foreach (var t in tokens)
            {
                set.Add(t);
            }

            // bigrams + trigrams (phrases)
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i + 1 < tokens.Count)
                {
                    set.Add(tokens[i] + " " + tokens[i + 1]);
                }

                if (i + 2 < tokens.Count)
                {
                    set.Add(tokens[i] + " " + tokens[i + 1] + " " + tokens[i + 2]);
                }
            }
        }

        private static void AddFromChoicesJson(HashSet<string> set, string stepType, string choicesJson)
        {
            if (string.IsNullOrWhiteSpace(choicesJson))
            {
                return;
            }

            try
            {
                using var doc = JsonDocument.Parse(choicesJson);

                // Choice: [{"text":"...","isCorrect":true}, ...]
                if (string.Equals(stepType, "Choice", StringComparison.OrdinalIgnoreCase))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        return;
                    }

                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        if (!item.TryGetProperty("isCorrect", out var flag) || flag.ValueKind != JsonValueKind.True)
                        {
                            continue;
                        }

                        if (!item.TryGetProperty("text", out var txt) || txt.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var val = NormalizePhrase(txt.GetString());
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            set.Add(val);
                        }
                    }

                    return;
                }

                // Input: {"correctAnswer":"...","acceptableAnswers":["...","..."]}
                if (string.Equals(stepType, "Input", StringComparison.OrdinalIgnoreCase))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return;
                    }

                    var correctAnswer = TryGetString(doc.RootElement, "correctAnswer")
                        ?? TryGetString(doc.RootElement, "CorrectAnswer");

                    var normalized = NormalizePhrase(correctAnswer);

                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        set.Add(normalized);
                    }

                    if (doc.RootElement.TryGetProperty("acceptableAnswers", out var acceptable)
                        && acceptable.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var a in acceptable.EnumerateArray())
                        {
                            if (a.ValueKind != JsonValueKind.String)
                            {
                                continue;
                            }

                            var val = NormalizePhrase(a.GetString());
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                set.Add(val);
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore invalid json
            }
        }

        private static string? TryGetString(JsonElement obj, string prop)
        {
            if (!obj.TryGetProperty(prop, out var el))
            {
                return null;
            }

            if (el.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return el.GetString();
        }

        private static string NormalizePhrase(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var tokens = Tokenize(value);

            if (tokens.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", tokens);
        }

        private static List<string> Tokenize(string value)
        {
            var list = new List<string>();

            if (string.IsNullOrWhiteSpace(value))
            {
                return list;
            }

            // normalize curly apostrophe to ASCII apostrophe
            value = value.Replace('\u2019', '\'');

            var buffer = new char[value.Length];
            int len = 0;

            foreach (var ch in value)
            {
                if (char.IsLetter(ch) || ch == '\'')
                {
                    buffer[len++] = char.ToLowerInvariant(ch);
                    continue;
                }

                if (len > 0)
                {
                    list.Add(new string(buffer, 0, len));
                    len = 0;
                }
            }

            if (len > 0)
            {
                list.Add(new string(buffer, 0, len));
            }

            // remove 1-letter noise and standalone apostrophes
            return list
                .Select(x => x.Trim('\''))
                .Where(x => x.Length > 1)
                .ToList();
        }
    }
}
