using System;
using Autofac;
using NHibernate;

namespace Web.Cqrs
{
    public interface ICommand
    {
    }

    public interface ICommandDispatcher
    {
        void Execute(ICommand command);
    }

    public abstract class CommandHandler<TCommand>
        where TCommand : ICommand
    {
        public abstract void Handle(TCommand command);
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

        public void Execute(ICommand command)
        {
            ((dynamic) this).ExecuteCommand((dynamic) command);
        }

        public void ExecuteCommand<T>(T command) where T : ICommand
        {
            using (var session = _sessionFactory.OpenSession())
            {
                Action<ContainerBuilder> enhanceScope = b => { b.RegisterInstance(session); };

                using (var scope = _lifetimeScope.BeginLifetimeScope(enhanceScope))
                using (var tx = session.BeginTransaction())
                {
                    var commandHandler = scope.Resolve<CommandHandler<T>>();
                    commandHandler.Handle(command);
                    tx.Commit();
                }
            }
        }
    }
}