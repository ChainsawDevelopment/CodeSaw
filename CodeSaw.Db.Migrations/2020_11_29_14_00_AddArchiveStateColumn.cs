using System.Data;
using FluentMigrator;

namespace CodeSaw.Db.Migrations
{
    [Migration(2020_11_29_14_00)]
    public class Migration_2020_11_29_14_00_AddArchiveStateColumn_AddArchiveStateColumn : Migration
    {
        public override void Up()
        {
             Alter.Table("Revisions").InSchema("dbo")
                .AddColumn("ArchiveState").AsInt32().WithDefaultValue(0);
        }

        public override void Down()
        {
            Delete.Column("ArchiveState").FromTable("Revisions").InSchema("dbo");
        }
    }
}
