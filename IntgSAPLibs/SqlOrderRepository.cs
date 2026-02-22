using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ConsoleApp2.IntgSAPLibs
{
    internal sealed class SqlOrderRepository
    {
        private const string IntegrationDatabase = "DB_INTG_SAPZOHO_PROD";
        private readonly string _connectionString;

        public SqlOrderRepository()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = SAPConnection.SqlServerName,
                InitialCatalog = IntegrationDatabase,
                UserID = SAPConnection.SqlUserName,
                Password = SAPConnection.SqlPassword,
                IntegratedSecurity = false,
                ConnectTimeout = 15
            };

            _connectionString = builder.ConnectionString;
        }

        public List<IntegrationOrder> GetPendingOrders()
        {
            var ordersById = new Dictionary<int, IntegrationOrder>();

            const string query = @"
SELECT
    o.id,
    o.id_zoho,
    o.customer,
    o.order_date,
    o.salesperson,
    d.id AS detail_id,
    d.order_id,
    d.product,
    d.quantity,
    d.unit_price,
    d.discount
FROM SAP_Orders o
LEFT JOIN SAP_Order_Details d ON d.order_id = o.id
WHERE o.is_integrated = 0
ORDER BY o.id, d.id;";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
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
                                Salesperson = reader["salesperson"] as string
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
    }
}