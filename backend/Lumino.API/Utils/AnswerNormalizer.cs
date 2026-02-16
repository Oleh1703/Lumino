using System.Text.RegularExpressions;

namespace Lumino.Api.Utils
{
    public static class AnswerNormalizer
    {
        private static readonly Regex MultiWhitespace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();

            trimmed = MultiWhitespace.Replace(trimmed, " ");

            return trimmed.ToLowerInvariant();
        }
    }
}
