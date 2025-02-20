using System;
using System.Globalization;
using DotLiquid.NamingConventions;
using DotLiquid.Tests.Util;
using NUnit.Framework;

namespace DotLiquid.Tests
{
    [TestFixture]
    public class VariableResolutionTests
    {
        private INamingConvention NamingConvention { get; } = TestsDefaultNamingConvention.GetDefaultNamingConvention();
        [Test]
        public void TestSimpleVariable()
        {
            Template template = Template.Parse("{{test}}", NamingConvention);
            Assert.AreEqual("worked", template.Render(Hash.FromAnonymousObject(new { test = "worked" })));
            Assert.AreEqual("worked wonderfully", template.Render(Hash.FromAnonymousObject(new { test = "worked wonderfully" })));
        }

        [Test]
        public void TestSimpleWithWhitespaces()
        {
            Template template = Template.Parse("  {{ test }}  ", NamingConvention);
            Assert.AreEqual("  worked  ", template.Render(Hash.FromAnonymousObject(new { test = "worked" })));
            Assert.AreEqual("  worked wonderfully  ", template.Render(Hash.FromAnonymousObject(new { test = "worked wonderfully" })));
        }

        [Test]
        public void TestIgnoreUnknown()
        {
            Template template = Template.Parse("{{ test }}", NamingConvention);
            Assert.AreEqual("", template.Render());
        }

        [Test]
        public void TestHashScoping()
        {
            Template template = Template.Parse("{{ test.test }}", NamingConvention);
            Assert.AreEqual("worked", template.Render(Hash.FromAnonymousObject(new { test = new { test = "worked" } })));
        }

        [Test]
        public void TestPresetAssigns()
        {
            Template template = Template.Parse("{{ test }}", NamingConvention);
            template.Assigns["test"] = "worked";
            Assert.AreEqual("worked", template.Render());
        }

        [Test]
        public void TestReuseParsedTemplate()
        {
            Template template = Template.Parse("{{ greeting }} {{ name }}", NamingConvention);
            template.Assigns["greeting"] = "Goodbye";
            Assert.AreEqual("Hello Tobi", template.Render(Hash.FromAnonymousObject(new { greeting = "Hello", name = "Tobi" })));
            Assert.AreEqual("Hello ", template.Render(Hash.FromAnonymousObject(new { greeting = "Hello", unknown = "Tobi" })));
            Assert.AreEqual("Hello Brian", template.Render(Hash.FromAnonymousObject(new { greeting = "Hello", name = "Brian" })));
            Assert.AreEqual("Goodbye Brian", template.Render(Hash.FromAnonymousObject(new { name = "Brian" })));
            CollectionAssert.AreEqual(Hash.FromAnonymousObject(new { greeting = "Goodbye" }), template.Assigns);
        }

        [Test]
        public void TestAssignsNotPollutedFromTemplate()
        {
            Template template = Template.Parse("{{ test }}{% assign test = 'bar' %}{{ test }}", NamingConvention);
            template.Assigns["test"] = "baz";
            Assert.AreEqual("bazbar", template.Render());
            Assert.AreEqual("bazbar", template.Render());
            Assert.AreEqual("foobar", template.Render(Hash.FromAnonymousObject(new { test = "foo" })));
            Assert.AreEqual("bazbar", template.Render());
        }

        [Test]
        public void TestHashWithDefaultProc()
        {
            Template template = Template.Parse("Hello {{ test }}", NamingConvention);
            Hash assigns = new Hash((h, k) => { throw new Exception("Unknown variable '" + k + "'"); }, NamingConvention);
            assigns["test"] = "Tobi";
            Assert.AreEqual("Hello Tobi", template.Render(new RenderParameters(CultureInfo.InvariantCulture)
            {
                LocalVariables = assigns,
                RethrowErrors = true
            }));
            assigns.Remove("test");
            Exception ex = Assert.Throws<Exception>(() => template.Render(new RenderParameters(CultureInfo.InvariantCulture)
            {
                LocalVariables = assigns,
                RethrowErrors = true
            }));
            Assert.AreEqual("Unknown variable 'test'", ex.Message);
        }
    }
}
