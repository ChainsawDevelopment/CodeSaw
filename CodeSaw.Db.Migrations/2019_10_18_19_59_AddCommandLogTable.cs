using System.Data;
using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2019_10_18_19_59)]
    public class Migration_2019_10_18_19_59_AddCommandLogTable : Migration
    {
        public override void Up()
        {
            Create.Table("CommandLog").InSchema("dbo")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("ExecutedAt").AsDateTimeOffset().Nullable()
                .WithColumn("UserName").AsString(200).Nullable()
                .WithColumn("Url").AsString().Nullable()
                .WithColumn("ProjectId").AsInt32().Nullable()
                .WithColumn("ReviewId").AsInt32().Nullable()
                .WithColumn("CommandType").AsMaxString().Nullable()
                .WithColumn("Command").AsMaxString().Nullable();
        }

        public override void Down()
        {
            Delete.Table("CommandLog").InSchema("dbo");
        }
    }
}
