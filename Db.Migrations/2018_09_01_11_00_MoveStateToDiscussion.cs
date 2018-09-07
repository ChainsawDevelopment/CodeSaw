using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_09_01_11_00)]
    public class Migration_2018_09_01_11_00_MoveStateToDiscussion : Migration
    {
        public override void Up()
        {
            Up("FileDiscussions");
            Up("ReviewDiscussions");

            Delete.Column("State").FromTable("Comments").InSchema("dbo");
        }

        private void Up(string tableName)
        {
            Alter.Table(tableName)
                .InSchema("dbo")
                .AddColumn("State").AsString(255).Nullable();

            Execute.Sql($"UPDATE dbo.{tableName} SET State = (SELECT State from dbo.Comments c where c.Id = RootCommentId)");

            Alter.Table(tableName)
                .InSchema("dbo")
                .AlterColumn("State").AsString(255).NotNullable();
        }

        public override void Down()
        {
            Alter.Table("Comments").InSchema("dbo")
                .AddColumn("State").AsString(255).WithDefaultValue("NoActionNeeded").NotNullable();

            Down("FileDiscussions");
            Down("ReviewDiscussions");
        }

        private void Down(string tableName)
        {
            Execute.Sql($"UPDATE c SET c.State = d.State FROM dbo.Comments c JOIN dbo.{tableName} d on d.RootCommentId = c.Id");

            Delete.Column("State")
                .FromTable(tableName).InSchema("dbo");
        }
    }
}