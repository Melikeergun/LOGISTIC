using System;
using System.Collections.Generic;

namespace MLYSO.Web.Models
{
    public class CustomerOrdersState
    {
        public List<CustomerOrder> Orders { get; set; } = new();
    }

    public class CustomerOrder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string OrderNo { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;

        public string Status { get; set; } = "New";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnApprovedAt { get; set; }
        public string? ReturnReason { get; set; }
    }
}
