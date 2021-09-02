using System.Globalization;
using System.Text;

namespace TextMiningExtensions
{
    public static class StringExtensions
    {

        public static string NormalizeString(this string input)
        {
            return input.RemoveDiacritics().Trim().ToLower();
        }

        public static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }        
    }
}
