using Genesys.Framework;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Genesys.UI.Data
{
    public class StoredProcedureDataService
    {
        private string ConnectionString => AppConfig.ConnectionString;

        public DataTable ExecuteDataTable(string storedProcedureName, Action<SqlParameterCollection> configureParameters)
        {
            var dt = new DataTable();

            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(storedProcedureName, cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                configureParameters?.Invoke(cmd.Parameters);

                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    dt.Load(dr);
                }
            }

            return dt;
        }

        public DataSet ExecuteDataSet(string storedProcedureName, Action<SqlParameterCollection> configureParameters)
        {
            var ds = new DataSet();

            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(storedProcedureName, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                configureParameters?.Invoke(cmd.Parameters);

                da.Fill(ds);
            }

            return ds;
        }
    }
}