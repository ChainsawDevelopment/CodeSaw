using System;
using System.Threading.Tasks;
using Autofac;
using NHibernate;

namespace Web.Cqrs
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
                Action<ContainerBuilder> enhanceScope = b => { b.RegisterInstance(session); };

                using (var scope = _lifetimeScope.BeginLifetimeScope(enhanceScope))
                using (var tx = session.BeginTransaction())
                {
                    var commandHandler = scope.Resolve<CommandHandler<T>>();
                    
                    await commandHandler.Handle(command);
                    
                    await tx.CommitAsync();
                }
            }
        }
    }
}