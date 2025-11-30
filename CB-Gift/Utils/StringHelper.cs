using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CB_Gift.Utils
{
    public static class StringHelper
    {
        // Hàm xóa dấu tiếng Việt và ký tự đặc biệt
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);

            // Thay thế 'đ' và 'Đ'
            noDiacritics = noDiacritics.Replace("đ", "d").Replace("Đ", "D");

            // Chỉ giữ lại chữ cái, số, và khoảng trắng
            noDiacritics = Regex.Replace(noDiacritics, "[^a-zA-Z0-9 ]", "", RegexOptions.Compiled);

            return noDiacritics;
        }
    }
}