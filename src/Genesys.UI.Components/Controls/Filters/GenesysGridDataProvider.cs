using Genesys.UI.Data;
using System;
using System.Data;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridDataProvider
    {
        private readonly StoredProcedureDataService dataService;

        public GenesysGridDataProvider()
        {
            dataService = new StoredProcedureDataService();
        }

        public GenesysGridFilterResult Execute(GenesysGridFilterRequest request)
        {
            DataSet ds = dataService.ExecuteDataSet(request.StoredProcedureName, delegate (System.Data.SqlClient.SqlParameterCollection parameters)
            {
                parameters.Add("@FechaInicio", SqlDbType.DateTime).Value =
                    request.FechaInicio.HasValue ? (object)request.FechaInicio.Value : DBNull.Value;

                parameters.Add("@FechaFinal", SqlDbType.DateTime).Value =
                    request.FechaFinal.HasValue ? (object)request.FechaFinal.Value : DBNull.Value;

                parameters.Add("@LookupValue", SqlDbType.VarChar).Value =
                    string.IsNullOrWhiteSpace(request.LookupValue) ? (object)DBNull.Value : request.LookupValue;

                parameters.Add("@ComboValue", SqlDbType.VarChar).Value =
                    request.ComboValue == null ? (object)DBNull.Value : request.ComboValue;

                foreach (var p in request.ExtraParameters)
                {
                    parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                }
            });

            if (ds.Tables.Count == 1)
            {
                return new GenesysGridFilterResult
                {
                    Table = ds.Tables[0]
                };
            }

            return new GenesysGridFilterResult
            {
                DataSet = ds
            };
        }
    }
}