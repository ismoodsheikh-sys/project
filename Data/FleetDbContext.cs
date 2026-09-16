using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Data;

public class FleetDbContext(DbContextOptions<FleetDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<FuelRecord> FuelRecords => Set<FuelRecord>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<AlertItem> Alerts => Set<AlertItem>();
    public DbSet<GeofenceZone> Geofences => Set<GeofenceZone>();
    public DbSet<LiveVehicle> LiveVehicles => Set<LiveVehicle>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<ActivityLog> ActivityLog => Set<ActivityLog>();
    public DbSet<AuditLogItem> AuditLog => Set<AuditLogItem>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<WeeklyStat> WeeklyStats => Set<WeeklyStat>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => v.PlateNumber).IsUnique();
            e.Property(v => v.OdometerKm);
            e.HasOne(v => v.AssignedDriver)
                .WithMany(d => d.AssignedVehicles)
                .HasForeignKey(v => v.AssignedDriverId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(v => v.LiveVehicle)
                .WithOne(l => l.Vehicle)
                .HasForeignKey<LiveVehicle>(l => l.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Driver>(e =>
        {
            e.HasIndex(d => d.LicenseNumber).IsUnique();
            e.HasIndex(d => d.Email).IsUnique();
        });

        modelBuilder.Entity<Trip>(e =>
        {
            e.HasIndex(t => t.TripCode).IsUnique();
            e.HasOne(t => t.Vehicle).WithMany(v => v.Trips).HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Driver).WithMany().HasForeignKey(t => t.DriverId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FuelRecord>(e =>
        {
            e.HasOne(f => f.Vehicle).WithMany(v => v.FuelRecords).HasForeignKey(f => f.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Driver).WithMany().HasForeignKey(f => f.DriverId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MaintenanceRecord>(e =>
        {
            e.HasOne(m => m.Vehicle).WithMany(v => v.MaintenanceRecords).HasForeignKey(m => m.VehicleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AlertItem>(e =>
        {
            e.HasOne(a => a.Vehicle).WithMany(v => v.Alerts).HasForeignKey(a => a.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Driver).WithMany().HasForeignKey(a => a.DriverId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<OtpCode>(e =>
        {
            e.HasIndex(o => o.Token).IsUnique();
            e.HasOne(o => o.User).WithMany(u => u.OtpCodes).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
