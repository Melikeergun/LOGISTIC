
using System.Text.Json;

namespace MLYSO.Web.Services;

public class PermissionService
{
    // Dataset-aligned field groups (you can extend later)
    public static readonly string[] WarehouseEditable = new[] {
        "warehouse_inventory_level","loading_unloading_time","handling_equipment_availability",
        "dock_availability","storage_space_utilization","iot_temperature","cargo_condition_status"
    };

    public static readonly string[] CustomerEditable = new[] {
        "delivery_preference","return_request","exchange_request","return_reason"
    };

    public static readonly string[] StatusFields = new[] { "order_fulfillment_status", "status" };

    public bool CanEditField(string role, string field)
    {
        return role switch
        {
            "Admin" => true,
            "WarehouseChief" => WarehouseEditable.Contains(field) || field.StartsWith("warehouse_"),
            "Customer" => CustomerEditable.Contains(field),
            "CustomerService" => CustomerEditable.Contains(field),
            _ => !StatusFields.Contains(field) // others can edit non-status generic fields
        };
    }
}
