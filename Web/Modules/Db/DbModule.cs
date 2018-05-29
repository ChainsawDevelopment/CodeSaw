using System.Collections.Generic;
using Nancy;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Web.Cqrs;

namespace Web.Modules.Db
{
    public class DbModule : NancyModule
    {
        public DbModule(ICommandDispatcher commands, IQueryRunner queries) : base("/test/db")
        {
            Get("/insert/{text}", _ =>
            {
                commands.Execute(new InsertRecord(_.Text));
                return new {ok = true};
            });
            Get("/list", _ => new
            {
                list = queries.Query(new ListEntities())
            });
        }
    }

    public class InsertRecord : ICommand
    {
        public InsertRecord(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public class Handler : CommandHandler<InsertRecord>
        {
            private readonly ISession _session;

            public Handler(ISession session)
            {
                _session = session;
            }

            public override void Handle(InsertRecord command)
            {
                _session.Save(new TestEntity()
                {
                    Text = command.Text
                });
            }
        }
    }

    public class ListEntities : IQuery<IList<string>>
    {
        public IList<string> Execute(ISession session)
        {
            return session.QueryOver<TestEntity>()
                .Select(t => t.Text)
                .List<string>();
        }
    }

    public class TestEntity
    {
        public virtual int Id { get; set; }
        public virtual string Text { get; set; }
    }

    public class TestEntityMapping : ClassMapping<TestEntity>
    {
        public TestEntityMapping()
        {
            Table("Test");
            Id(x => x.Id, id => id.Generator(Generators.Identity));
            Property(x => x.Text);
        }
    }
}