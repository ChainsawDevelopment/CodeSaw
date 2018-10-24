using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2018_10_24_19_00)]
    public class Migration_2018_10_24_19_00_RemoveCommentLengthLimit: Migration

    {
        public override void Up()
        {
            Alter.Column("Content").OnTable("Comments").InSchema("dbo").AsMaxString();
        }

        public override void Down()
        {
            Alter.Column("Content").OnTable("Comments").InSchema("dbo").AsString(255);
        }
    }
}