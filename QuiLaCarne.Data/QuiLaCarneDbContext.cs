using Microsoft.EntityFrameworkCore;
using QuiLaCarne.Models;
using QuiLaCarne.Models.Lookup;
using QuiLaCarne.Models.Enums;

namespace QuiLaCarne.Data;

public class QuiLaCarneDbContext : DbContext
{
    public QuiLaCarneDbContext(DbContextOptions<QuiLaCarneDbContext> options) : base(options) { }


    public DbSet<Users> Users => Set<Users>();
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
    public DbSet<Bans> Bans => Set<Bans>();
    public DbSet<RestaurantTables> RestaurantTables => Set<RestaurantTables>();
    public DbSet<Reservations> Reservations => Set<Reservations>();
    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderItems> OrderItems => Set<OrderItems>();
    public DbSet<Dishes> Dishes => Set<Dishes>();
    public DbSet<Ingredients> Ingredients => Set<Ingredients>();
    public DbSet<GuestReports> GuestReports => Set<GuestReports>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    public DbSet<Allergens> Allergens => Set<Allergens>();
    public DbSet<BanStatus> BanStatuses => Set<BanStatus>();
    public DbSet<DishesCategories> DishesCategories => Set<DishesCategories>();
    public DbSet<GuestReportStatus> GuestReportStatuses => Set<GuestReportStatus>();
    public DbSet<OrderItemsStatus> OrderItemsStatuses => Set<OrderItemsStatus>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<ReservationStatus> ReservationStatuses => Set<ReservationStatus>();
    public DbSet<Roles> Roles => Set<Roles>();
    public DbSet<TableStatus> TableStatuses => Set<TableStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties())
            {
                if (prop.ClrType == typeof(Guid) || prop.ClrType == typeof(Guid?))
                    prop.SetColumnType("TEXT");
                if (prop.ClrType == typeof(DateTimeOffset) || prop.ClrType == typeof(DateTimeOffset?))
                    prop.SetColumnType("TEXT");
                if (prop.ClrType == typeof(decimal) || prop.ClrType == typeof(decimal?))
                    prop.SetColumnType("TEXT");
            }
        }


        modelBuilder.Entity<VerificationToken>()
            .Property(e => e.TokenType)
            .HasConversion<string>();

        
        modelBuilder.Entity<Users>()
            .HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity(j => j.ToTable("users_roles"));

        modelBuilder.Entity<Bans>()
            .HasMany(b => b.Statuses)
            .WithMany()
            .UsingEntity(j => j.ToTable("bans_statuses"));

        modelBuilder.Entity<RestaurantTables>()
            .HasMany(t => t.TableStatus)
            .WithMany()
            .UsingEntity(j => j.ToTable("tables_statuses"));

        modelBuilder.Entity<Reservations>()
            .HasMany(r => r.Statuses)
            .WithMany()
            .UsingEntity(j => j.ToTable("reservations_statuses"));

        modelBuilder.Entity<Orders>()
            .HasMany(o => o.Statuses)
            .WithMany()
            .UsingEntity(j => j.ToTable("orders_statuses"));

        modelBuilder.Entity<OrderItems>()
            .HasMany(oi => oi.Statuses)
            .WithMany()
            .UsingEntity(j => j.ToTable("order_items_statuses"));

        modelBuilder.Entity<Dishes>()
            .HasMany(d => d.Ingredients)
            .WithMany(i => i.Dishes)
            .UsingEntity(j => j.ToTable("dishes_ingredients"));

        modelBuilder.Entity<Ingredients>()
            .HasMany(i => i.Allergens)
            .WithMany(a => a.Ingredients)
            .UsingEntity(j => j.ToTable("ingredients_allergens"));

        modelBuilder.Entity<GuestReports>()
            .HasMany(g => g.Statuses)
            .WithMany()
            .UsingEntity(j => j.ToTable("guest_reports_statuses"));

        modelBuilder.Entity<GuestReports>()
            .HasOne(g => g.Reporter)
            .WithMany(u => u.GuestReports)
            .HasForeignKey(g => g.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GuestReports>()
            .HasOne(g => g.ReportedUser)
            .WithMany()
            .HasForeignKey(g => g.ReportedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Users>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Users>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<VerificationToken>().HasIndex(v => v.Token).IsUnique();
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<Models.Base.BaseEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.OnCreate();
            else if (entry.State == EntityState.Modified) entry.Entity.OnUpdate();
        }
    }
}
