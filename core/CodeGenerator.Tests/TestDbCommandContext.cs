﻿using System;
using System.Data;
using System.Data.Common;
using System.IO;
using SqlProcBinder;

namespace CodeGenerator.Tests
{
    public class TestDbCommandContext : IDbCommandContext
    {
        private DbConnection _connection;
        private DbCommand _command;

        public DbCommand Command => _command;

        public TestDbCommandContext(DbConnection connection, DbCommand command)
        {
            _connection = connection;
            _command = command;
        }

        public void Dispose()
        {
            if (_command != null)
            {
                _command.Dispose();
                _command = null;
            }
        }

        public void OnExecuting()
        {
            if (_command.CommandType == CommandType.StoredProcedure)
            {
                // Whenever stored-procedure is being executed,
                // create this procedure on test database.

                var dir = AppDomain.CurrentDomain.BaseDirectory;
                var idx = dir.LastIndexOf("CodeGenerator.Tests");
                if (idx != -1)
                    dir = Path.Combine(dir.Substring(0, idx + 19), "Sql");

                var filePath = Path.Combine(dir, _command.CommandText + ".sql");
                if (File.Exists(filePath))
                {
                    if (_command.CommandText.StartsWith("Vector3List"))
                        EnsureTableType("Vector3List", Path.Combine(dir, "Vector3List.sql"));

                    RecreateStoredProcedure(_command.CommandText, filePath);
                }
            }
        }

        private void EnsureTableType(string typeName, string filePath)
        {
            var checkCommand = _connection.CreateCommand();
            checkCommand.CommandText = $"SELECT COUNT(*) FROM sys.table_types WHERE name = '{typeName}'";
            var count = (int)checkCommand.ExecuteScalar();
            if (count <= 0)
            {
                var createCommand = _connection.CreateCommand();
                createCommand.CommandText = File.ReadAllText(filePath);
                createCommand.ExecuteNonQuery();
            }
        }

        private void RecreateStoredProcedure(string procName, string filePath)
        {
            var dropCommand = _connection.CreateCommand();
            dropCommand.CommandText = string.Format(
                "IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = '{0}') DROP PROCEDURE {0};",
                procName);
            dropCommand.ExecuteNonQuery();

            var createCommand = _connection.CreateCommand();
            createCommand.CommandText = File.ReadAllText(filePath);
            createCommand.ExecuteNonQuery();
        }

        public void OnExecuted()
        {
        }
    }
}
