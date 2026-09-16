using System.ComponentModel.DataAnnotations;

namespace VehicleFleetMS.Models;

public class Vehicle
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string PlateNumber { get; set; } = "";
    [Required, StringLength(40)]
    public string Make { get; set; } = "";
    [Required, StringLength(60)]
    public string Model { get; set; } = "";
    [Range(1980, 2100)]
    public int Year { get; set; }
    [Required, StringLength(40)]
    public string Category { get; set; } = "";
    [StringLength(24)]
    public string Vin { get; set; } = "";
    public int OdometerKm { get; set; }
    [Required, StringLength(20)]
    public string FuelType { get; set; } = "";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
    [Required, StringLength(60)]
    public string Depot { get; set; } = "";

    public int? AssignedDriverId { get; set; }
    public Driver? AssignedDriver { get; set; }

    public DateOnly InsuranceExpiry { get; set; }
    public DateOnly RegistrationExpiry { get; set; }
    public DateOnly LastServiceDate { get; set; }
    public DateOnly NextServiceDue { get; set; }
    [StringLength(20)]
    public string GpsDeviceId { get; set; } = "";
    [Range(0, 100)]
    public int HealthScore { get; set; }
    [StringLength(10)]
    public string Color { get; set; } = "";

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<FuelRecord> FuelRecords { get; set; } = new List<FuelRecord>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public ICollection<AlertItem> Alerts { get; set; } = new List<AlertItem>();
    public LiveVehicle? LiveVehicle { get; set; }
}

public class Driver
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";
    [StringLength(4)]
    public string Initials { get; set; } = "";
    [Required, StringLength(20)]
    public string LicenseNumber { get; set; } = "";
    [Required, StringLength(30)]
    public string LicenseClass { get; set; } = "";
    public DateOnly LicenseExpiry { get; set; }
    [StringLength(30)]
    public string Phone { get; set; } = "";
    [Required, StringLength(120), EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
    public int TripsCompleted { get; set; }
    public int TotalDistanceKm { get; set; }
    [Range(0, 100)]
    public double SafetyScore { get; set; } = 85;
    [Range(0, 5)]
    public double Rating { get; set; } = 4.5;
    public int YearsExperience { get; set; }
    [Required, StringLength(60)]
    public string Depot { get; set; } = "";

    public ICollection<Vehicle> AssignedVehicles { get; set; } = new List<Vehicle>();
}

public class Trip
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string TripCode { get; set; } = "";
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    [Required, StringLength(80)]
    public string Origin { get; set; } = "";
    [Required, StringLength(80)]
    public string Destination { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double DistanceKm { get; set; }
    public double FuelUsedL { get; set; }
    [Required, StringLength(20)]
    public string Status { get; set; } = "Scheduled";
    [StringLength(80)]
    public string Purpose { get; set; } = "";
    [StringLength(400)]
    public string? Notes { get; set; }
}
