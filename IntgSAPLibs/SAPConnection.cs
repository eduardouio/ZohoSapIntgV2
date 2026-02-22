using System;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class SAPConnection : IDisposable
    {
        public Company Company { get; }

        public SAPConnection(IntegrationSettings settings, EnterpriseSapTarget target)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Company = new Company();
            ConfigureConnection(Company, settings, target);
            Connect(Company);
            FileLogger.Info("Conexión SAP establecida correctamente para enterprise=" + target.EnterpriseCode + " db=" + target.CompanyDatabase + ".");
        }

        private static void ConfigureConnection(Company company, IntegrationSettings settings, EnterpriseSapTarget target)
        {
            company.Server = settings.SapServer;
            company.LicenseServer = settings.SapLicenseServer;
            company.CompanyDB = target.CompanyDatabase;
            company.DbServerType = settings.SapDbServerType;
            company.UserName = settings.SapUserName;
            company.Password = settings.SapPassword;
            company.DbUserName = settings.SqlUserName;
            company.DbPassword = settings.SqlPassword;

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