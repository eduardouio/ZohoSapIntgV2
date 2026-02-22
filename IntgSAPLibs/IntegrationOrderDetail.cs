namespace ConsoleApp2.IntgSAPLibs
{
    internal sealed class IntegrationOrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public string Product { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Discount { get; set; }
    }
}