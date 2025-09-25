using System.Reflection;
using FoxIDs.Logic;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class ValidateApiModelDynamicElementLogicTests
    {
        [Fact]
        public void SanitizeHtml_RemovesInlineEventAndStyleAttributes()
        {
            var sanitized = InvokeSanitizeHtml("<div STYLE=\"color:red\" OnClick=\"alert(1)\">Safe</div>");

            Assert.Equal("<div>Safe</div>", sanitized);
            Assert.DoesNotContain("style=", sanitized, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeHtml_RemovesStyleElementButKeepsParent()
        {
            var sanitized = InvokeSanitizeHtml("<section><style>.bad{}</style><span>ok</span></section>");

            Assert.Equal("<section><span>ok</span></section>", sanitized);
            Assert.DoesNotContain("<style", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeHtml_RemovesDisallowedScriptElement()
        {
            var sanitized = InvokeSanitizeHtml("<div><SCRIPT>alert('x')</SCRIPT><p>Safe</p></div>");

            Assert.Equal("<div><p>Safe</p></div>", sanitized);
            Assert.DoesNotContain("<script", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("<a href=\"javascript:alert(1)\">Click</a>", "<a>Click</a>")]
        [InlineData("<a href='vbscript:msgbox(1)'>Click</a>", "<a>Click</a>")]
        [InlineData("<img src=\"javascript:alert(1)\" alt=\"logo\" />", "<img alt=\"logo\" />")]
        [InlineData("<img SRCSET=\"javascript:alert(1) 1x, https://foxids.com/logo.png 2x\" alt=\"logo\" />", "<img alt=\"logo\" />")]
        [InlineData("<button formaction=\"javascript:alert(1)\">Send</button>", "")]
        public void SanitizeHtml_StripsDangerousUriProtocols(string html, string expected)
        {
            var sanitized = InvokeSanitizeHtml(html);

            Assert.Equal(expected, sanitized);
            Assert.DoesNotContain("javascript:", sanitized, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("vbscript:", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeHtml_RemovesSrcDocAttribute()
        {
            var sanitized = InvokeSanitizeHtml("<div srcdoc=\"<p>Inner</p>\">Content</div>");

            Assert.Equal("<div>Content</div>", sanitized);
            Assert.DoesNotContain("srcdoc", sanitized, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizeHtml_AllowsSafeMarkup()
        {
            var html = "<div class=\"safe\"><p data-info=\"42\">Text</p><a href=\"https://foxids.com\">FoxIDs</a></div>";
            var sanitized = InvokeSanitizeHtml(html);

            Assert.Equal(html, sanitized);
        }

        private static string InvokeSanitizeHtml(string html)
        {
            var sanitizeMethod = typeof(ValidateApiModelDynamicElementLogic).GetMethod("SanitizeHtml", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(sanitizeMethod);

            return (string)sanitizeMethod!.Invoke(null, new object[] { html })!;
        }
    }
}
