using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class SqlOrderRepository
    {
        private readonly string _connectionString;

        public SqlOrderRepository(IntegrationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = settings.SqlServerName,
                InitialCatalog = settings.IntegrationDatabase,
                UserID = settings.SqlUserName,
                Password = settings.SqlPassword,
                IntegratedSecurity = false,
                ConnectTimeout = 15
            };

            _connectionString = builder.ConnectionString;
        }

        public List<IntegrationOrder> GetPendingOrders(string enterprise, int warehouseId)
        {
            const string query = @"
SELECT
    o.id,
    o.id_zoho,
    o.customer,
    o.order_date,
    o.doc_entry,
    o.doc_num,
    o.salesperson,
    o.seler_id,
    o.notes,
    o.enterprise,
    o.id_warehouse,
    d.id AS detail_id,
    d.order_id,
    d.product,
    d.quantity,
    d.unit_price,
    d.discount
FROM SAP_Orders o
LEFT JOIN SAP_Order_Details d ON d.order_id = o.id
WHERE o.is_integrated = 0
    AND o.is_failed = 0
    AND o.enterprise = @enterprise
    AND o.id_warehouse = @warehouseId
ORDER BY o.id, d.id;";

            return GetOrdersByQuery(query, enterprise, warehouseId);
        }

        public List<IntegrationOrder> GetOrdersToUpdate(string enterprise, int warehouseId)
        {
            const string query = @"
SELECT
    o.id,
    o.id_zoho,
    o.customer,
    o.order_date,
    o.doc_entry,
    o.doc_num,
    o.salesperson,
    o.seler_id,
    o.notes,
    o.enterprise,
    o.id_warehouse,
    d.id AS detail_id,
    d.order_id,
    d.product,
    d.quantity,
    d.unit_price,
    d.discount
FROM SAP_Orders o
LEFT JOIN SAP_Order_Details d ON d.order_id = o.id
WHERE o.is_integrated = 1
  AND o.is_updated = 1
    AND o.is_failed = 0
    AND o.enterprise = @enterprise
    AND o.id_warehouse = @warehouseId
  AND o.doc_entry IS NOT NULL
ORDER BY o.id, d.id;";

                        return GetOrdersByQuery(query, enterprise, warehouseId);
        }

                private List<IntegrationOrder> GetOrdersByQuery(string query, string enterprise, int warehouseId)
        {
            var ordersById = new Dictionary<int, IntegrationOrder>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                                command.Parameters.AddWithValue("@enterprise", enterprise);
                                command.Parameters.AddWithValue("@warehouseId", warehouseId);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int orderId = reader.GetInt32(reader.GetOrdinal("id"));
                        IntegrationOrder order;
                        if (!ordersById.TryGetValue(orderId, out order))
                        {
                            order = new IntegrationOrder
                            {
                                Id = orderId,
                                ZohoId = reader["id_zoho"] as string,
                                Customer = reader["customer"] as string,
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("order_date")),
                                DocEntry = reader["doc_entry"] == DBNull.Value ? (int?)null : reader.GetInt32(reader.GetOrdinal("doc_entry")),
                                DocNum = reader["doc_num"] == DBNull.Value ? (int?)null : reader.GetInt32(reader.GetOrdinal("doc_num")),
                                Salesperson = reader["salesperson"] as string,
                                SelerId = reader["seler_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["seler_id"]),
                                Notes = reader["notes"] as string,
                                Enterprise = reader["enterprise"] as string,
                                IdWarehouse = reader.GetInt32(reader.GetOrdinal("id_warehouse"))
                            };

                            ordersById.Add(orderId, order);
                        }

                        if (reader["detail_id"] != DBNull.Value)
                        {
                            var detail = new IntegrationOrderDetail
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("detail_id")),
                                OrderId = reader.GetInt32(reader.GetOrdinal("order_id")),
                                Product = reader["product"] as string,
                                Quantity = reader.GetDecimal(reader.GetOrdinal("quantity")),
                                UnitPrice = reader.GetDecimal(reader.GetOrdinal("unit_price")),
                                Discount = reader.GetDecimal(reader.GetOrdinal("discount"))
                            };

                            order.Details.Add(detail);
                        }
                    }
                }
            }

            return new List<IntegrationOrder>(ordersById.Values);
        }

        public void MarkOrderIntegrated(int orderId, int docEntry, int docNum)
        {
            const string update = @"
UPDATE SAP_Orders
SET
    is_integrated = 1,
    is_updated = 0,
    is_failed = 0,
    error_message = NULL,
    integration_date = GETDATE(),
    doc_entry = @docEntry,
    doc_num = @docNum
WHERE id = @id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(update, connection))
            {
                command.Parameters.AddWithValue("@id", orderId);
                command.Parameters.AddWithValue("@docEntry", docEntry);
                command.Parameters.AddWithValue("@docNum", docNum);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void MarkOrderUpdated(int orderId)
        {
            const string update = @"
UPDATE SAP_Orders
SET
    is_updated = 0,
    is_failed = 0,
    error_message = NULL,
    integration_date = GETDATE()
WHERE id = @id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(update, connection))
            {
                command.Parameters.AddWithValue("@id", orderId);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void MarkOrderCreateFailed(int orderId, string errorMessage)
        {
            const string update = @"
UPDATE SAP_Orders
SET
    is_failed = 1,
    error_message = @errorMessage
WHERE id = @id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(update, connection))
            {
                command.Parameters.AddWithValue("@id", orderId);
                command.Parameters.AddWithValue("@errorMessage", TruncateErrorMessage(errorMessage));

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void MarkOrderUpdateFailed(int orderId, string errorMessage)
        {
            const string update = @"
UPDATE SAP_Orders
SET
    is_updated = 1,
    is_failed = 1,
    error_message = @errorMessage
WHERE id = @id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(update, connection))
            {
                command.Parameters.AddWithValue("@id", orderId);
                command.Parameters.AddWithValue("@errorMessage", TruncateErrorMessage(errorMessage));

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static string TruncateErrorMessage(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return "Error no especificado durante integración SAP.";
            }

            return errorMessage.Length <= 500 ? errorMessage : errorMessage.Substring(0, 500);
        }
    }
}