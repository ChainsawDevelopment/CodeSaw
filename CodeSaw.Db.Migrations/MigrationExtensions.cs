using System;
using System.Collections.Generic;
using System.Data;
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

        public static IDataReader ExecuteQuery(this IDbTransaction transaction, string sql)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            return command.ExecuteReader();
        }

        public static List<T> ToList<T>(this IDataReader reader, Func<IDataReader, T> projection)
        {
            var result = new List<T>();

            while (reader.Read())
            {
                result.Add(projection(reader));
            }

            reader.Dispose();

            return result;
        }

        public static IDbCommand CreateCommand(this IDbTransaction transaction, string sql)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = sql;

            return command;
        }

        public static void CreateParameter(this IDbCommand cmd, string name, DbType type)
        {
            var parameter = cmd.CreateParameter();
            parameter.DbType = type;
            parameter.ParameterName = name;
            cmd.Parameters.Add(parameter);
        }

        public static void CreateParameter(this IDbCommand cmd, string name, DbType type, object value)
        {
            var parameter = cmd.CreateParameter();
            parameter.DbType = type;
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(parameter);
        }
    }
}