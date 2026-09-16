using System.ComponentModel.DataAnnotations;

namespace VehicleFleetMS.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";
    [StringLength(4)]
    public string Initials { get; set; } = "";
    [Required, StringLength(120), EmailAddress]
    public string Email { get; set; } = "";
    [StringLength(60)]
    public string Username { get; set; } = "";
    [StringLength(30)]
    public string Phone { get; set; } = "";
    [StringLength(200)]
    public string? PhotoPath { get; set; }
    [Required, StringLength(30)]
    public string Role { get; set; } = "Fleet Manager";
    [Required, StringLength(60)]
    public string Department { get; set; } = "";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Invited";
    public DateTime? LastActiveAt { get; set; }

    [Required]
    public string PasswordHash { get; set; } = "";

    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
}

public class OtpCode
{
    public int Id { get; set; }
    public Guid Token { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required, StringLength(6)]
    public string Code { get; set; } = "";
    [Required, StringLength(10)]
    public string Purpose { get; set; } = "login"; // "login" | "reset"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public class ActivityLog
{
    public int Id { get; set; }
    [Required, StringLength(30)]
    public string IconType { get; set; } = "";
    [Required, StringLength(300)]
    public string Text { get; set; } = "";
    [Required, StringLength(80)]
    public string Actor { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class AuditLogItem
{
    public int Id { get; set; }
    [Required, StringLength(80)]
    public string Actor { get; set; } = "";
    [Required, StringLength(200)]
    public string Action { get; set; } = "";
    [Required, StringLength(120)]
    public string Target { get; set; } = "";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    [StringLength(40)]
    public string IpAddress { get; set; } = "—";
}

public class AppNotification
{
    public int Id { get; set; }
    [Required, StringLength(30)]
    public string IconType { get; set; } = "";
    [Required, StringLength(20)]
    public string Severity { get; set; } = "";
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [Required, StringLength(200)]
    public string Detail { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Unread { get; set; } = true;
}

/// <summary>Weekly fleet-wide rollups used to render historical trend charts.</summary>
public class WeeklyStat
{
    public int Id { get; set; }
    [Required, StringLength(10)]
    public string WeekLabel { get; set; } = "";
    public DateOnly WeekStart { get; set; }
    public double DistanceKm { get; set; }
    public double FuelCost { get; set; }
    public double MaintenanceCost { get; set; }
    [Range(0, 100)]
    public double UtilizationPct { get; set; }
}

/// <summary>Single-row table holding agency-wide configuration (Settings module).</summary>
public class AppSettings
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string AgencyName { get; set; } = "Meridian Metro Transit Authority";
    [Required, StringLength(10)]
    public string AgencyShort { get; set; } = "MMTA";
    [Required, StringLength(80)]
    public string DivisionName { get; set; } = "Fleet Operations Division";
    [StringLength(200)]
    public string HeadquartersAddress { get; set; } = "1200 Harborview Drive, Cedar Bay, CA 94612";
    [Required, StringLength(60)]
    public string Timezone { get; set; } = "(UTC-08:00) Pacific Time";
    [Required, StringLength(120), EmailAddress]
    public string ContactEmail { get; set; } = "fleetadmin@mmta.gov";
    [StringLength(30)]
    public string ContactPhone { get; set; } = "(415) 555-0100";
    [StringLength(10)]
    public string BrandAccent { get; set; } = "#0e6e52";
    [StringLength(200)]
    public string? LogoPath { get; set; }

    public int LocationUpdateIntervalSeconds { get; set; } = 15;
    public int OverspeedingThresholdKph { get; set; } = 90;
    public int OfflineAlertTimeoutMinutes { get; set; } = 30;
    [StringLength(60)]
    public string MapProvider { get; set; } = "VMS Maps (default)";
    [StringLength(40)]
    public string DefaultMapCenter { get; set; } = "37.7749, -122.4194";
    public bool GeofenceExitAlerts { get; set; } = true;
    public bool RouteDeviationDetection { get; set; } = true;
    public bool IdleEngineTracking { get; set; }

    public bool RequireTwoFactor { get; set; } = true;
    public bool AgencySso { get; set; } = true;
    public bool AutoSuspendOnSafetyDrop { get; set; } = true;
    [StringLength(20)]
    public string SessionTimeout { get; set; } = "30 minutes";
    [StringLength(20)]
    public string PasswordExpiry { get; set; } = "90 days";
    [StringLength(400)]
    public string IpAllowlist { get; set; } = "10.24.0.0/16";

    public bool NotifyOverspeedingEmail { get; set; } = true;
    public bool NotifyOverspeedingSms { get; set; }
    public bool NotifyOverspeedingPush { get; set; } = true;
    public bool NotifyLowFuelEmail { get; set; } = true;
    public bool NotifyLowFuelSms { get; set; }
    public bool NotifyLowFuelPush { get; set; } = true;
    public bool NotifyMaintenanceEmail { get; set; } = true;
    public bool NotifyMaintenanceSms { get; set; }
    public bool NotifyMaintenancePush { get; set; } = true;
    public bool NotifyOfflineEmail { get; set; } = true;
    public bool NotifyOfflineSms { get; set; } = true;
    public bool NotifyOfflinePush { get; set; } = true;
    public bool NotifyAccidentEmail { get; set; } = true;
    public bool NotifyAccidentSms { get; set; } = true;
    public bool NotifyAccidentPush { get; set; } = true;
    public bool NotifyDocumentExpiryEmail { get; set; } = true;
    public bool NotifyDocumentExpirySms { get; set; }
    public bool NotifyDocumentExpiryPush { get; set; } = true;

    [Required, StringLength(20)]
    public string BackupFrequency { get; set; } = "Daily at 03:00";
    [Required, StringLength(30)]
    public string DataRetentionPeriod { get; set; } = "3 years";
    public DateTime? LastBackupAt { get; set; } = DateTime.UtcNow;

    [Required, StringLength(10)]
    public string ThemeMode { get; set; } = "system";
    [Required, StringLength(15)]
    public string TableDensity { get; set; } = "Comfortable";
}
