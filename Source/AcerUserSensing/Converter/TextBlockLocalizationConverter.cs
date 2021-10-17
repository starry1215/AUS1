using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace AcerUserSensing.Converter
{
    class TextBlockLocalizationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length >= 3)
            {
                var tblk = values[0] as TextBlock;
                var format = values[1] as string;
                var tokens = Tokenizer.ParseTokens(format);
                int hyperLinkIndex = 0;
                for (int i = 2; i < values.Length; i++)
                {
                    var token = tokens.FirstOrDefault((p) => string.Equals(p.Value as string, "{" + hyperLinkIndex + "}"));
                    if (token != null)
                    {
                        token.Value = values[i];
                    }
                    hyperLinkIndex++;
                }

                tblk.Inlines.Clear();
                foreach (var token in tokens)
                {
                    if (token.Value is Hyperlink)
                        tblk.Inlines.Add((Hyperlink)token.Value);
                    else
                        tblk.Inlines.Add(new Run(token.Value as string));
                }

                return tblk;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private class Token
        {
            public object Value { get; set; }
            public Token(object value)
            {
                Value = value;
            }
        }
        private class HyperlinkToken : Token
        {
            public HyperlinkToken(object value) : base(value) { }
        }

        private static class Tokenizer
        {
            public static List<Token> ParseTokens(string format)
            {
                var tokens = new List<Token>();
                var strings = Regex.Split(format, @"({\d+})");
                foreach (var str in strings)
                {
                    if (Regex.IsMatch(str, @"({\d+})"))
                    {
                        tokens.Add(new HyperlinkToken(str));
                    }
                    else
                    {
                        tokens.Add(new Token(str));
                    }
                }
                return tokens;
            }
        }
    }
}
