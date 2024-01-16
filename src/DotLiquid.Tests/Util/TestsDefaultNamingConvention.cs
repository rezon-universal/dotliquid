using DotLiquid.NamingConventions;

namespace DotLiquid.Tests.Util
{
    internal class TestsDefaultNamingConvention
    {
        public static INamingConvention GetDefaultNamingConvention()
        {
            return new RubyNamingConvention();
        }
    }
}
