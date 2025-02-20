using System;
using DotLiquid.Exceptions;
using DotLiquid.FileSystems;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using DotLiquid.NamingConventions;
using DotLiquid.Tests.Util;

namespace DotLiquid.Tests.Tags
{
    [TestFixture]
    public class IncludeTagTests
    {
        private static INamingConvention NamingConvention { get; } = TestsDefaultNamingConvention.GetDefaultNamingConvention();

        private class TestFileSystem : IFileSystem
        {
            public string ReadTemplateFile(Context context, string templateName)
            {
                string templatePath = (string) context[templateName];

                switch (templatePath)
                {
                    case "product":
                        return "Product: {{ product.title }} ";

                    case "locale_variables":
                        return "Locale: {{echo1}} {{echo2}}";

                    case "variant":
                        return "Variant: {{ variant.title }}";

                    case "nested_template":
                        return "{% include 'header' %} {% include 'body' %} {% include 'footer' %}";

                    case "body":
                        return "body {% include 'body_detail' %}";

                    case "nested_product_template":
                        return "Product: {{ nested_product_template.title }} {%include 'details'%} ";

                    case "recursively_nested_template":
                        return "-{% include 'recursively_nested_template' %}";

                    case "pick_a_source":
                        return "from TestFileSystem";

                    default:
                        return templatePath;
                }
            }
        }

        internal class TestTemplateFileSystem : ITemplateFileSystem
        {
            private IDictionary<string, Template> _templateCache = new Dictionary<string, Template>();
            private IFileSystem _baseFileSystem = null;
            private int _cacheHitTimes;
            public int CacheHitTimes { get { return _cacheHitTimes; } }

            public TestTemplateFileSystem(IFileSystem baseFileSystem)
            {
                _baseFileSystem = baseFileSystem;
            }

            public string ReadTemplateFile(Context context, string templateName)
            {
                return _baseFileSystem.ReadTemplateFile(context, templateName);
            }

            public Template GetTemplate(Context context, string templateName)
            {
                Template template;
                if (_templateCache.TryGetValue(templateName, out template))
                {
                    ++_cacheHitTimes;
                    return template;
                }
                var result = ReadTemplateFile(context, templateName);
                template = Template.Parse(result, NamingConvention);
                _templateCache[templateName] = template;
                return template;
            }
        }

        private class OtherFileSystem : IFileSystem
        {
            public string ReadTemplateFile(Context context, string templateName)
            {
                return "from OtherFileSystem";
            }
        }

        private class InfiniteFileSystem : IFileSystem
        {
            public string ReadTemplateFile(Context context, string templateName)
            {
                return "-{% include 'loop' %}";
            }
        }

        [SetUp]
        public void SetUp()
        {
            Template.FileSystem = new TestFileSystem();
        }

        [Test]
        public void TestIncludeTagMustNotBeConsideredError()
        {
            Assert.AreEqual(0, Template.Parse("{% include 'product_template' %}", NamingConvention).Errors.Count);
            Assert.DoesNotThrow(() => Template.Parse("{% include 'product_template' %}", NamingConvention).Render(new RenderParameters(CultureInfo.InvariantCulture) { RethrowErrors = true }));
        }

        [Test]
        public void TestIncludeTagLooksForFileSystemInRegistersFirst()
        {
            Assert.AreEqual("from OtherFileSystem", Template.Parse("{% include 'pick_a_source' %}", NamingConvention).Render(new RenderParameters(CultureInfo.InvariantCulture) { Registers = Hash.FromAnonymousObject(new { file_system = new OtherFileSystem() }) }));
        }

        [Test]
        public void TestIncludeTagWith()
        {
            Assert.AreEqual("Product: Draft 151cm ", Template.Parse("{% include 'product' with products[0] %}", NamingConvention).Render(Hash.FromAnonymousObject(new { products = new[] { Hash.FromAnonymousObject(new { title = "Draft 151cm" }), Hash.FromAnonymousObject(new { title = "Element 155cm" }) } })));
        }

        [Test]
        public void TestIncludeTagWithDefaultName()
        {
            Assert.AreEqual("Product: Draft 151cm ", Template.Parse("{% include 'product' %}", NamingConvention).Render(Hash.FromAnonymousObject(new { product = Hash.FromAnonymousObject(new { title = "Draft 151cm" }) })));
        }

        [Test]
        public void TestIncludeTagFor()
        {
            Assert.AreEqual("Product: Draft 151cm Product: Element 155cm ", Template.Parse("{% include 'product' for products %}", NamingConvention).Render(Hash.FromAnonymousObject(new { products = new[] { Hash.FromAnonymousObject(new { title = "Draft 151cm" }), Hash.FromAnonymousObject(new { title = "Element 155cm" }) } })));
        }

        [Test]
        public void TestIncludeTagWithLocalVariables()
        {
            Assert.AreEqual("Locale: test123 ", Template.Parse("{% include 'locale_variables' echo1: 'test123' %}", NamingConvention).Render());
        }

        [Test]
        public void TestIncludeTagWithMultipleLocalVariables()
        {
            Assert.AreEqual("Locale: test123 test321", Template.Parse("{% include 'locale_variables' echo1: 'test123', echo2: 'test321' %}", NamingConvention).Render());
        }

        [Test]
        public void TestIncludeTagWithMultipleLocalVariablesFromContext()
        {
            Assert.AreEqual("Locale: test123 test321",
                Template.Parse("{% include 'locale_variables' echo1: echo1, echo2: more_echos.echo2 %}", NamingConvention).Render(Hash.FromAnonymousObject(new { echo1 = "test123", more_echos = Hash.FromAnonymousObject(new { echo2 = "test321" }) })));
        }

        [Test]
        public void TestNestedIncludeTag()
        {
            Assert.AreEqual("body body_detail", Template.Parse("{% include 'body' %}", NamingConvention).Render());

            Assert.AreEqual("header body body_detail footer", Template.Parse("{% include 'nested_template' %}", NamingConvention).Render());
        }

        [Test]
        public void TestNestedIncludeTagWithVariable()
        {
            Assert.AreEqual("Product: Draft 151cm details ",
                Template.Parse("{% include 'nested_product_template' with product %}", NamingConvention).Render(Hash.FromAnonymousObject(new { product = Hash.FromAnonymousObject(new { title = "Draft 151cm" }) })));

            Assert.AreEqual("Product: Draft 151cm details Product: Element 155cm details ",
                Template.Parse("{% include 'nested_product_template' for products %}", NamingConvention).Render(Hash.FromAnonymousObject(new { products = new[] { Hash.FromAnonymousObject(new { title = "Draft 151cm" }), Hash.FromAnonymousObject(new { title = "Element 155cm" }) } })));
        }

        [Test]
        public void TestRecursivelyIncludedTemplateDoesNotProductEndlessLoop()
        {
            Template.FileSystem = new InfiniteFileSystem();

            Assert.Throws<StackLevelException>(() => Template.Parse("{% include 'loop' %}", NamingConvention).Render(new RenderParameters(CultureInfo.InvariantCulture) { RethrowErrors = true }));
        }

        [Test]
        public void TestDynamicallyChosenTemplate()
        {
            Assert.AreEqual("Test123", Template.Parse("{% include template %}", NamingConvention).Render(Hash.FromAnonymousObject(new { template = "Test123" })));
            Assert.AreEqual("Test321", Template.Parse("{% include template %}", NamingConvention).Render(Hash.FromAnonymousObject(new { template = "Test321" })));

            Assert.AreEqual("Product: Draft 151cm ", Template.Parse("{% include template for product %}", NamingConvention).Render(Hash.FromAnonymousObject(new { template = "product", product = Hash.FromAnonymousObject(new { title = "Draft 151cm" }) })));
        }

        [Test]
        public void TestUndefinedTemplateVariableWithTestFileSystem()
        {
            Assert.AreEqual(" hello  world ", Template.Parse(" hello {% include notthere %} world ", NamingConvention).Render());
        }

        [Test]
        public void TestUndefinedTemplateVariableWithLocalFileSystem()
        {
            Template.FileSystem = new LocalFileSystem(string.Empty);
            Assert.Throws<FileSystemException>(() => Template.Parse(" hello {% include notthere %} world ", NamingConvention).Render(new RenderParameters(CultureInfo.InvariantCulture)
            {
                RethrowErrors = true
            }));
        }

        [Test]
        public void TestMissingTemplateWithLocalFileSystem()
        {
            Template.FileSystem = new LocalFileSystem(string.Empty);
            Assert.Throws<FileSystemException>(() => Template.Parse(" hello {% include 'doesnotexist' %} world ", NamingConvention).Render(new RenderParameters(CultureInfo.InvariantCulture)
            {
                RethrowErrors = true
            }));
        }

        [Test]
        public void TestIncludeFromTemplateFileSystem()
        {
            var fileSystem = new TestTemplateFileSystem(new TestFileSystem());
            Template.FileSystem = fileSystem;
            for (int i = 0; i < 2; ++i)
            {
                Assert.AreEqual("Product: Draft 151cm ", Template.Parse("{% include 'product' with products[0] %}", NamingConvention).Render(Hash.FromAnonymousObject(new { products = new[] { Hash.FromAnonymousObject(new { title = "Draft 151cm" }), Hash.FromAnonymousObject(new { title = "Element 155cm" }) } })));
            }
            Assert.AreEqual(fileSystem.CacheHitTimes, 1);
        }
    }
}
