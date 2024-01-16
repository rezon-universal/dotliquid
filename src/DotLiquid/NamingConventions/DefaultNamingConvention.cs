namespace DotLiquid.NamingConventions
{
    public static class DefaultNamingConvention
    {
        public static INamingConvention GetDefaultNamingConvention()
        {
            return new RubyNamingConvention();
        }
    }
}
