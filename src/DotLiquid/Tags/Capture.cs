using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid.Exceptions;
using DotLiquid.NamingConventions;
using DotLiquid.Util;

namespace DotLiquid.Tags
{
    /// <summary>
    /// Capture stores the result of a block into a variable without rendering it inplace.
    ///
    /// {% capture heading %}
    /// Monkeys!
    /// {% endcapture %}
    /// ...
    /// <h1>{{ heading }}</h1>
    ///
    /// Capture is useful for saving content for use later in your template, such as
    /// in a sidebar or footer.
    /// </summary>
    public class Capture : DotLiquid.Block
    {
        private string _to;

        public override void Initialize(string tagName, string markup, List<string> tokens, INamingConvention namingConvention)
        {
            Match syntaxMatch = Liquid.VariableSegmentRegex.Match(markup);
            if (syntaxMatch.Success)
                _to = syntaxMatch.Groups["Variable"].Value;
            else
                throw new SyntaxException(Liquid.ResourceManager.GetString("CaptureTagSyntaxException"));

            base.Initialize(tagName, markup, tokens, namingConvention);
        }

        public override void Render(Context context, TextWriter result)
        {
            using (TextWriter temp = new StringWriter(result.FormatProvider))
            {
                base.Render(context, temp);
                context.Scopes.Last()[_to] = temp.ToString();
            }
        }
    }
}
