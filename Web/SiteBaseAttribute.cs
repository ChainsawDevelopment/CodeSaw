using System.Reflection;
using Autofac;
using Autofac.Features.AttributeFilters;

namespace Web
{
    public class SiteBaseAttribute : ParameterFilterAttribute
    {
        public override object ResolveParameter(ParameterInfo parameter, IComponentContext context)
        {
            return context.ResolveKeyed<string>("SiteBase");
        }
    }
}