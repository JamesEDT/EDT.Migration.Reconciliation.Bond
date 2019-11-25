using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using Dapper;

namespace Edt.Bond.Migration.Reconciliation.Framework.Repositories
{
    public class SqlExecutor
    {
        private readonly string _connectionString;
        public SqlExecutor(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void ExecuteScalar(string sql, object parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.ExecuteScalar(sql, parameters, null, null, CommandType.Text);
            }
        }

        public T ExecuteScalar<T>(string sql, object parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.ExecuteScalar<T>(sql, parameters, null, null, CommandType.Text);
            }
        }

        public T QueryFirstOrDefault<T>(string sql, object parameters = null)
        {
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
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(sql, parameters);
            }
        }

        public IEnumerable<dynamic> Query(string sql, object parameters = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(sql, parameters);
            }
        }
    }
}
