using System;
using SAPbobsCOM;

namespace ConsoleApp2.IntgSAPLibs
{
    internal sealed class SAPConnection : IDisposable
    {
        public Company Company { get; }

        public SAPConnection()
        {
            Company = new Company();
            ConfigureConnection(Company);
            Connect(Company);
            FileLogger.Info("Conexión SAP establecida correctamente.");
        }

        private static void ConfigureConnection(Company company)
        {
            company.Server = "SERVIDORSAP";
            company.LicenseServer = "SERVIDORSAP:30000";
            company.CompanyDB = "TEST_VINESA";
            company.DbServerType = BoDataServerTypes.dst_MSSQL2016;

            // CAMBIAR DESPUES, NO OLVIDAR
            company.UserName = "auditori";
            company.Password = "1234";
            company.DbUserName = "intg";
            company.DbPassword = "Horiz0nt3s";

            company.UseTrusted = false;
        }

        private static void Connect(Company company)
        {
            FileLogger.Info("Intentando conectar a SAP.");
            int connectionResult = company.Connect();
            if (connectionResult != 0)
            {
                throw new InvalidOperationException("Conexión Errónea: " + company.GetLastErrorDescription());
            }
        }

        public void Dispose()
        {
            if (Company != null && Company.Connected)
            {
                FileLogger.Info("Desconectando sesión SAP.");
                Company.Disconnect();
            }
        }
    }
}