using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAPbobsCOM;
using System.Threading.Tasks;
using System.Threading;
using System.ServiceProcess;
using System.Diagnostics;

namespace ConsoleApp2
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            if (Environment.UserInteractive || IsConsoleMode(args))
            {
                EjecutarProcesoSAP();
                return;
            }

            ServiceBase.Run(new SapIntegrationService());
        }

        internal static void EjecutarProcesoSAP()
        {
            Company oCompany = new SAPbobsCOM.Company();

            try
            {
                oCompany.Server = "SERVIDORSAP";
                oCompany.LicenseServer = "SERVIDORSAP:30000";
                oCompany.CompanyDB = "TEST_VINESA";
                oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2016;

                // CAMBIAR DESPUES, NO OLVIDAR
                oCompany.UserName = "auditori";
                oCompany.Password = "1234";
                oCompany.DbUserName = "intg";
                oCompany.DbPassword = "Horiz0nt3s";

                oCompany.UseTrusted = false;

                int conexion = oCompany.Connect();
                if (conexion != 0)
                {
                    throw new InvalidOperationException("Conexión Errónea: " + oCompany.GetLastErrorDescription());
                }

                WriteInfo("Conectado correctamente");
                CrearOrdenCompra(oCompany);
            }
            finally
            {
                if (oCompany.Connected)
                {
                    oCompany.Disconnect();
                }
            }
        }

        private static bool IsConsoleMode(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return false;
            }

            return args.Any(a => string.Equals(a, "--console", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "/console", StringComparison.OrdinalIgnoreCase));
        }

        private static void WriteInfo(string message)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(message);
            }
        }

        static void CrearOrdenCompra(Company oCompany)
        {
            Documents oOrden = (Documents)oCompany.GetBusinessObject(BoObjectTypes.oOrders);
            oOrden.CardCode = "C1790016919001";
            oOrden.FederalTaxID = "1790016919001";
            oOrden.DocDate = DateTime.Now;
            oOrden.DocDueDate = DateTime.Now.AddDays(7);
            oOrden.TaxDate = DateTime.Now.AddDays(7);
            oOrden.Comments = "OrdenPrueba creada desde la API";

            oOrden.Lines.ItemCode = "01011010010206010750";
            oOrden.Lines.Quantity = 904000;
            oOrden.Lines.WarehouseCode = "1";
            WriteInfo("proveedor:" + oOrden.CardCode);
            WriteInfo("Fecha: " + oOrden.DocDate);
            oOrden.DocObjectCode = BoObjectTypes.oPurchaseOrders;
            oOrden.Lines.Add();


            int RESULT = oOrden.Add();

            if (RESULT != 0)
            {
                throw new InvalidOperationException("Error al crear la orden de compra: " + oCompany.GetLastErrorDescription());
            }
            else
            {
                string docEntry;
                oCompany.GetNewObjectCode(out docEntry);
                WriteInfo("Orden de compra creada exitosamente. DocEntry: " + docEntry);
            }
        }

    }

    internal sealed class SapIntegrationService : ServiceBase
    {
        private Timer _timer;
        private bool _isRunning;

        public SapIntegrationService()
        {
            ServiceName = "ConsoleApp2Service";
            AutoLog = true;
            CanPauseAndContinue = false;
            CanShutdown = true;
            CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer(RunProcessSafely, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void RunProcessSafely(object state)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            try
            {
                Program.EjecutarProcesoSAP();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ServiceName, ex.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
