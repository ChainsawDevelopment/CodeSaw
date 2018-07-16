using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_07_09_20_00)]
    public class Migration_2018_07_09_20_00_RemoveChangeKeyFromComment : Migration
    {
        public override void Up()
        {
            Delete.Column("FilePath").FromTable("Comments").InSchema("dbo");
            Delete.Column("ChangeKey").FromTable("Comments").InSchema("dbo");
        }

        public override void Down()
        {
            Create.Column("ChangeKey").OnTable("Comments").InSchema("dbo").AsString().Nullable();
            Create.Column("FilePath").OnTable("Comments").InSchema("dbo").AsString().Nullable();
        }
    }
}