using Genesys.UI.Controls;
using System;
using System.Data;
using System.Data.SqlClient;

namespace Genesys.UI.Data
{
    public class StoredProcedureLookupProvider : ILookupProvider
    {
        private readonly StoredProcedureDataService dataService;

        public string ParametroValor { get; set; }

        public StoredProcedureLookupProvider()
        {
            dataService = new StoredProcedureDataService();
        }

        public DataTable Search()
        {
            return dataService.ExecuteDataTable("uspDataTables", parameters =>
            {
                parameters.Add("@Titulo", SqlDbType.VarChar).Value = ParametroValor;
                parameters.Add("@Filtro", SqlDbType.VarChar).Value = DBNull.Value;
            });
        }

        public LookupResult GetByValue(string value)
        {
            var dt = dataService.ExecuteDataTable("uspDataTables", parameters =>
            {
                parameters.Add("@Titulo", SqlDbType.VarChar).Value = ParametroValor;
                parameters.Add("@Filtro", SqlDbType.VarChar).Value = value?.Trim();
            });

            if (dt.Rows.Count == 0)
                return null;

            if (dt.Rows.Count > 1)
                throw new InvalidOperationException(
                    $"El S.P. (uspDataTables) con @Titulo ({ParametroValor}) regresa multiples registros ({dt.Rows.Count}) para el valor '{value}'.");

            var row = dt.Rows[0];

            return new LookupResult
            {
                Value = row[0]?.ToString(),
                Description = row.Table.Columns.Count > 1
                    ? row[1]?.ToString()
                    : row[0]?.ToString(),
                Data = row
            };
        }
    }
}