using System;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class SAPConnection : IDisposable
    {
        internal const string SqlServerName = "SERVIDORSAP";
        internal const string SqlUserName = "intg";
        internal const string SqlPassword = "Horiz0nt3s";

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
            company.Server = SqlServerName;
            company.LicenseServer = SqlServerName + ":30000";
            company.CompanyDB = "TEST_VINESA";
            company.DbServerType = BoDataServerTypes.dst_MSSQL2016;

            // CAMBIAR DESPUES, NO OLVIDAR
            company.UserName = "auditori";
            company.Password = "1234";
            company.DbUserName = SqlUserName;
            company.DbPassword = SqlPassword;

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