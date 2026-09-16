using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleFleetMS.Models;

public class FuelRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public DateOnly Date { get; set; }
    [Required, StringLength(30)]
    public string FuelType { get; set; } = "";
    public double Liters { get; set; }
    public double CostPerLiter { get; set; }
    [NotMapped]
    public double TotalCost => Math.Round(Liters * CostPerLiter, 2);
    public int Odometer { get; set; }
    [Required, StringLength(60)]
    public string Station { get; set; } = "";
    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = "";
}

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    [Required, StringLength(120)]
    public string ServiceType { get; set; } = "";
    [Required, StringLength(20)]
    public string Category { get; set; } = "";
    public DateOnly Date { get; set; }
    public DateOnly? DueDate { get; set; }
    public double Cost { get; set; }
    [Required, StringLength(80)]
    public string Workshop { get; set; } = "";
    [Required, StringLength(20)]
    public string Status { get; set; } = "Scheduled";
    [StringLength(200)]
    public string PartsReplaced { get; set; } = "";
}

public class AlertItem
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? DriverId { get; set; }
    public Driver? Driver { get; set; }

    [Required, StringLength(30)]
    public string Type { get; set; } = "";
    [Required, StringLength(20)]
    public string Severity { get; set; } = "";
    [Required, StringLength(300)]
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    [Required, StringLength(20)]
    public string Status { get; set; } = "New";
    [StringLength(100)]
    public string Location { get; set; } = "";
}

public class GeofenceZone
{
    public int Id { get; set; }
    [Required, StringLength(80)]
    public string Name { get; set; } = "";
    [Required, StringLength(20)]
    public string Type { get; set; } = "";
    public int VehiclesInside { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class LiveVehicle
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public double Left { get; set; }
    public double Top { get; set; }
    public double SpeedKph { get; set; }
    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
    [StringLength(6)]
    public string Heading { get; set; } = "";
    public DateTime LastUpdated { get; set; }
}
