using System;
using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_05_29_15_50)]
    public class Class1 : Migration
    {
        public override void Up()
        {
            Create
                .Table("Test")
                .InSchema("dbo")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("Text").AsString(200).NotNullable();
        }

        public override void Down()
        {
            Delete.Table("Test").InSchema("dbo");
        }
    }
}
