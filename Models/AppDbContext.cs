using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models.Transport;
using TwinNS = MLYSO.Web.Models.Twin;

namespace MLYSO.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<DynamicOrder> Orders => Set<DynamicOrder>();

        public DbSet<OOrder> OOrders => Set<OOrder>();
        public DbSet<OOrderItem> OOrderItems => Set<OOrderItem>();
        public DbSet<OPayment> OPayments => Set<OPayment>();
        public DbSet<OCustomer> OCustomers => Set<OCustomer>();
        public DbSet<OSeller> OSellers => Set<OSeller>();
        public DbSet<OGeo> OGeos => Set<OGeo>();

        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Quote> Quotes => Set<Quote>();
        public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Rma> Rmas => Set<Rma>();
        public DbSet<Purchase> Purchases => Set<Purchase>();

        public DbSet<CrmCustomer> CrmCustomers => Set<CrmCustomer>();
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Sla> Slas => Set<Sla>();
        public DbSet<ChurnScore> ChurnScores => Set<ChurnScore>();
        public DbSet<CrmRisk> CrmRisks => Set<CrmRisk>();

        public DbSet<WhTask> WhTasks => Set<WhTask>();
        public DbSet<AsnOrder> AsnOrders => Set<AsnOrder>();

        public DbSet<Route> Routes => Set<Route>();
        public DbSet<RouteStop> RouteStops => Set<RouteStop>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Activity> Activities => Set<Activity>();

        // Planning / Transport
        public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();
        public DbSet<RoutePlanStop> RoutePlanStops => Set<RoutePlanStop>();
        public DbSet<Shipment> Shipments => Set<Shipment>();

        // ---- Twin (dijital ikiz) ----
        public DbSet<TwinNS.Warehouse> Warehouses => Set<TwinNS.Warehouse>();
        public DbSet<TwinNS.ContainerType> ContainerTypes => Set<TwinNS.ContainerType>();
        public DbSet<TwinNS.BoxType> BoxTypes => Set<TwinNS.BoxType>();
        public DbSet<TwinNS.PackingJob> PackingJobs => Set<TwinNS.PackingJob>();
        public DbSet<TwinNS.PackingPlan> PackingPlans => Set<TwinNS.PackingPlan>();
        public DbSet<MLYSO.Web.Models.Twin.DemandHistory> DemandHistories => Set<MLYSO.Web.Models.Twin.DemandHistory>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<User>().HasIndex(u => u.Username).IsUnique();
            b.Entity<User>().Property<string?>("SelectedRole").HasMaxLength(64);
            b.Entity<User>().Property<bool>("RoleChosen").HasDefaultValue(false);
            b.Entity<User>().Property<bool>("IsAdmin").HasDefaultValue(false);

            b.Entity<OCustomer>().HasIndex(x => x.CustomerId).IsUnique();
            b.Entity<OSeller>().HasIndex(x => x.SellerId).IsUnique();
            b.Entity<OOrder>().HasIndex(x => x.OrderId).IsUnique();

            b.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
            b.Entity<Supplier>().HasIndex(x => x.Name).IsUnique();

            b.Entity<Quote>()
                .HasMany(q => q.Lines).WithOne()
                .HasForeignKey(l => l.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<PurchaseOrder>()
                .HasMany(p => p.Lines).WithOne()
                .HasForeignKey(l => l.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Purchase>().Property(x => x.Supplier).HasMaxLength(64);
            b.Entity<Purchase>().Property(x => x.Sku).HasMaxLength(64);
            b.Entity<Purchase>().Property(x => x.Status).HasMaxLength(24);

            b.Entity<CrmRisk>().Property(x => x.Customer).HasMaxLength(64);
            b.Entity<CrmRisk>().Property(x => x.Segment).HasMaxLength(16);

            b.Entity<AsnOrder>().Property(x => x.Sku).HasMaxLength(64);
            b.Entity<AsnOrder>().Property(x => x.Status).HasMaxLength(24);

            b.Entity<WhTask>().Property(x => x.Type).HasMaxLength(24);
            b.Entity<WhTask>().Property(x => x.Status).HasMaxLength(24);
            b.Entity<WhTask>().Property(x => x.Location).HasMaxLength(64);
            b.Entity<WhTask>().Property(x => x.Sku).HasMaxLength(64);

            b.Entity<Route>().Property(x => x.Vehicle).HasMaxLength(32);
            b.Entity<Route>().Property(x => x.Status).HasMaxLength(24);

            b.Entity<RouteStop>().HasIndex(x => new { x.RouteId, x.StopNo }).IsUnique();
            b.Entity<RouteStop>().Property(x => x.Status).HasMaxLength(24);
            b.Entity<RouteStop>().Property(x => x.ProofCode).HasMaxLength(24);

            b.Entity<Activity>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Audience).HasConversion<long>();
                e.HasIndex(x => new { x.EntityType, x.EntityId });
                e.HasIndex(x => x.OrderId);
                e.HasIndex(x => x.CreatedAt);
                e.Property(x => x.Code).HasMaxLength(48);
                e.Property(x => x.Title).HasMaxLength(160);
                e.Property(x => x.Severity).HasMaxLength(24);
                e.Property(x => x.EntityType).HasMaxLength(24);
                e.Property(x => x.EntityId).HasMaxLength(64);
                e.Property(x => x.OrderId).HasMaxLength(64);
                e.Property(x => x.RouteId).HasMaxLength(64);
                e.Property(x => x.TaskId).HasMaxLength(64);
                e.Property(x => x.Reason).HasMaxLength(160);
                e.Property(x => x.ActorUser).HasMaxLength(64);
                e.Property(x => x.ActorRole).HasMaxLength(48);
            });

            // RoutePlan & Stops
            b.Entity<RoutePlan>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(40);
                e.Property(x => x.Name).HasMaxLength(120);
                e.Property(x => x.VehiclePlate).HasMaxLength(16);
                e.Property(x => x.Optimization).HasMaxLength(16);

                e.HasMany(x => x.Stops)
                 .WithOne(s => s.RoutePlan!)
                 .HasForeignKey(s => s.RoutePlanId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<RoutePlan>(e => {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(40);
                e.Property(x => x.Name).HasMaxLength(120);
                e.Property(x => x.VehiclePlate).HasMaxLength(16);
                e.Property(x => x.Optimization).HasMaxLength(16);
                e.HasMany(x => x.Stops)
                 .WithOne(s => s.RoutePlan!)
                 .HasForeignKey(s => s.RoutePlanId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<RoutePlanStop>(e => {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.RoutePlanId, x.OrderNo }).IsUnique();
                e.Property(x => x.Title).HasMaxLength(120);
                e.Property(x => x.Address).HasMaxLength(240);
                e.Property(x => x.Status).HasMaxLength(24); // <-- Modeldeki Status ile eþleþiyor
            });

            // =========================
            // Twin (Dijital Ýkiz) model konfigürasyonu
            // =========================
            b.Entity<TwinNS.Warehouse>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(40);
                e.Property(x => x.Name).HasMaxLength(120);
            });

            b.Entity<TwinNS.ContainerType>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(24);
                e.Property(x => x.Name).HasMaxLength(64);
            });

            b.Entity<TwinNS.BoxType>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Code).HasMaxLength(24);
                e.Property(x => x.Name).HasMaxLength(64);
            });

            // PackingJob -> Items/Containers
            b.Entity<TwinNS.PackingJob>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Items)
                    .WithOne(i => i.Job)
                    .HasForeignKey(i => i.PackingJobId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(x => x.Containers)
                    .WithOne(c => c.Job)
                    .HasForeignKey(c => c.PackingJobId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PackingPlan -> WarehousePlacements
            b.Entity<TwinNS.PackingPlan>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.WarehousePlacements)
                    .WithOne(wp => wp.Plan)
                    .HasForeignKey(wp => wp.PackingPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WarehousePlacement -> BoxPlacements
            b.Entity<TwinNS.WarehousePlacement>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.BoxPlacements)
                    .WithOne(bp => bp.WarehousePlacement)
                    .HasForeignKey(bp => bp.WarehousePlacementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // BoxPlacement basit key
            b.Entity<TwinNS.BoxPlacement>(e =>
            {
                e.HasKey(x => x.Id);
            });
        }
    }
}
