using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using Dapper;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories
{
    public class SqlExecutor : IDisposable
    {

        private readonly string _connectionString;
        private StreamWriter _streamWriter;

        public SqlExecutor(string connectionString)
        {
            _connectionString = connectionString;
            _streamWriter = new StreamWriter(Path.Combine(Settings.LogDirectory, "SqlExecutor.txt"), true);
            _streamWriter.AutoFlush = true;
            _streamWriter.WriteLine($"Log opened {DateTime.Now.ToString()}");
        }

        public void Dispose()
        {
            if (_streamWriter != null)
            {
                _streamWriter.Flush();
                _streamWriter.Close();
            }
        }

        private void WriteLog(string sql)
        {
            _streamWriter.WriteLine(sql);
            _streamWriter.WriteLine();
        }

        public void ExecuteScalar(string sql, object parameters = null)
        {
            WriteLog(sql);
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.ExecuteScalar(sql, parameters, null, null, CommandType.Text);
            }
        }

        public T ExecuteScalar<T>(string sql, object parameters = null)
        {
            WriteLog(sql);
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.ExecuteScalar<T>(sql, parameters, null, null, CommandType.Text);
            }
        }

        public T QueryFirstOrDefault<T>(string sql, object parameters = null)
        {
            WriteLog(sql);
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.QueryFirstOrDefault<T>(sql, parameters);
            }
        }
        public T QueryFirstOrDefaultWithDelay<T>(string sql, object parameters = null, int sleep = 500, int attempts = 6)
        {
            Thread.Sleep(sleep);
            var result = default(T);
            for (var i = 0; i < attempts; i++)
            {
                result = QueryFirstOrDefault<T>(sql, parameters);
                if (result == null)
                {
                    Thread.Sleep(sleep);
                }
            }

            return result;
        }

        public IEnumerable<T> Query<T>(string sql, object parameters = null)
        {
            WriteLog(sql);
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(sql, parameters);
            }
        }

        public IEnumerable<dynamic> Query(string sql, object parameters = null)
        {
            WriteLog(sql);
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(sql, parameters);
            }
        }
    }
}
