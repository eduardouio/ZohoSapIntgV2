using System;
using System.Linq;
using System.Threading;
using System.ServiceProcess;
using System.Diagnostics;
using ZhohoSapIntg.IntgSAPLibs;

namespace ZhohoSapIntg
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
                    WriteInfo("Conectado correctamente a SAP");
                    var salesOrderManager = new SalesOrderManager(sapConnection.Company);
                    var salesOrderUpdateManager = new SalesOrderUpdateManager(sapConnection.Company);
                    var repository = new SqlOrderRepository();

                    WriteInfo("Buscando pedidos pendientes por actualizar...");
                    var ordersToUpdate = repository.GetOrdersToUpdate();
                    WriteInfo("Pedidos para actualizar encontrados: " + ordersToUpdate.Count);

                    int updatedCount = 0;
                    int updateFailedCount = 0;

                    foreach (var order in ordersToUpdate)
                    {
                        try
                        {
                            salesOrderUpdateManager.ActualizarOrdenVenta(order);
                            repository.MarkOrderUpdated(order.Id);
                            updatedCount++;
                            WriteInfo("Pedido actualizado. order_id=" + order.Id + " docEntry=" + order.DocEntry);
                        }
                        catch (Exception updateEx)
                        {
                            updateFailedCount++;
                            try
                            {
                                repository.MarkOrderUpdateFailed(order.Id, updateEx.Message);
                            }
                            catch (Exception persistEx)
                            {
                                FileLogger.Error("No se pudo persistir estado de fallo para actualización order_id=" + order.Id + ".", persistEx);
                            }
                            FileLogger.Error("Error actualizando pedido order_id=" + order.Id + ". Se continuará con el siguiente.", updateEx);
                        }
                    }

                    WriteInfo("Buscando pedidos pendientes por importar...");
                    var pendingOrders = repository.GetPendingOrders();

                    WriteInfo("Pedidos pendientes encontrados: " + pendingOrders.Count);

                    if (pendingOrders.Count == 0 && ordersToUpdate.Count == 0)
                    {
                        WriteInfo("No hay pedidos pendientes por crear ni por actualizar en este ciclo.");
                        return;
                    }

                    WriteInfo("Iniciando importación de " + pendingOrders.Count + " pedido(s).");
                    int importedCount = 0;
                    int failedCount = 0;

                    foreach (var order in pendingOrders)
                    {
                        try
                        {
                            var result = salesOrderManager.CrearOrdenVenta(order);
                            repository.MarkOrderIntegrated(order.Id, result.DocEntry, result.DocNum);

                            WriteInfo("Pedido integrado. order_id=" + order.Id + " docEntry=" + result.DocEntry + " docNum=" + result.DocNum);
                            importedCount++;
                        }
                        catch (Exception orderEx)
                        {
                            failedCount++;
                            try
                            {
                                repository.MarkOrderCreateFailed(order.Id, orderEx.Message);
                            }
                            catch (Exception persistEx)
                            {
                                FileLogger.Error("No se pudo persistir estado de fallo para creación order_id=" + order.Id + ".", persistEx);
                            }
                            FileLogger.Error("Error integrando pedido order_id=" + order.Id + ". Se continuará con el siguiente.", orderEx);
                        }
                    }

                    WriteInfo("Resumen de creación: total=" + pendingOrders.Count + ", importados=" + importedCount + ", fallidos=" + failedCount + ".");
                    WriteInfo("Resumen de actualización: total=" + ordersToUpdate.Count + ", actualizados=" + updatedCount + ", fallidos=" + updateFailedCount + ".");
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
            ServiceName = "ZhohoSapIntgService";
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
