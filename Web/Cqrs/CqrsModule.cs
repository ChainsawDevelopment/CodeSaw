using Autofac;

namespace Web.Cqrs
{
    public class CqrsModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommandDispatcher>().As<ICommandDispatcher>();

            builder.RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(typeof(CommandHandler<>));

            builder.RegisterType<QueryRunner>().As<IQueryRunner>();
        }
    }
}