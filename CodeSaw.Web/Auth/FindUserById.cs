﻿using System.Threading.Tasks;
using CodeSaw.Web.Cqrs;
using NHibernate;

namespace CodeSaw.Web.Auth
{
    public class FindUserById : IQuery<ReviewUser>
    {
        public string UserId { get; }

        public FindUserById(string userId)
        {
            UserId = userId;
        }

        public class Handler : IQueryHandler<FindUserById, ReviewUser>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public Task<ReviewUser> Execute(FindUserById query)
            {
                return _session.GetAsync<ReviewUser>(query.UserId);
            }
        }
    }
}