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
            DataSet ds = dataService.ExecuteDataSet(
                request.StoredProcedureName,
                delegate (System.Data.SqlClient.SqlParameterCollection parameters)
                {
                    parameters.Add("@TipoDeAccion", SqlDbType.VarChar).Value =
                        string.IsNullOrWhiteSpace(request.TipoDeAccion)
                            ? (object)DBNull.Value
                            : request.TipoDeAccion;

                    parameters.Add("@FechaInicio", SqlDbType.DateTime).Value =
                        request.FechaInicio.HasValue
                            ? (object)request.FechaInicio.Value
                            : DBNull.Value;

                    parameters.Add("@FechaFinal", SqlDbType.DateTime).Value =
                        request.FechaFinal.HasValue
                            ? (object)request.FechaFinal.Value
                            : DBNull.Value;

                    // Aqui se inyecta el nombre de paramentro a filtrar, por ejemplo "@Contrato" que es un paramentro del S.P. "uspCABCContratos"
                    // se envía desde el form hijo
                    if (!string.IsNullOrWhiteSpace(request.LookupParameterName))
                    {
                        parameters.Add(request.LookupParameterName, SqlDbType.VarChar).Value =
                            string.IsNullOrWhiteSpace(request.LookupValue)
                                ? (object)DBNull.Value
                                : request.LookupValue;
                    }

                    // Aqui se inyecta el nombre de paramentro para el combro box, por ejemplo "@Estatus" que es un paramentro del S.P. "uspCABCContratos"
                    // se envía desde el form hijo
                    if (!string.IsNullOrWhiteSpace(request.ComboParameterName))
                    {
                        parameters.Add(request.ComboParameterName, SqlDbType.VarChar).Value =
                            request.ComboValue == null
                                ? (object)DBNull.Value
                                : request.ComboValue;
                    }

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