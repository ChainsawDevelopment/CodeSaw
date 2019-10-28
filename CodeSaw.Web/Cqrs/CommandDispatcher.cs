using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using NHibernate;
using NLog;

namespace CodeSaw.Web.Cqrs
{
    public interface ICommand
    {
    }

    public interface ICommandDispatcher
    {
        Task Execute(ICommand command);
    }

    public abstract class CommandHandler<TCommand>
        where TCommand : ICommand
    {
        public abstract Task Handle(TCommand command);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        public static readonly Logger Log = LogManager.GetLogger("CommandDispatcher.Execute");

        private readonly ILifetimeScope _lifetimeScope;
        private readonly ISessionFactory _sessionFactory;

        public CommandDispatcher(ILifetimeScope lifetimeScope, ISessionFactory sessionFactory)
        {
            _lifetimeScope = lifetimeScope;
            _sessionFactory = sessionFactory;
        }

        public async Task Execute(ICommand command)
        {
            await ((dynamic) this).ExecuteCommand((dynamic) command);
        }

        public async Task ExecuteCommand<T>(T command) where T : ICommand
        {
            using (var session = _sessionFactory.OpenSession())
            {
                Log.Info("Executing command {commandType}: {command}", typeof(T).Name, JsonConvert.SerializeObject(command));

                Action<ContainerBuilder> enhanceScope = b =>
                {
                    b.RegisterInstance(session);
                    b.RegisterType<EventAccumulator>().As<IEventBus>().AsSelf().SingleInstance();
                };

                using (var scope = _lifetimeScope.BeginLifetimeScope(enhanceScope))
                using (var tx = session.BeginTransaction())
                {
                    var commandHandler = scope.Resolve<CommandHandler<T>>();
                    
                    await commandHandler.Handle(command);
                    
                    await session.FlushAsync();

                    await scope.Resolve<EventAccumulator>().Flush();

                    await tx.CommitAsync();
                }
            }
        }
    }
}