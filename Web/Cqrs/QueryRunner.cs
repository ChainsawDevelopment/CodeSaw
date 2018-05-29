﻿using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Autofac;
using NHibernate;

namespace Web.Cqrs
{
    public interface IQuery<TResult>
    {
        Task<TResult> Execute(ISession session);
    }

    public interface IQueryRunner
    {
        Task<TResult> Query<TResult>(IQuery<TResult> query);
    }

    public class QueryRunner : IQueryRunner
    {
        private static readonly MethodInfo QueryCoreMethod = typeof(QueryRunner).GetMethod(nameof(QueryCore), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ILifetimeScope _lifetimeScope;
        private readonly ISessionFactory _sessionFactory;

        public QueryRunner(ILifetimeScope lifetimeScope, ISessionFactory sessionFactory)
        {
            _lifetimeScope = lifetimeScope;
            _sessionFactory = sessionFactory;
        }

        public async Task<TResult> Query<TResult>(IQuery<TResult> query)
        {
            try
            {
                return await (Task<TResult>)QueryCoreMethod.MakeGenericMethod(query.GetType(), typeof(TResult)).Invoke(this, new object[] {query});
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        private async Task<TResult> QueryCore<TQuery, TResult>(TQuery query)
            where TQuery : IQuery<TResult>
        {
            var currentSession = _lifetimeScope.ResolveOptional<ISession>();

            if (currentSession != null)
            {
                return await query.Execute(currentSession);
            }

            using (var session = _sessionFactory.OpenSession())
            {
                session.DefaultReadOnly = true;
                session.FlushMode = FlushMode.Manual;
                
                return await query.Execute(session);
            }
        }
    }
}