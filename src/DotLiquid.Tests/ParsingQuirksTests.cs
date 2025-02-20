using DotLiquid.Exceptions;
using DotLiquid.NamingConventions;
using DotLiquid.Tests.Util;
using NUnit.Framework;

namespace DotLiquid.Tests
{
    [TestFixture]
    public class ParsingQuirksTests
    {
        private INamingConvention NamingConvention { get; } = TestsDefaultNamingConvention.GetDefaultNamingConvention();

        [Test]
        public void TestErrorWithCss()
        {
            const string text = " div { font-weight: bold; } ";
            Template template = Template.Parse(text, NamingConvention);
            Assert.AreEqual(text, template.Render());
            Assert.AreEqual(1, template.Root.NodeList.Count);
            Assert.IsInstanceOf<string>(template.Root.NodeList[0]);
        }

        [Test]
        public void TestRaiseOnSingleCloseBrace()
        {
            Assert.Throws<SyntaxException>(() => Template.Parse("text {{method} oh nos!", NamingConvention));
        }

        [Test]
        public void TestRaiseOnLabelAndNoCloseBrace()
        {
            Assert.Throws<SyntaxException>(() => Template.Parse("TEST {{ ", NamingConvention));
        }

        [Test]
        public void TestRaiseOnLabelAndNoCloseBracePercent()
        {
            Assert.Throws<SyntaxException>(() => Template.Parse("TEST {% ", NamingConvention));
        }

        [Test]
        public void TestErrorOnEmptyFilter()
        {
            Assert.DoesNotThrow(() =>
            {
                Template.Parse("{{test |a|b|}}", NamingConvention);
                Template.Parse("{{test}}", NamingConvention);
                Template.Parse("{{|test|}}", NamingConvention);
            });
        }

        [Test]
        public void TestMeaninglessParens()
        {
            Hash assigns = Hash.FromAnonymousObject(new { b = "bar", c = "baz" });
            Helper.AssertTemplateResult(" YES ", "{% if a == 'foo' or (b == 'bar' and c == 'baz') or false %} YES {% endif %}", assigns);
        }

        [Test]
        public void TestUnexpectedCharactersSilentlyEatLogic()
        {
            Helper.AssertTemplateResult(" YES ", "{% if true && false %} YES {% endif %}");
            Helper.AssertTemplateResult("", "{% if false || true %} YES {% endif %}");
        }

        [Test]
        public void TestLiquidTagsInQuotes()
        {
            Helper.AssertTemplateResult("{{ {% %} }}", "{{ '{{ {% %} }}' }}");
            Helper.AssertTemplateResult("{{ {% %} }}", "{% assign x = '{{ {% %} }}' %}{{x}}");
        }

        [TestCase(".")]
        [TestCase("x.")]
        [TestCase("$x")]
        [TestCase("x?")]
        [TestCase("x¿")]
        [TestCase(".y")]
        public void TestVariableNotTerminatedFromInvalidVariableName(string variableName)
        {
            var template = Template.Parse("{{ " + variableName + " }}", NamingConvention);
            SyntaxException ex = Assert.Throws<SyntaxException>(() => template.Render(new RenderParameters(System.Globalization.CultureInfo.InvariantCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new { x = "" }),
                ErrorsOutputMode = ErrorsOutputMode.Rethrow,
                SyntaxCompatibilityLevel = SyntaxCompatibility.DotLiquid22
            }));
            Assert.AreEqual(
                expected: string.Format(Liquid.ResourceManager.GetString("VariableNotTerminatedException"), variableName),
                actual: ex.Message);

            template = Template.Parse("{{ x[" + variableName + "] }}", NamingConvention);
            ex = Assert.Throws<SyntaxException>(() => template.Render(new RenderParameters(System.Globalization.CultureInfo.InvariantCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new { x = new { x = "" } }),
                ErrorsOutputMode = ErrorsOutputMode.Rethrow,
                SyntaxCompatibilityLevel = SyntaxCompatibility.DotLiquid22
            }));
            Assert.AreEqual(
                expected: string.Format(Liquid.ResourceManager.GetString("VariableNotTerminatedException"), variableName),
                actual: ex.Message);
        }

        [Test]
        public void TestNestedVariableNotTerminated()
        {
            var template = Template.Parse("{{ x[[] }}", NamingConvention);
            var ex = Assert.Throws<SyntaxException>(() => template.Render(new RenderParameters(System.Globalization.CultureInfo.InvariantCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new { x = new { x = "" } }),
                ErrorsOutputMode = ErrorsOutputMode.Rethrow,
                SyntaxCompatibilityLevel = SyntaxCompatibility.DotLiquid22
            }));
            Assert.AreEqual(
                expected: string.Format(Liquid.ResourceManager.GetString("VariableNotTerminatedException"), "["),
                actual: ex.Message);
        }

        [TestCase("[\"]")]
        [TestCase("[\"\"")]
        [TestCase("[']")]
        public void TestVariableTokenizerNotTerminated(string variableName)
        {
            var ex = Assert.Throws<SyntaxException>(() => Tokenizer.GetVariableEnumerator(variableName).MoveNext());
            Assert.AreEqual(
                expected: string.Format(Liquid.ResourceManager.GetString("VariableNotTerminatedException"), variableName),
                actual: ex.Message);
        }

        [Test]
        public void TestShortHandSyntaxIsIgnored()
        {
            // These tests are based on actual handling on Ruby Liquid, not indicative of wanted behavior. Behavior for legacy dotliquid parser is in TestEmptyLiteral
            Assert.AreEqual("}", Template.Parse("{{{}}}", NamingConvention, SyntaxCompatibility.DotLiquid22).Render());
            Assert.AreEqual("{##}", Template.Parse("{##}", NamingConvention, SyntaxCompatibility.DotLiquid22).Render());
        }
    }
}
