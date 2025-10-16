// Models/Roles.cs
namespace MLYSO.Web.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";

        // Operasyon / Planlama / Lojistik
        public const string Operations = "Operations";
        public const string Planning = "Planning";
        public const string Logistics = "Logistics";

        // Depo
        public const string WarehouseManager = "WarehouseManager";
        public const string WarehouseChief = "WarehouseChief";   // _Layout & AccountController & AuthApiController
        public const string WarehouseOperator = "WarehouseOperator";

        // Saha
        public const string Driver = "Driver";

        // Satýnalma / ERP / Tedarikçi
        public const string Purchasing = "Purchasing";
        public const string ERP = "ERP";
        public const string Supplier = "Supplier";

        // CRM
        public const string CRM = "CRM";
        public const string CustomerService = "CustomerService";
        public const string CrmAgent = "CrmAgent";

        // Müþteri
        public const string Customer = "Customer";
    }
}
