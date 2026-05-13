using Genesys.Framework;
using Genesys.UI.Controls;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Genesys.UI.Data
{
    public class StoredProcedureLookupProvider : ILookupProvider
    {
        private string ConnectionString => AppConfig.ConnectionString;

        public string ParametroValor { get; set; } // "Clientes", "Turnos", etc.

        public DataTable Search()
        {
            var dt = new DataTable();

            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("uspDataTables", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Titulo", SqlDbType.VarChar).Value = ParametroValor;
                cmd.Parameters.Add("@Filtro", SqlDbType.VarChar).Value = DBNull.Value; // 🔥 sin filtro
                cn.Open();
                dt.Load(cmd.ExecuteReader());
            }

            return dt;
        }

        public LookupResult GetByValue(string value)
        {
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("uspDataTables", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Titulo", SqlDbType.VarChar).Value = ParametroValor;
                cmd.Parameters.Add("@Filtro", SqlDbType.VarChar).Value = value?.Trim();
                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    var dt = new DataTable();
                    dt.Load(dr);

                    if (dt.Rows.Count == 0) return null;

                    // Exception por si llega a ver mas de un registro lo que significa que el Stored Procedure no filtro correctamente un solo registro.
                    if (dt.Rows.Count > 1)
                        throw new InvalidOperationException($"El S.P. (uspDataTables) con @Titulo ({ParametroValor}) regresa multiples registros ({dt.Rows.Count}) para el valor '{value}'.");

                    var row = dt.Rows[0];

                    return new LookupResult
                    {
                        Value = row[0]?.ToString(),
                        Description = row.Table.Columns.Count > 1 ? row[1]?.ToString() : row[0]?.ToString(),    // Si el row solo tiene una columna usa esa columna para la descripción
                        Data = row
                    };
                }
            }
        }
    }
}