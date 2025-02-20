using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotLiquid.NamingConventions;

namespace DotLiquid.Util
{
    /// <summary>
    /// Extend System.Reflection.MethodInfo
    /// </summary>
    public static class MethodInfoExtensionMethods
    {
        /// <summary>
        /// Get count of parameters in method that are not the Context
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static int GetNonContextParameterCount(this MethodInfo method)
        {
            return method.GetParameters().Count(parameter => parameter.ParameterType != typeof(Context));
        }
        /// <summary>
        /// Check if current method matches compareMethod in name and in parameters
        /// </summary>
        /// <param name="method"></param>
        /// <param name="compareMethod"></param>
        /// <param name="namingConvention">Naming convention used for template parsing</param>
        /// <returns></returns>
        public static bool MatchesMethod(this MethodInfo method, KeyValuePair<string, IList<Tuple<object, MethodInfo>>> compareMethod, INamingConvention namingConvention)
        {
            if (compareMethod.Key != namingConvention.GetMemberName(method.Name))
            {
                return false;
            }

            var methodParamCount = method.GetNonContextParameterCount();

            return compareMethod.Value.Any(m => m.Item2.GetNonContextParameterCount() == methodParamCount);
        }
    }
}
