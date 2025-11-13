using ITfoxtec.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FoxIDs
{
    public static class MarkdownExtension
    {
        private static readonly Regex markdownLinkRegex = new Regex(@"\[(.+?)\]\((.+?)\)", RegexOptions.Compiled);
        private static readonly Regex markdownBoldAsteriskRegex = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex markdownBoldUnderscoreRegex = new Regex(@"__(.+?)__", RegexOptions.Compiled);
        private static readonly Regex markdownItalicAsteriskRegex = new Regex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
        private static readonly Regex markdownItalicUnderscoreRegex = new Regex(@"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", RegexOptions.Compiled);
        private static readonly Regex markdownCodeRegex = new Regex(@"`(.+?)`", RegexOptions.Compiled);

        public static string ConvertMarkdownToHtml(this string markdown)
        {
            if (markdown.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var normalized = markdown.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            var builder = new StringBuilder();
            var paragraphLines = new List<string>();
            var listItems = new List<string>();

            void FlushParagraph()
            {
                if (paragraphLines.Count == 0)
                {
                    return;
                }

                var encodedLines = paragraphLines.Select(EncodeInlineMarkdown);
                builder.Append("<p>");
                builder.Append(string.Join("<br />", encodedLines));
                builder.Append("</p>");
                paragraphLines.Clear();
            }

            void FlushList()
            {
                if (listItems.Count == 0)
                {
                    return;
                }

                builder.Append("<ul>");
                foreach (var item in listItems)
                {
                    builder.Append("<li>");
                    builder.Append(EncodeInlineMarkdown(item));
                    builder.Append("</li>");
                }

                builder.Append("</ul>");
                listItems.Clear();
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();
                var trimmedStart = line.TrimStart();

                if (trimmedStart.Length == 0)
                {
                    FlushList();
                    FlushParagraph();
                    continue;
                }

                if (trimmedStart.StartsWith("### "))
                {
                    FlushList();
                    FlushParagraph();
                    builder.Append("<h3 class=\"h5\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(4)));
                    builder.Append("</h3>");
                    continue;
                }

                if (trimmedStart.StartsWith("## "))
                {
                    FlushList();
                    FlushParagraph();
                    builder.Append("<h2 class=\"h4\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(3)));
                    builder.Append("</h2>");
                    continue;
                }

                if (trimmedStart.StartsWith("# "))
                {
                    FlushList();
                    FlushParagraph();
                    builder.Append("<h1 class=\"h3\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(2)));
                    builder.Append("</h1>");
                    continue;
                }

                if (trimmedStart.StartsWith("- ") || trimmedStart.StartsWith("* "))
                {
                    FlushParagraph();
                    listItems.Add(trimmedStart.Substring(2));
                    continue;
                }

                FlushList();
                paragraphLines.Add(line);
            }

            FlushList();
            FlushParagraph();

            return builder.ToString();
        }

        private static string EncodeInlineMarkdown(string text)
        {
            if (text.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var encoded = WebUtility.HtmlEncode(text);

            encoded = markdownLinkRegex.Replace(encoded, match =>
            {
                var linkText = match.Groups[1].Value;
                var url = WebUtility.HtmlEncode(match.Groups[2].Value);
                return $"<a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{linkText}</a>";
            });

            encoded = markdownBoldAsteriskRegex.Replace(encoded, match => $"<strong>{match.Groups[1].Value}</strong>");
            encoded = markdownBoldUnderscoreRegex.Replace(encoded, match => $"<strong>{match.Groups[1].Value}</strong>");
            encoded = markdownItalicAsteriskRegex.Replace(encoded, match => $"<em>{match.Groups[1].Value}</em>");
            encoded = markdownItalicUnderscoreRegex.Replace(encoded, match => $"<em>{match.Groups[1].Value}</em>");
            encoded = markdownCodeRegex.Replace(encoded, match => $"<code>{match.Groups[1].Value}</code>");

            return encoded;
        }
    }
}
