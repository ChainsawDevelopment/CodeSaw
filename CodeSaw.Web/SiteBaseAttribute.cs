using System.Reflection;
using Autofac;
using Autofac.Features.AttributeFilters;

namespace CodeSaw.Web
{
    public class SiteBaseAttribute : ParameterFilterAttribute
    {
        public override object ResolveParameter(ParameterInfo parameter, IComponentContext context)
        {
            return context.ResolveKeyed<string>("SiteBase");
        }
    }
    
    public class HookSiteBaseAttribute : ParameterFilterAttribute
    {
        public override object ResolveParameter(ParameterInfo parameter, IComponentContext context)
        {
            return context.ResolveKeyed<string>("HookSiteBase");
        }
    }
}