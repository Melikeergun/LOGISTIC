using System;
using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models
{
    [Flags]
    public enum Audience : long
    {
        None = 0,
        Admin = 1 << 0,
        Operations = 1 << 1,
        Planning = 1 << 2,
        Logistics = 1 << 3,
        WarehouseManager = 1 << 4,
        WarehouseOperator = 1 << 5,
        Driver = 1 << 6,
        Purchasing = 1 << 7,
        CrmAgent = 1 << 8,
        CustomerService = 1 << 9,
        Supplier = 1 << 10,
        Customer = 1 << 11,
        All = ~0L
    }

    public class Activity
    {
        public long Id { get; set; }

        [MaxLength(48)]
        public string Code { get; set; } = ""; 

        [MaxLength(160)]
        public string Title { get; set; } = ""; 

        public string Detail { get; set; } = ""; 

        [MaxLength(24)]
        public string Severity { get; set; } = "info"; 

        [MaxLength(24)]
        public string EntityType { get; set; } = "";   

        [MaxLength(64)]
        public string? EntityId { get; set; }          
        [MaxLength(64)]
        public string? OrderId { get; set; }

        [MaxLength(64)]
        public string? RouteId { get; set; }

        [MaxLength(64)]
        public string? TaskId { get; set; }

        [MaxLength(160)]
        public string? Reason { get; set; }

        public int? DelayMinutes { get; set; }

        public Audience Audience { get; set; } = Audience.All;

        public string? DataJson { get; set; } //
        [MaxLength(64)]
        public string? ActorUser { get; set; }

        [MaxLength(48)]
        public string? ActorRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
