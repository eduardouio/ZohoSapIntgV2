using System;
using System.Linq;
using System.Threading;
using System.ServiceProcess;
using System.Diagnostics;
using ConsoleApp2.IntgSAPLibs;

namespace ConsoleApp2
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            FileLogger.Info("Aplicación iniciada.");

            if (Environment.UserInteractive || IsConsoleMode(args))
            {
                FileLogger.Info("Ejecución en modo interactivo/consola.");
                EjecutarProcesoSAP();
                return;
            }

            FileLogger.Info("Ejecución en modo servicio de Windows.");
            ServiceBase.Run(new SapIntegrationService());
        }

        internal static void EjecutarProcesoSAP()
        {
            FileLogger.Info("Inicia proceso SAP.");
            try
            {
                using (var sapConnection = new SAPConnection())
                {
                    WriteInfo("Conectado correctamente");
                    var salesOrderManager = new SalesOrderManager(sapConnection.Company);
                    string docEntry = salesOrderManager.CrearOrdenVentaPrueba();
                    WriteInfo("Orden de venta creada exitosamente. DocEntry: " + docEntry);
                }
            }
            catch (Exception ex)
            {
                FileLogger.Error("Error en proceso SAP.", ex);
                throw;
            }
            finally
            {
                FileLogger.Info("Finaliza proceso SAP.");
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
            FileLogger.Info(message);

            if (Environment.UserInteractive)
            {
                Console.WriteLine(message);
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
            FileLogger.Info("Servicio iniciado.");
            _timer = new Timer(RunProcessSafely, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        protected override void OnStop()
        {
            FileLogger.Info("Servicio detenido.");
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
                FileLogger.Info("Proceso SAP omitido: ya existe una ejecución en curso.");
                return;
            }

            _isRunning = true;
            try
            {
                FileLogger.Info("Ejecución de tarea programada del servicio.");
                Program.EjecutarProcesoSAP();
            }
            catch (Exception ex)
            {
                FileLogger.Error("Error en ejecución del servicio.", ex);
                EventLog.WriteEntry(ServiceName, ex.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                _isRunning = false;
            }
        }
    }
}
