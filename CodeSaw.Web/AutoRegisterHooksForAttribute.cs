using Autofac;
using Autofac.Features.AttributeFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeSaw.Web
{
    public class AutoRegisterHooksForAttribute : ParameterFilterAttribute
    {
        public override object ResolveParameter(ParameterInfo parameter, IComponentContext context)
        {
            return context.ResolveKeyed<string[]>("AutoRegisterHooksFor");
        }
    }
}
