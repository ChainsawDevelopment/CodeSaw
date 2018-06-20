using Autofac;
using Autofac.Features.AttributeFilters;

namespace Web.Cqrs
{
    public class CqrsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommandDispatcher>().As<ICommandDispatcher>()
                .WithAttributeFiltering();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(typeof(CommandHandler<>))
                .WithAttributeFiltering();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(typeof(IQueryHandler<,>))
                .WithAttributeFiltering();

            builder.RegisterType<QueryRunner>().As<IQueryRunner>()
                .WithAttributeFiltering();
        }
    }
}