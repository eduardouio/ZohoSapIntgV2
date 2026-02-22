using System;
using System.Collections.Generic;
using System.Configuration;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class EnterpriseSapTarget
    {
        public string EnterpriseCode { get; set; }

        public string CompanyDatabase { get; set; }

        public int WarehouseId { get; set; }
    }

    internal sealed class IntegrationSettings
    {
        public string SqlServerName { get; private set; }

        public string SqlUserName { get; private set; }

        public string SqlPassword { get; private set; }

        public string IntegrationDatabase { get; private set; }

        public string SapServer { get; private set; }

        public string SapLicenseServer { get; private set; }

        public BoDataServerTypes SapDbServerType { get; private set; }

        public string SapUserName { get; private set; }

        public string SapPassword { get; private set; }

        public List<EnterpriseSapTarget> EnterpriseTargets { get; } = new List<EnterpriseSapTarget>();

        public static IntegrationSettings LoadFromAppConfig()
        {
            var settings = new IntegrationSettings
            {
                SqlServerName = GetRequiredSetting("SqlServerName"),
                SqlUserName = GetRequiredSetting("SqlUserName"),
                SqlPassword = GetRequiredSetting("SqlPassword"),
                IntegrationDatabase = GetRequiredSetting("IntegrationDatabase"),
                SapServer = GetRequiredSetting("SapServer"),
                SapLicenseServer = GetRequiredSetting("SapLicenseServer"),
                SapUserName = GetRequiredSetting("SapUserName"),
                SapPassword = GetRequiredSetting("SapPassword"),
                SapDbServerType = ParseDbServerType(GetRequiredSetting("SapDbServerType"))
            };

            string enterpriseMappingsRaw = GetRequiredSetting("EnterpriseMappings");
            ParseEnterpriseMappings(enterpriseMappingsRaw, settings.EnterpriseTargets);

            if (settings.EnterpriseTargets.Count == 0)
            {
                throw new InvalidOperationException("No se encontraron empresas configuradas en EnterpriseMappings.");
            }

            return settings;
        }

        private static string GetRequiredSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Falta configuración requerida en App.config: " + key);
            }

            return value.Trim();
        }

        private static BoDataServerTypes ParseDbServerType(string value)
        {
            BoDataServerTypes parsedType;
            if (!Enum.TryParse(value, true, out parsedType))
            {
                throw new InvalidOperationException("Valor inválido para SapDbServerType: " + value);
            }

            return parsedType;
        }

        private static void ParseEnterpriseMappings(string rawMappings, List<EnterpriseSapTarget> targets)
        {
            string[] mappings = rawMappings.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string mapping in mappings)
            {
                string[] parts = mapping.Split(new[] { '|' }, StringSplitOptions.None);
                if (parts.Length != 3)
                {
                    throw new InvalidOperationException("Formato inválido en EnterpriseMappings. Use ENTERPRISE|SAP_DB|WAREHOUSE_ID.");
                }

                int warehouseId;
                if (!int.TryParse(parts[2].Trim(), out warehouseId) || warehouseId <= 0)
                {
                    throw new InvalidOperationException("id_warehouse inválido en EnterpriseMappings para: " + mapping);
                }

                targets.Add(new EnterpriseSapTarget
                {
                    EnterpriseCode = parts[0].Trim(),
                    CompanyDatabase = parts[1].Trim(),
                    WarehouseId = warehouseId
                });
            }
        }
    }
}