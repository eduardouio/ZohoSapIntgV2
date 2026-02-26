using System;
using SAPbobsCOM;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class SalesOrderManager
    {
        private readonly Company _company;

        public SalesOrderManager(Company company)
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

        public SapOrderResult CrearOrdenVenta(IntegrationOrder order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (order.Details == null || order.Details.Count == 0)
            {
                throw new InvalidOperationException("La orden no tiene detalles para integrar a SAP.");
            }

            FileLogger.Info("Iniciando creación de orden SAP para order_id: " + order.Id);

            var salesOrder = (Documents)_company.GetBusinessObject(BoObjectTypes.oOrders);
            salesOrder.CardCode = order.Customer;
            salesOrder.DocDate = order.OrderDate;
            salesOrder.DocDueDate = order.OrderDate;
            salesOrder.TaxDate = order.OrderDate;
            salesOrder.Comments = "Integración SQL order_id=" + order.Id + " zoho_id=" + (order.ZohoId ?? string.Empty);
            salesOrder.DocObjectCode = BoObjectTypes.oOrders;

            // Asignar vendedor desde OSLP
            var resolver = new SalesPersonResolver(_company);
            int slpCode = resolver.ResolveSlpCode(order.Salesperson);
            if (slpCode >= 0)
            {
                salesOrder.SalesPersonCode = slpCode;
                FileLogger.Info("Vendedor asignado a orden SAP: SlpCode=" + slpCode + " order_id=" + order.Id);
            }

            for (int lineIndex = 0; lineIndex < order.Details.Count; lineIndex++)
            {
                var detail = order.Details[lineIndex];

                if (lineIndex > 0)
                {
                    salesOrder.Lines.Add();
                }

                salesOrder.Lines.SetCurrentLine(lineIndex);
                salesOrder.Lines.ItemCode = detail.Product;
                salesOrder.Lines.Quantity = (double)detail.Quantity;
                salesOrder.Lines.UnitPrice = (double)detail.UnitPrice;
                salesOrder.Lines.DiscountPercent = (double)detail.Discount;
                salesOrder.Lines.WarehouseCode = order.IdWarehouse.ToString();
            }

            int addResult = salesOrder.Add();
            if (addResult != 0)
            {
                throw new InvalidOperationException("Error al crear la orden de venta: " + _company.GetLastErrorDescription());
            }

            string newDocEntry;
            _company.GetNewObjectCode(out newDocEntry);

            int docEntryValue;
            if (!int.TryParse(newDocEntry, out docEntryValue))
            {
                throw new InvalidOperationException("No se pudo convertir el DocEntry retornado por SAP: " + newDocEntry);
            }

            int docNumValue = 0;
            var createdOrder = (Documents)_company.GetBusinessObject(BoObjectTypes.oOrders);
            if (createdOrder.GetByKey(docEntryValue))
            {
                docNumValue = createdOrder.DocNum;
            }

            FileLogger.Info("Orden de venta creada. order_id=" + order.Id + " docEntry=" + docEntryValue + " docNum=" + docNumValue);

            return new SapOrderResult
            {
                DocEntry = docEntryValue,
                DocNum = docNumValue
            };
        }
    }
}