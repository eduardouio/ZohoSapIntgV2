using System;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    /// <summary>
    /// Resuelve el código de vendedor (SlpCode) desde la tabla OSLP de SAP
    /// a partir del nombre del vendedor almacenado en la base de integración.
    /// </summary>
    internal sealed class SalesPersonResolver
    {
        private readonly Company _company;

        public SalesPersonResolver(Company company)
        {
            if (company == null)
            {
                throw new ArgumentNullException(nameof(company));
            }

            _company = company;
        }

        /// <summary>
        /// Busca el SlpCode en la tabla OSLP de SAP.
        /// Si <paramref name="salesperson"/> es numérico, lo usa directamente como SlpCode
        /// verificando que exista en OSLP.
        /// Si es texto, busca por coincidencia exacta en SlpName.
        /// Retorna -1 si no se encuentra.
        /// </summary>
        public int ResolveSlpCode(string salesperson)
        {
            if (string.IsNullOrWhiteSpace(salesperson))
            {
                FileLogger.Info("Salesperson vacío, no se asignará vendedor en SAP.");
                return -1;
            }

            int slpCode;
            if (int.TryParse(salesperson.Trim(), out slpCode))
            {
                // El valor es numérico: verificar que exista en OSLP
                if (ExistsSlpCode(slpCode))
                {
                    FileLogger.Info("Vendedor resuelto por SlpCode directo: " + slpCode);
                    return slpCode;
                }

                FileLogger.Info("SlpCode=" + slpCode + " no encontrado en OSLP, se intentará buscar por nombre.");
            }

            // Buscar por nombre en OSLP
            int resolved = FindSlpCodeByName(salesperson.Trim());
            if (resolved >= 0)
            {
                FileLogger.Info("Vendedor resuelto por nombre '" + salesperson.Trim() + "' → SlpCode=" + resolved);
            }
            else
            {
                FileLogger.Info("Vendedor '" + salesperson.Trim() + "' no encontrado en OSLP.");
            }

            return resolved;
        }

        private bool ExistsSlpCode(int slpCode)
        {
            var rs = (Recordset)_company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                rs.DoQuery("SELECT SlpCode FROM OSLP WHERE SlpCode = " + slpCode);
                return !rs.EoF;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }

        private int FindSlpCodeByName(string name)
        {
            var rs = (Recordset)_company.GetBusinessObject(BoObjectTypes.BoRecordset);
            try
            {
                // Escapar comillas simples para evitar inyección SQL
                string safeName = name.Replace("'", "''");
                rs.DoQuery("SELECT SlpCode FROM OSLP WHERE SlpName = '" + safeName + "'");

                if (!rs.EoF)
                {
                    return (int)rs.Fields.Item("SlpCode").Value;
                }

                return -1;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
    }
}
