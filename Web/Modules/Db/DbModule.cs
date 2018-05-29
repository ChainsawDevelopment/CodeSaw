using System.Collections.Generic;
using System.Threading.Tasks;
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
            Get("/insert/{text}", async _ =>
            {
                await commands.Execute(new InsertRecord(_.Text));
                return new {ok = true};
            });
            Get("/list", async _ => new
            {
                list = await queries.Query(new ListEntities())
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

            public override async Task Handle(InsertRecord command)
            {
                await _session.SaveAsync(new TestEntity()
                {
                    Text = command.Text
                });
            }
        }
    }

    public class ListEntities : IQuery<IList<string>>
    {
        public async Task<IList<string>> Execute(ISession session)
        {
            return await session.QueryOver<TestEntity>()
                .Select(t => t.Text)
                .ListAsync<string>();
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