using FoxIDs;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class MarkdownExtensionTests
    {
        [Fact]
        public void ConvertMarkdownToHtml_NumberedList_UsesOrderedList()
        {
            var markdown = "1. First\n2. Second\n3. Third";

            var html = markdown.ConvertMarkdownToHtml();

            Assert.Equal("<ol><li>First</li><li>Second</li><li>Third</li></ol>", html);
        }

        [Fact]
        public void ConvertMarkdownToHtml_HorizontalRule_AddsHrTag()
        {
            var markdown = "Before\n---\nAfter";

            var html = markdown.ConvertMarkdownToHtml();

            Assert.Equal("<p>Before</p><hr /><p>After</p>", html);
        }

        [Fact]
        public void ConvertMarkdownToHtml_NestedUnorderedList_RendersNestedUl()
        {
            var markdown = "- Parent\n  - Child\n    - Grandchild";

            var html = markdown.ConvertMarkdownToHtml();

            Assert.Equal("<ul><li>Parent<ul><li>Child<ul><li>Grandchild</li></ul></li></ul></li></ul>", html);
        }

        [Fact]
        public void ConvertMarkdownToHtml_NestedOrderedAndUnorderedList_RendersMixedLists()
        {
            var markdown = "1. First\n   - Child\n2. Second";

            var html = markdown.ConvertMarkdownToHtml();

            Assert.Equal("<ol><li>First<ul><li>Child</li></ul></li><li>Second</li></ol>", html);
        }
    }
}
