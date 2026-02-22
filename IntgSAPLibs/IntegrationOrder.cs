using System;
using System.Collections.Generic;

namespace ZhohoSapIntg.IntgSAPLibs
{
    internal sealed class IntegrationOrder
    {
        public int Id { get; set; }

        public string ZohoId { get; set; }

        public string Customer { get; set; }

        public DateTime OrderDate { get; set; }

        public string Salesperson { get; set; }

        public int? DocEntry { get; set; }

        public int? DocNum { get; set; }

        public List<IntegrationOrderDetail> Details { get; } = new List<IntegrationOrderDetail>();
    }
}