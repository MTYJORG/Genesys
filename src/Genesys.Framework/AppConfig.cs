using System;
using System.Configuration;
using System.Runtime;

namespace Genesys.Framework
{
    public static class AppConfig
    {
        public static string ConnectionString { get; private set; }

        public static void Initialize()
        {
            string sModoProducción = ConfigurationManager.AppSettings["ModoProducción"];

            if (sModoProducción == "Si")
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["Contpaqi"].ConnectionString;

                if (string.IsNullOrWhiteSpace(ConnectionString))
                {
                    throw new Exception("No se encontró el connection string 'Contpaqi'.");
                }
            }
            else 
            {
                ConnectionString = "Data Source=" + Environment.MachineName + "\\SQLEXPRESS;" + "Initial Catalog=adJuguera_Allende_SA;" + "User Id=sa;" + "Password=Pa$$w0rd;";
            }
        }
    }
}
