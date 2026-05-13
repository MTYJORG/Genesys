using System;
using System.Collections.Generic;
using System.Data;

namespace Genesys.UI.Forms
{
    //public static class AppConfiguracion
    //{
    //    public static string CadenaConexion { get; set; }
    //}

    public class ConfiguracionColumna
    {
        public string NombreCampo { get; set; }
        public string TextoEncabezado { get; set; }
        public int? Ancho { get; set; }
        public bool Visible { get; set; } = true;
    }

    public class ParametroDatos
    {
        public string Nombre { get; set; }
        public object Valor { get; set; }
        public ParametroDatos(string nombre, object valor)
        {
            Nombre = nombre;
            Valor = valor ?? DBNull.Value;
        }
    }

    public class ConfiguracionVista
    {
        public string Nombre { get; set; }
        public DateTime FechaCreacion { get; set; }
        public Dictionary<string, double> AnchosColumnas { get; set; } = new Dictionary<string, double>();
        public List<string> OrdenColumnas { get; set; } = new List<string>();
        public List<string> ColumnasOcultas { get; set; } = new List<string>();
    }
}