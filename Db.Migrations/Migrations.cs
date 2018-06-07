using System;
using FluentMigrator;

namespace Db.Migrations
{
    [Migration(2018_05_29_15_50)]
    public class AddTestTable : Migration
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

    [Migration(2018_06_01_10_26)]
    public class AddUserTable : Migration
    {
        public override void Up()
        {
            Create
                .Table("Users")
                .InSchema("dbo")
                .WithColumn("Id").AsInt32().NotNullable().PrimaryKey().Identity()
                .WithColumn("UserName").AsString(200).NotNullable();
        }

        public override void Down()
        {
            Delete.Table("Users").InSchema("dbo");
        }
    }

    [Migration(2018_06_01_12_26)]
    public class AddUserToken : Migration
    {
        public override void Up()
        {
            Alter
                .Table("Users")
                .InSchema("dbo")
                .AddColumn("Token").AsString(200).Nullable();
        }

        public override void Down()
        {
            Delete.Column("Token").FromTable("Users").InSchema("dbo");
        }
    }
}
