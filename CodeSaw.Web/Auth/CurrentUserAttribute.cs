using System.Reflection;
using Autofac;
using Autofac.Features.AttributeFilters;

namespace CodeSaw.Web.Auth
{
    public class CurrentUserAttribute : ParameterFilterAttribute
    {
        public override object ResolveParameter(ParameterInfo parameter, IComponentContext context)
        {
            return context.ResolveKeyed<ReviewUser>("currentUser");
        }
    }
}