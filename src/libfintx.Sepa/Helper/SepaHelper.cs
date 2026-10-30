using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace libfintx.Sepa.Helper
{
    public static class SepaHelper
    {
        public static string Escape(string str)
        {
            if (str == null)
                return null;

            // Zunächst sicherstellen, dass nur gemäß SEPA gültige Zeichen verwendet werden,
            // erst danach XML-escapen. In umgekehrter Reihenfolge würde aus "&amp;" durch das
            // Entfernen von '&' und ';' der Text "amp" (z.B. "A & B" -> "A amp B").
            var valid = ConvertToValidSepaString(str);
            return SecurityElement.Escape(valid);
        }

        /// <summary>
        /// Character set of the DFÜ-Abkommen Anlage 3 V3.9 (pp. 82-84) for pain.001.001.09: letters, digits, the
        /// characters ' : ? , - ( + . ) / space and Ä Ö Ü ä ö ü ß &amp; * $ % are kept; € becomes EUR, typographic
        /// quotes and dashes become ' and -, accented letters their base letter, anything else a full stop.
        /// </summary>
        public static string ConvertToDkCharacterSet(string str)
        {
            if (str == null)
                return null;

            var sb = new System.Text.StringBuilder(str.Length);
            foreach (var c in str)
            {
                if (c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || "':?,-(+.)/ ÄÖÜäöüß&*$%".IndexOf(c) >= 0)
                {
                    sb.Append(c);
                    continue;
                }
                switch (c)
                {
                    case '€': sb.Append("EUR"); continue;
                    case '\t': case '\r': case '\n': case '\u00A0': sb.Append(' '); continue;
                    case '"': case '“': case '”': case '„': case '‚': case '‘': case '’': case '`': case '´': sb.Append('\''); continue;
                    case '–': case '—': case '_': sb.Append('-'); continue;
                    case 'Æ': sb.Append("AE"); continue;
                    case 'æ': sb.Append("ae"); continue;
                    case 'Ø': sb.Append('O'); continue;
                    case 'ø': sb.Append('o'); continue;
                }
                // accented letters: base letter (é -> e), anything else a full stop
                var decomposed = c.ToString().Normalize(System.Text.NormalizationForm.FormD);
                sb.Append(decomposed.Length > 0 && (decomposed[0] >= 'A' && decomposed[0] <= 'Z' || decomposed[0] >= 'a' && decomposed[0] <= 'z') ? decomposed[0] : '.');
            }
            return sb.ToString();
        }

        public static string ConvertToValidSepaString(string str)
        {
            return new string(str.Select(c => ConvertToValidSepaString(c)).SelectMany(s => s).ToArray());
        }

        /// <summary>
        /// Hier findet kein XML-Escaping statt, sondern es wird sichergestellt, dass nur die in SEPA gültigen Zeichen verwendet werden.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private static string ConvertToValidSepaString(char c)
        {
            switch (c)
            {
                case 'Ä': return "Ae";
                case 'Ö': return "Oe";
                case 'Ü': return "Ue";
                case 'ä': return "ae";
                case 'ö': return "oe";
                case 'ü': return "ue";
                case 'ß': return "ss";
            }

            if (!Regex.Match($"{c}", @"^[\sa-zA-Z0-9/?:\(\)\.,'+-]$").Success)
                return string.Empty;

            return $"{c}";
        }
    }
}