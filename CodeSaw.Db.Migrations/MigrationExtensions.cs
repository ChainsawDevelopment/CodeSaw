using FluentMigrator.Builders.Alter.Column;
using FluentMigrator.Builders.Create.Table;

namespace CodeSaw.Db.Migrations
{
    internal static class MigrationExtensions
    {
        public static ICreateTableColumnOptionOrWithColumnSyntax AsMaxString(this ICreateTableColumnAsTypeSyntax column)
        {
            return column.AsString(int.MaxValue);
        }

        public static IAlterColumnOptionSyntax AsMaxString(this IAlterColumnAsTypeSyntax column)
        {
            return column.AsString(int.MaxValue);
        }
    }
}