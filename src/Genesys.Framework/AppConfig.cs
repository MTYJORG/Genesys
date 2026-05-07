using System;
using System.Configuration;

namespace Genesys.Framework
{
    public static class AppConfig
    {
        public static string ConnectionString { get; private set; }

        public static void Initialize()
        {
            ConnectionString =
                ConfigurationManager
                    .ConnectionStrings["MainDb"]
                    .ConnectionString;

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new Exception(
                    "No se encontró el connection string 'MainDb'.");
            }
        }
    }
}
