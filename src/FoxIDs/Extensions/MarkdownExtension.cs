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
            var listStack = new Stack<ListState>();

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

            void CloseCurrentListItem()
            {
                if (listStack.Count == 0)
                {
                    return;
                }

                var current = listStack.Peek();
                if (current.ItemOpen)
                {
                    builder.Append("</li>");
                    current.ItemOpen = false;
                }
            }

            void FlushListStack()
            {
                while (listStack.Count > 0)
                {
                    var state = listStack.Pop();
                    if (state.ItemOpen)
                    {
                        builder.Append("</li>");
                        state.ItemOpen = false;
                    }

                    builder.Append(state.Ordered ? "</ol>" : "</ul>");
                }
            }

            void EnsureList(int indent, bool ordered)
            {
                while (listStack.Count > 0 && indent < listStack.Peek().Indent)
                {
                    FlushListStackLevel();
                }

                if (listStack.Count > 0 && indent == listStack.Peek().Indent && listStack.Peek().Ordered != ordered)
                {
                    FlushListStackLevel();
                }

                if (listStack.Count > 0 && indent == listStack.Peek().Indent)
                {
                    CloseCurrentListItem();
                    return;
                }

                if (listStack.Count == 0 || indent > listStack.Peek().Indent)
                {
                    builder.Append(ordered ? "<ol>" : "<ul>");
                    listStack.Push(new ListState { Indent = indent, Ordered = ordered });
                }
            }

            void FlushListStackLevel()
            {
                if (listStack.Count == 0)
                {
                    return;
                }

                var state = listStack.Pop();
                if (state.ItemOpen)
                {
                    builder.Append("</li>");
                    state.ItemOpen = false;
                }

                builder.Append(state.Ordered ? "</ol>" : "</ul>");
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();
                var indent = GetIndent(rawLine);
                var trimmedStart = rawLine.TrimStart(' ', '\t');

                if (trimmedStart.Length == 0)
                {
                    FlushListStack();
                    FlushParagraph();
                    continue;
                }

                if (trimmedStart == "---")
                {
                    FlushListStack();
                    FlushParagraph();
                    builder.Append("<hr />");
                    continue;
                }

                if (trimmedStart.StartsWith("### "))
                {
                    FlushListStack();
                    FlushParagraph();
                    builder.Append("<h3 class=\"h5\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(4)));
                    builder.Append("</h3>");
                    continue;
                }

                if (trimmedStart.StartsWith("## "))
                {
                    FlushListStack();
                    FlushParagraph();
                    builder.Append("<h2 class=\"h4\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(3)));
                    builder.Append("</h2>");
                    continue;
                }

                if (trimmedStart.StartsWith("# "))
                {
                    FlushListStack();
                    FlushParagraph();
                    builder.Append("<h1 class=\"h3\">");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(2)));
                    builder.Append("</h1>");
                    continue;
                }

                if (trimmedStart.StartsWith("- ") || trimmedStart.StartsWith("* "))
                {
                    FlushParagraph();
                    EnsureList(indent, ordered: false);
                    var current = listStack.Peek();
                    builder.Append("<li>");
                    builder.Append(EncodeInlineMarkdown(trimmedStart.Substring(2)));
                    current.ItemOpen = true;
                    continue;
                }

                if (TryGetOrderedListItem(trimmedStart, out var orderedItem))
                {
                    FlushParagraph();
                    EnsureList(indent, ordered: true);
                    var current = listStack.Peek();
                    builder.Append("<li>");
                    builder.Append(EncodeInlineMarkdown(orderedItem));
                    current.ItemOpen = true;
                    continue;
                }

                FlushListStack();
                paragraphLines.Add(line);
            }

            FlushListStack();
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

        private static bool TryGetOrderedListItem(string line, out string content)
        {
            content = default;
            if (line.Length < 3 || !char.IsDigit(line[0]))
            {
                return false;
            }

            var index = 0;
            while (index < line.Length && char.IsDigit(line[index]))
            {
                index++;
            }

            if (index == 0 || index + 1 >= line.Length)
            {
                return false;
            }

            if (line[index] != '.' || line[index + 1] != ' ')
            {
                return false;
            }

            content = line.Substring(index + 2);
            return true;
        }

        private static int GetIndent(string line)
        {
            var index = 0;
            while (index < line.Length && (line[index] == ' ' || line[index] == '\t'))
            {
                index++;
            }

            return index;
        }

        private sealed class ListState
        {
            public int Indent { get; set; }
            public bool Ordered { get; set; }
            public bool ItemOpen { get; set; }
        }
    }
}
