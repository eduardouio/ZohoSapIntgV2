using System;
using SAPbobsCOM;

namespace ConsoleApp2.IntgSAPLibs
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

        public string CrearOrdenVentaPrueba()
        {
            FileLogger.Info("Iniciando creación de orden de venta de prueba.");

            var salesOrder = (Documents)_company.GetBusinessObject(BoObjectTypes.oOrders);
            salesOrder.CardCode = "C1790016919001";
            salesOrder.FederalTaxID = "1790016919001";
            salesOrder.DocDate = DateTime.Now;
            salesOrder.DocDueDate = DateTime.Now.AddDays(7);
            salesOrder.TaxDate = DateTime.Now.AddDays(7);
            salesOrder.Comments = "Orden de venta de prueba creada desde la API";

            salesOrder.Lines.ItemCode = "01011010010206010750";
            salesOrder.Lines.Quantity = 904000;
            salesOrder.Lines.WarehouseCode = "1";
            salesOrder.DocObjectCode = BoObjectTypes.oOrders;
            salesOrder.Lines.Add();

            int addResult = salesOrder.Add();
            if (addResult != 0)
            {
                throw new InvalidOperationException("Error al crear la orden de venta: " + _company.GetLastErrorDescription());
            }

            string docEntry;
            _company.GetNewObjectCode(out docEntry);
            FileLogger.Info("Orden de venta creada. DocEntry: " + docEntry);
            return docEntry;
        }
    }
}