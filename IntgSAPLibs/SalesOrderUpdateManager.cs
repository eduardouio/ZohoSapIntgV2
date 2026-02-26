using System;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class SalesOrderUpdateManager
    {
        private readonly Company _company;

        public SalesOrderUpdateManager(Company company)
        {
            if (company == null)
            {
                throw new ArgumentNullException(nameof(company));
            }

            if (!company.Connected)
            {
                throw new InvalidOperationException("La conexión SAP no está activa.");
            }

            _company = company;
        }

        public void ActualizarOrdenVenta(IntegrationOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (!order.DocEntry.HasValue || order.DocEntry.Value <= 0)
            {
                throw new InvalidOperationException("La orden no tiene doc_entry válido para actualizar en SAP.");
            }

            if (order.Details == null || order.Details.Count == 0)
            {
                throw new InvalidOperationException("La orden no tiene detalles para actualizar en SAP.");
            }

            FileLogger.Info("Iniciando actualización de orden SAP para order_id=" + order.Id + " docEntry=" + order.DocEntry.Value);

            var sapOrder = (Documents)_company.GetBusinessObject(BoObjectTypes.oOrders);
            if (!sapOrder.GetByKey(order.DocEntry.Value))
            {
                throw new InvalidOperationException("No se encontró la orden en SAP para docEntry=" + order.DocEntry.Value);
            }

            sapOrder.CardCode = order.Customer;
            sapOrder.DocDate = order.OrderDate;
            sapOrder.DocDueDate = order.OrderDate;
            sapOrder.TaxDate = order.OrderDate;
            sapOrder.Comments = "Actualización SQL order_id=" + order.Id + " zoho_id=" + (order.ZohoId ?? string.Empty);

            // Asignar vendedor desde OSLP
            var resolver = new SalesPersonResolver(_company);
            int slpCode = resolver.ResolveSlpCode(order.Salesperson);
            if (slpCode >= 0)
            {
                sapOrder.SalesPersonCode = slpCode;
                FileLogger.Info("Vendedor asignado a orden SAP (update): SlpCode=" + slpCode + " order_id=" + order.Id);
            }

            int existingLineCount = sapOrder.Lines.Count;

            for (int lineIndex = 0; lineIndex < order.Details.Count; lineIndex++)
            {
                var detail = order.Details[lineIndex];

                if (lineIndex >= existingLineCount)
                {
                    sapOrder.Lines.Add();
                    existingLineCount++;
                }

                sapOrder.Lines.SetCurrentLine(lineIndex);
                sapOrder.Lines.ItemCode = detail.Product;
                sapOrder.Lines.Quantity = (double)detail.Quantity;
                sapOrder.Lines.UnitPrice = (double)detail.UnitPrice;
                sapOrder.Lines.DiscountPercent = (double)detail.Discount;
                sapOrder.Lines.WarehouseCode = order.IdWarehouse.ToString();
            }

            for (int lineIndex = existingLineCount - 1; lineIndex >= order.Details.Count; lineIndex--)
            {
                sapOrder.Lines.SetCurrentLine(lineIndex);
                sapOrder.Lines.Delete();
            }

            int updateResult = sapOrder.Update();
            if (updateResult != 0)
            {
                throw new InvalidOperationException("Error al actualizar la orden de venta: " + _company.GetLastErrorDescription());
            }

            FileLogger.Info("Orden de venta actualizada correctamente. order_id=" + order.Id + " docEntry=" + order.DocEntry.Value);
        }
    }
}