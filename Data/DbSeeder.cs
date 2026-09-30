using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleFleetMS.Models;

namespace VehicleFleetMS.Data;

/// <summary>Seeds a fresh database with realistic Meridian Metro Transit Authority fleet data.</summary>
public static class DbSeeder
{
    public const string DemoPassword = "FleetPulse@2026";
    public const string AgencyShort = "MMTA";
    public const string DivisionName = "Fleet Operations Division";

    public static async Task SeedAsync(FleetDbContext db)
    {
        // Each section is guarded independently so that if one table is ever emptied on its
        // own (e.g. every account removed via Users & Roles), a restart repairs just that
        // section instead of silently staying broken because "Vehicles" still has rows.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var vehiclesExist = await db.Vehicles.AnyAsync();
        if (vehiclesExist && !await db.Trips.AnyAsync())
        {
            // A previous seed attempt inserted vehicles/drivers but crashed before finishing
            // (e.g. a data error later in the same batch). Clear the partial state and redo it
            // atomically rather than leaving the fleet data permanently half-seeded.
            db.Vehicles.RemoveRange(db.Vehicles);
            db.Drivers.RemoveRange(db.Drivers);
            await db.SaveChangesAsync();
            vehiclesExist = false;
        }
        if (!vehiclesExist)
        {
            await SeedFleetDataAsync(db);
        }
        if (!await db.Users.AnyAsync())
        {
            await SeedUsersAsync(db);
        }
        if (!await db.Settings.AnyAsync())
        {
            db.Settings.Add(new AppSettings());
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();
    }

    private static async Task SeedFleetDataAsync(FleetDbContext db)
    {
        var today = new DateOnly(2026, 7, 12);

        // ---------- Drivers ----------
        var elena = new Driver { Name = "Elena Cho", Initials = "EC", LicenseNumber = "DL-88213", LicenseClass = "Class B (Transit)", LicenseExpiry = new DateOnly(2027, 3, 9), Phone = "(415) 555-0142", Email = "elena.cho@mmta.gov", Status = "On Trip", TripsCompleted = 1204, TotalDistanceKm = 182300, SafetyScore = 97, Rating = 4.9, YearsExperience = 9, Depot = "Central Garage" };
        var marcus = new Driver { Name = "Marcus Webb", Initials = "MW", LicenseNumber = "DL-77031", LicenseClass = "Class C", LicenseExpiry = new DateOnly(2026, 8, 22), Phone = "(415) 555-0198", Email = "marcus.webb@mmta.gov", Status = "Active", TripsCompleted = 832, TotalDistanceKm = 96500, SafetyScore = 91, Rating = 4.6, YearsExperience = 5, Depot = "North Depot" };
        var priya = new Driver { Name = "Priya Nair", Initials = "PN", LicenseNumber = "DL-65590", LicenseClass = "Class B (Heavy)", LicenseExpiry = new DateOnly(2027, 1, 30), Phone = "(415) 555-0117", Email = "priya.nair@mmta.gov", Status = "On Trip", TripsCompleted = 2011, TotalDistanceKm = 241100, SafetyScore = 95, Rating = 4.8, YearsExperience = 11, Depot = "Harbor Terminal" };
        var sana = new Driver { Name = "Sana Malik", Initials = "SM", LicenseNumber = "DL-90284", LicenseClass = "Class C", LicenseExpiry = new DateOnly(2026, 7, 29), Phone = "(415) 555-0166", Email = "sana.malik@mmta.gov", Status = "Active", TripsCompleted = 560, TotalDistanceKm = 48200, SafetyScore = 88, Rating = 4.4, YearsExperience = 3, Depot = "Central Garage" };
        var dara = new Driver { Name = "Dara Osei", Initials = "DO", LicenseNumber = "DL-71408", LicenseClass = "Class C (Pursuit)", LicenseExpiry = new DateOnly(2027, 5, 17), Phone = "(415) 555-0184", Email = "dara.osei@mmta.gov", Status = "Off Duty", TripsCompleted = 1420, TotalDistanceKm = 163900, SafetyScore = 93, Rating = 4.7, YearsExperience = 7, Depot = "Riverside Yard" };
        var tomas = new Driver { Name = "Tomas Reyes", Initials = "TR", LicenseNumber = "DL-83355", LicenseClass = "Class M", LicenseExpiry = new DateOnly(2026, 9, 3), Phone = "(415) 555-0129", Email = "tomas.reyes@mmta.gov", Status = "On Trip", TripsCompleted = 980, TotalDistanceKm = 71200, SafetyScore = 90, Rating = 4.5, YearsExperience = 4, Depot = "Harbor Terminal" };
        var grace = new Driver { Name = "Grace Kim", Initials = "GK", LicenseNumber = "DL-59902", LicenseClass = "Class B (Heavy)", LicenseExpiry = new DateOnly(2026, 12, 12), Phone = "(415) 555-0175", Email = "grace.kim@mmta.gov", Status = "Active", TripsCompleted = 1755, TotalDistanceKm = 210400, SafetyScore = 96, Rating = 4.9, YearsExperience = 10, Depot = "Harbor Terminal" };
        var liam = new Driver { Name = "Liam Fischer", Initials = "LF", LicenseNumber = "DL-94471", LicenseClass = "Class C", LicenseExpiry = new DateOnly(2027, 6, 25), Phone = "(415) 555-0153", Email = "liam.fischer@mmta.gov", Status = "Active", TripsCompleted = 210, TotalDistanceKm = 15300, SafetyScore = 85, Rating = 4.3, YearsExperience = 1, Depot = "North Depot" };
        var noor = new Driver { Name = "Noor Haddad", Initials = "NH", LicenseNumber = "DL-68820", LicenseClass = "Class B (Transit)", LicenseExpiry = new DateOnly(2026, 7, 18), Phone = "(415) 555-0161", Email = "noor.haddad@mmta.gov", Status = "Suspended", TripsCompleted = 640, TotalDistanceKm = 58900, SafetyScore = 68, Rating = 3.6, YearsExperience = 3, Depot = "Central Garage" };
        var owen = new Driver { Name = "Owen Bright", Initials = "OB", LicenseNumber = "DL-40217", LicenseClass = "Class C", LicenseExpiry = new DateOnly(2027, 2, 4), Phone = "(415) 555-0138", Email = "owen.bright@mmta.gov", Status = "Off Duty", TripsCompleted = 305, TotalDistanceKm = 27400, SafetyScore = 89, Rating = 4.5, YearsExperience = 2, Depot = "Riverside Yard" };
        // Drivers can already exist even when Vehicles is empty (e.g. every vehicle was
        // deleted, cascading out Trips/Fuel/Maintenance, while Drivers was untouched).
        // Reuse existing rows by email instead of re-inserting, or SaveChanges below throws
        // a unique-index violation on Email.
        var existingDrivers = await db.Drivers.ToDictionaryAsync(d => d.Email);
        async Task<Driver> EnsureDriver(Driver seed)
        {
            if (existingDrivers.TryGetValue(seed.Email, out var existing)) return existing;
            db.Drivers.Add(seed);
            return seed;
        }
        elena = await EnsureDriver(elena);
        marcus = await EnsureDriver(marcus);
        priya = await EnsureDriver(priya);
        sana = await EnsureDriver(sana);
        dara = await EnsureDriver(dara);
        tomas = await EnsureDriver(tomas);
        grace = await EnsureDriver(grace);
        liam = await EnsureDriver(liam);
        noor = await EnsureDriver(noor);
        owen = await EnsureDriver(owen);
        await db.SaveChangesAsync();

        // ---------- Vehicles ----------
        var v2201 = new Vehicle { PlateNumber = "MMTA-2201", Make = "Volvo", Model = "7900 Electric", Year = 2023, Category = "Transit Bus", Vin = "YV3R8N02XPA112201", OdometerKm = 48210, FuelType = "Electric", Status = "Active", Depot = "Central Garage", AssignedDriver = elena, InsuranceExpiry = new DateOnly(2026, 11, 4), RegistrationExpiry = new DateOnly(2027, 2, 18), LastServiceDate = new DateOnly(2026, 5, 12), NextServiceDue = new DateOnly(2026, 8, 12), GpsDeviceId = "GPS-7841", HealthScore = 94, Color = "#0e6e52" };
        var v2214 = new Vehicle { PlateNumber = "MMTA-2214", Make = "Ford", Model = "Transit 350", Year = 2021, Category = "Utility Van", Vin = "1FTBW3XM5MKA22214", OdometerKm = 81430, FuelType = "Diesel", Status = "Active", Depot = "North Depot", AssignedDriver = marcus, InsuranceExpiry = new DateOnly(2026, 9, 30), RegistrationExpiry = new DateOnly(2026, 12, 1), LastServiceDate = new DateOnly(2026, 4, 2), NextServiceDue = new DateOnly(2026, 7, 2), GpsDeviceId = "GPS-7842", HealthScore = 88, Color = "#2a78d6" };
        var v2229 = new Vehicle { PlateNumber = "MMTA-2229", Make = "Chevrolet", Model = "Tahoe PPV", Year = 2022, Category = "Patrol SUV", Vin = "1GNSK5KC9NR622229", OdometerKm = 63980, FuelType = "Petrol", Status = "Maintenance", Depot = "Riverside Yard", InsuranceExpiry = new DateOnly(2026, 8, 14), RegistrationExpiry = new DateOnly(2027, 1, 9), LastServiceDate = new DateOnly(2026, 7, 1), NextServiceDue = new DateOnly(2026, 7, 15), GpsDeviceId = "GPS-7843", HealthScore = 61, Color = "#a8660b" };
        var v2233 = new Vehicle { PlateNumber = "MMTA-2233", Make = "Isuzu", Model = "NPR Refuse", Year = 2020, Category = "Refuse Truck", Vin = "JALC4B16X07622233", OdometerKm = 112040, FuelType = "Diesel", Status = "Active", Depot = "Harbor Terminal", AssignedDriver = priya, InsuranceExpiry = new DateOnly(2026, 10, 21), RegistrationExpiry = new DateOnly(2026, 12, 29), LastServiceDate = new DateOnly(2026, 6, 3), NextServiceDue = new DateOnly(2026, 9, 3), GpsDeviceId = "GPS-7844", HealthScore = 76, Color = "#c9631f" };
        var v2241 = new Vehicle { PlateNumber = "MMTA-2241", Make = "Toyota", Model = "Camry Hybrid", Year = 2023, Category = "Sedan", Vin = "4T1G11AK2PU622241", OdometerKm = 22110, FuelType = "Hybrid", Status = "Active", Depot = "Central Garage", AssignedDriver = sana, InsuranceExpiry = new DateOnly(2027, 1, 2), RegistrationExpiry = new DateOnly(2027, 3, 15), LastServiceDate = new DateOnly(2026, 5, 28), NextServiceDue = new DateOnly(2026, 8, 28), GpsDeviceId = "GPS-7845", HealthScore = 97, Color = "#0e6e52" };
        var v2256 = new Vehicle { PlateNumber = "MMTA-2256", Make = "Volvo", Model = "7900 Electric", Year = 2022, Category = "Transit Bus", Vin = "YV3R8N02XPA112256", OdometerKm = 71350, FuelType = "Electric", Status = "Idle", Depot = "Central Garage", InsuranceExpiry = new DateOnly(2026, 9, 9), RegistrationExpiry = new DateOnly(2027, 2, 18), LastServiceDate = new DateOnly(2026, 6, 20), NextServiceDue = new DateOnly(2026, 9, 20), GpsDeviceId = "GPS-7846", HealthScore = 90, Color = "#0e6e52" };
        var v2262 = new Vehicle { PlateNumber = "MMTA-2262", Make = "Freightliner", Model = "M2 106", Year = 2019, Category = "Light Truck", Vin = "1FVACWDT9KHKX2262", OdometerKm = 138760, FuelType = "Diesel", Status = "Out of Service", Depot = "North Depot", InsuranceExpiry = new DateOnly(2026, 7, 20), RegistrationExpiry = new DateOnly(2026, 8, 4), LastServiceDate = new DateOnly(2026, 6, 29), NextServiceDue = new DateOnly(2026, 7, 10), GpsDeviceId = "GPS-7847", HealthScore = 38, Color = "#b23a3a" };
        var v2278 = new Vehicle { PlateNumber = "MMTA-2278", Make = "Ford", Model = "Police Interceptor", Year = 2023, Category = "Patrol SUV", Vin = "1FM5K8AR7PGA22278", OdometerKm = 19870, FuelType = "Petrol", Status = "Active", Depot = "Riverside Yard", AssignedDriver = dara, InsuranceExpiry = new DateOnly(2027, 2, 11), RegistrationExpiry = new DateOnly(2027, 4, 2), LastServiceDate = new DateOnly(2026, 6, 11), NextServiceDue = new DateOnly(2026, 9, 11), GpsDeviceId = "GPS-7848", HealthScore = 95, Color = "#2a78d6" };
        var v2285 = new Vehicle { PlateNumber = "MMTA-2285", Make = "Honda", Model = "CB500X", Year = 2022, Category = "Motorcycle", Vin = "MLHPC4408N5522285", OdometerKm = 14200, FuelType = "Petrol", Status = "Active", Depot = "Harbor Terminal", AssignedDriver = tomas, InsuranceExpiry = new DateOnly(2026, 12, 5), RegistrationExpiry = new DateOnly(2027, 1, 17), LastServiceDate = new DateOnly(2026, 5, 4), NextServiceDue = new DateOnly(2026, 8, 4), GpsDeviceId = "GPS-7849", HealthScore = 91, Color = "#c9631f" };
        var v2291 = new Vehicle { PlateNumber = "MMTA-2291", Make = "Isuzu", Model = "NPR Refuse", Year = 2021, Category = "Refuse Truck", Vin = "JALC4B16X07622291", OdometerKm = 94500, FuelType = "Diesel", Status = "Active", Depot = "Harbor Terminal", AssignedDriver = grace, InsuranceExpiry = new DateOnly(2026, 11, 30), RegistrationExpiry = new DateOnly(2027, 1, 6), LastServiceDate = new DateOnly(2026, 6, 15), NextServiceDue = new DateOnly(2026, 9, 15), GpsDeviceId = "GPS-7850", HealthScore = 82, Color = "#c9631f" };
        var v2304 = new Vehicle { PlateNumber = "MMTA-2304", Make = "Toyota", Model = "Camry Hybrid", Year = 2024, Category = "Sedan", Vin = "4T1G11AK2PU622304", OdometerKm = 8420, FuelType = "Hybrid", Status = "Active", Depot = "North Depot", AssignedDriver = liam, InsuranceExpiry = new DateOnly(2027, 4, 19), RegistrationExpiry = new DateOnly(2027, 6, 1), LastServiceDate = new DateOnly(2026, 4, 30), NextServiceDue = new DateOnly(2026, 10, 30), GpsDeviceId = "GPS-7851", HealthScore = 99, Color = "#0e6e52" };
        var v2318 = new Vehicle { PlateNumber = "MMTA-2318", Make = "Chevrolet", Model = "Tahoe PPV", Year = 2020, Category = "Patrol SUV", Vin = "1GNSK5KC9NR622318", OdometerKm = 104330, FuelType = "Petrol", Status = "Maintenance", Depot = "Riverside Yard", InsuranceExpiry = new DateOnly(2026, 8, 2), RegistrationExpiry = new DateOnly(2026, 10, 14), LastServiceDate = new DateOnly(2026, 6, 28), NextServiceDue = new DateOnly(2026, 7, 20), GpsDeviceId = "GPS-7852", HealthScore = 57, Color = "#a8660b" };
        db.Vehicles.AddRange(v2201, v2214, v2229, v2233, v2241, v2256, v2262, v2278, v2285, v2291, v2304, v2318);
        await db.SaveChangesAsync();

        // ---------- Trips ----------
        db.Trips.AddRange(
            new Trip { TripCode = "TRP-5501", Vehicle = v2201, Driver = elena, Origin = "Central Garage", Destination = "Downtown Loop A", StartTime = new DateTime(2026, 7, 12, 6, 10, 0, DateTimeKind.Utc), DistanceKm = 18.4, Status = "In Progress", Purpose = "Scheduled Transit Route" },
            new Trip { TripCode = "TRP-5502", Vehicle = v2233, Driver = priya, Origin = "Harbor Terminal", Destination = "Sector 9 Collection Loop", StartTime = new DateTime(2026, 7, 12, 5, 30, 0, DateTimeKind.Utc), DistanceKm = 27.1, Status = "In Progress", Purpose = "Waste Collection" },
            new Trip { TripCode = "TRP-5503", Vehicle = v2285, Driver = tomas, Origin = "Harbor Terminal", Destination = "City Hall Annex", StartTime = new DateTime(2026, 7, 12, 8, 0, 0, DateTimeKind.Utc), DistanceKm = 9.8, Status = "In Progress", Purpose = "Courier Run" },
            new Trip { TripCode = "TRP-5498", Vehicle = v2214, Driver = marcus, Origin = "North Depot", Destination = "Riverside Storage", StartTime = new DateTime(2026, 7, 11, 13, 20, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 11, 14, 55, 0, DateTimeKind.Utc), DistanceKm = 41.2, FuelUsedL = 6.1, Status = "Completed", Purpose = "Equipment Transfer" },
            new Trip { TripCode = "TRP-5497", Vehicle = v2241, Driver = sana, Origin = "Central Garage", Destination = "Regional Court", StartTime = new DateTime(2026, 7, 11, 9, 5, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 11, 10, 10, 0, DateTimeKind.Utc), DistanceKm = 15.6, FuelUsedL = 1.2, Status = "Completed", Purpose = "Official Business" },
            new Trip { TripCode = "TRP-5496", Vehicle = v2278, Driver = dara, Origin = "Riverside Yard", Destination = "Patrol Sector 4", StartTime = new DateTime(2026, 7, 11, 7, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 11, 15, 0, 0, DateTimeKind.Utc), DistanceKm = 96.4, FuelUsedL = 11.8, Status = "Completed", Purpose = "Patrol Shift" },
            new Trip { TripCode = "TRP-5495", Vehicle = v2291, Driver = grace, Origin = "Harbor Terminal", Destination = "Sector 3 Collection Loop", StartTime = new DateTime(2026, 7, 11, 5, 30, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 11, 11, 15, 0, DateTimeKind.Utc), DistanceKm = 52.3, FuelUsedL = 19.4, Status = "Completed", Purpose = "Waste Collection" },
            new Trip { TripCode = "TRP-5490", Vehicle = v2304, Driver = liam, Origin = "North Depot", Destination = "Airport Liaison Office", StartTime = new DateTime(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 10, 12, 40, 0, DateTimeKind.Utc), DistanceKm = 38.9, FuelUsedL = 2.6, Status = "Completed", Purpose = "Official Business" },
            new Trip { TripCode = "TRP-5489", Vehicle = v2262, Driver = owen, Origin = "North Depot", Destination = "Depot Yard B", StartTime = new DateTime(2026, 7, 10, 9, 0, 0, DateTimeKind.Utc), Status = "Delayed", Purpose = "Fleet Reposition" },
            new Trip { TripCode = "TRP-5510", Vehicle = v2256, Driver = noor, Origin = "Central Garage", Destination = "Downtown Loop C", StartTime = new DateTime(2026, 7, 13, 6, 0, 0, DateTimeKind.Utc), Status = "Scheduled", Purpose = "Scheduled Transit Route" },
            new Trip { TripCode = "TRP-5511", Vehicle = v2214, Driver = marcus, Origin = "North Depot", Destination = "Central Garage", StartTime = new DateTime(2026, 7, 13, 9, 30, 0, DateTimeKind.Utc), Status = "Scheduled", Purpose = "Equipment Transfer" },
            new Trip { TripCode = "TRP-5485", Vehicle = v2278, Driver = dara, Origin = "Riverside Yard", Destination = "Patrol Sector 2", StartTime = new DateTime(2026, 7, 9, 7, 0, 0, DateTimeKind.Utc), EndTime = new DateTime(2026, 7, 9, 15, 10, 0, DateTimeKind.Utc), DistanceKm = 88.1, FuelUsedL = 10.9, Status = "Cancelled", Purpose = "Patrol Shift" }
        );

        // ---------- Fuel records ----------
        db.FuelRecords.AddRange(
            new FuelRecord { Vehicle = v2233, Driver = priya, Date = new DateOnly(2026, 7, 10), FuelType = "Petrol", Liters = 68.2, CostPerLiter = 1.42, Odometer = 63820, Station = "Central Fuel Depot", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2214, Driver = marcus, Date = new DateOnly(2026, 7, 10), FuelType = "Diesel", Liters = 54.0, CostPerLiter = 1.61, Odometer = 81290, Station = "North Depot Pump", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2262, Date = new DateOnly(2026, 7, 9), FuelType = "Diesel", Liters = 71.5, CostPerLiter = 1.60, Odometer = 138510, Station = "North Depot Pump", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2291, Driver = grace, Date = new DateOnly(2026, 7, 9), FuelType = "Diesel", Liters = 62.8, CostPerLiter = 1.61, Odometer = 94120, Station = "Harbor Terminal Pump", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2278, Driver = dara, Date = new DateOnly(2026, 7, 8), FuelType = "Petrol", Liters = 44.3, CostPerLiter = 1.42, Odometer = 19640, Station = "Riverside Fuel Point", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2285, Driver = tomas, Date = new DateOnly(2026, 7, 8), FuelType = "Petrol", Liters = 11.6, CostPerLiter = 1.44, Odometer = 14040, Station = "Harbor Terminal Pump", PaymentMethod = "Fuel Voucher" },
            new FuelRecord { Vehicle = v2233, Driver = priya, Date = new DateOnly(2026, 7, 6), FuelType = "Petrol", Liters = 66.9, CostPerLiter = 1.41, Odometer = 63410, Station = "Central Fuel Depot", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2318, Date = new DateOnly(2026, 7, 5), FuelType = "Petrol", Liters = 58.4, CostPerLiter = 1.43, Odometer = 104080, Station = "Riverside Fuel Point", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2241, Driver = sana, Date = new DateOnly(2026, 7, 4), FuelType = "Hybrid Petrol", Liters = 21.0, CostPerLiter = 1.45, Odometer = 21860, Station = "Central Fuel Depot", PaymentMethod = "Fleet Card" },
            new FuelRecord { Vehicle = v2291, Driver = grace, Date = new DateOnly(2026, 7, 2), FuelType = "Diesel", Liters = 59.7, CostPerLiter = 1.59, Odometer = 93490, Station = "Harbor Terminal Pump", PaymentMethod = "Fleet Card" }
        );

        // ---------- Maintenance ----------
        db.MaintenanceRecords.AddRange(
            new MaintenanceRecord { Vehicle = v2229, ServiceType = "Brake system overhaul", Category = "Repair", Date = new DateOnly(2026, 7, 1), Cost = 1240.00, Workshop = "Riverside Fleet Workshop", Status = "In Progress", PartsReplaced = "Brake pads, rotors, calipers" },
            new MaintenanceRecord { Vehicle = v2318, ServiceType = "Transmission diagnostic", Category = "Repair", Date = new DateOnly(2026, 6, 28), Cost = 860.00, Workshop = "Riverside Fleet Workshop", Status = "In Progress", PartsReplaced = "Transmission fluid, filter" },
            new MaintenanceRecord { Vehicle = v2262, ServiceType = "Engine inspection — critical", Category = "Inspection", Date = new DateOnly(2026, 6, 29), DueDate = new DateOnly(2026, 7, 10), Cost = 0.00, Workshop = "North Depot Bay 2", Status = "Overdue", PartsReplaced = "—" },
            new MaintenanceRecord { Vehicle = v2201, ServiceType = "Battery pack health check", Category = "Scheduled", Date = new DateOnly(2026, 5, 12), DueDate = new DateOnly(2026, 8, 12), Cost = 210.00, Workshop = "Central Garage EV Bay", Status = "Completed", PartsReplaced = "Coolant top-up" },
            new MaintenanceRecord { Vehicle = v2214, ServiceType = "Oil & filter change", Category = "Scheduled", Date = new DateOnly(2026, 4, 2), DueDate = new DateOnly(2026, 7, 2), Cost = 145.00, Workshop = "North Depot Bay 1", Status = "Completed", PartsReplaced = "Oil filter, engine oil" },
            new MaintenanceRecord { Vehicle = v2233, ServiceType = "Tire rotation & alignment", Category = "Scheduled", Date = new DateOnly(2026, 7, 1), DueDate = new DateOnly(2026, 7, 15), Cost = 180.00, Workshop = "Riverside Fleet Workshop", Status = "Scheduled", PartsReplaced = "—" },
            new MaintenanceRecord { Vehicle = v2233, ServiceType = "AC compressor replacement", Category = "Repair", Date = new DateOnly(2026, 7, 1), Cost = 690.00, Workshop = "Riverside Fleet Workshop", Status = "In Progress", PartsReplaced = "AC compressor, refrigerant" },
            new MaintenanceRecord { Vehicle = v2291, ServiceType = "Hydraulic lift service", Category = "Scheduled", Date = new DateOnly(2026, 6, 15), DueDate = new DateOnly(2026, 9, 15), Cost = 320.00, Workshop = "Harbor Terminal Bay", Status = "Completed", PartsReplaced = "Hydraulic fluid, seals" },
            new MaintenanceRecord { Vehicle = v2285, ServiceType = "Chain & sprocket replacement", Category = "Repair", Date = new DateOnly(2026, 5, 4), Cost = 95.00, Workshop = "Harbor Terminal Bay", Status = "Completed", PartsReplaced = "Drive chain, sprocket set" },
            new MaintenanceRecord { Vehicle = v2278, ServiceType = "Annual safety inspection", Category = "Inspection", Date = new DateOnly(2026, 6, 11), DueDate = new DateOnly(2026, 9, 11), Cost = 60.00, Workshop = "Riverside Fleet Workshop", Status = "Completed", PartsReplaced = "—" },
            new MaintenanceRecord { Vehicle = v2256, ServiceType = "Battery pack health check", Category = "Scheduled", Date = new DateOnly(2026, 6, 20), DueDate = new DateOnly(2026, 9, 20), Cost = 210.00, Workshop = "Central Garage EV Bay", Status = "Completed", PartsReplaced = "—" },
            new MaintenanceRecord { Vehicle = v2304, ServiceType = "First 10,000km service", Category = "Scheduled", Date = new DateOnly(2026, 4, 30), DueDate = new DateOnly(2026, 10, 30), Cost = 130.00, Workshop = "North Depot Bay 1", Status = "Completed", PartsReplaced = "Oil filter, cabin filter" }
        );

        // ---------- Alerts ----------
        db.Alerts.AddRange(
            new AlertItem { Vehicle = v2278, Driver = dara, Type = "Overspeeding", Severity = "Critical", Message = "Sustained 118 km/h in a 90 km/h zone for 40s", Timestamp = new DateTime(2026, 7, 12, 9, 42, 0, DateTimeKind.Utc), Status = "New", Location = "Route 9, Patrol Sector 4" },
            new AlertItem { Vehicle = v2214, Driver = marcus, Type = "Accident", Severity = "Critical", Message = "Airbag deployment signal received from telematics unit", Timestamp = new DateTime(2026, 7, 12, 8, 15, 0, DateTimeKind.Utc), Status = "Acknowledged", Location = "Junction of 5th & Alder" },
            new AlertItem { Vehicle = v2262, Type = "Offline", Severity = "Serious", Message = "GPS device unresponsive for 3h 20m", Timestamp = new DateTime(2026, 7, 12, 6, 5, 0, DateTimeKind.Utc), Status = "New", Location = "North Depot" },
            new AlertItem { Vehicle = v2262, Type = "MaintenanceDue", Severity = "Serious", Message = "Critical engine inspection overdue by 2 days", Timestamp = new DateTime(2026, 7, 10, 7, 0, 0, DateTimeKind.Utc), Status = "New", Location = "North Depot" },
            new AlertItem { Vehicle = v2262, Type = "DocumentExpiry", Severity = "Warning", Message = "Registration expires in 25 days", Timestamp = new DateTime(2026, 7, 10, 6, 0, 0, DateTimeKind.Utc), Status = "Acknowledged", Location = "North Depot" },
            new AlertItem { Vehicle = v2233, Driver = priya, Type = "LowFuel", Severity = "Warning", Message = "Fuel level at 11% — below reserve threshold", Timestamp = new DateTime(2026, 7, 12, 7, 55, 0, DateTimeKind.Utc), Status = "New", Location = "Sector 9 Collection Loop" },
            new AlertItem { Vehicle = v2285, Driver = tomas, Type = "GeofenceExit", Severity = "Warning", Message = "Exited approved courier corridor geofence", Timestamp = new DateTime(2026, 7, 12, 8, 30, 0, DateTimeKind.Utc), Status = "Resolved", Location = "City Hall Annex" },
            new AlertItem { Vehicle = v2233, Driver = priya, Type = "Overspeeding", Severity = "Serious", Message = "102 km/h in a 70 km/h collection route", Timestamp = new DateTime(2026, 7, 11, 14, 2, 0, DateTimeKind.Utc), Status = "Resolved", Location = "Sector 3 Collection Loop" },
            new AlertItem { Vehicle = v2233, Driver = priya, Type = "MaintenanceDue", Severity = "Warning", Message = "Tire rotation due within 7 days", Timestamp = new DateTime(2026, 7, 9, 9, 0, 0, DateTimeKind.Utc), Status = "Acknowledged", Location = "Riverside Yard" },
            new AlertItem { Vehicle = v2291, Driver = grace, Type = "LowFuel", Severity = "Good", Message = "Refuelled — fuel level restored to 96%", Timestamp = new DateTime(2026, 7, 9, 6, 10, 0, DateTimeKind.Utc), Status = "Resolved", Location = "Harbor Terminal Pump" }
        );

        // ---------- Geofences ----------
        db.Geofences.AddRange(
            new GeofenceZone { Name = "Downtown Restricted Corridor", Type = "Restricted", VehiclesInside = 2, Left = 52, Top = 22, Width = 26, Height = 22 },
            new GeofenceZone { Name = "Harbor Terminal Depot", Type = "Depot", VehiclesInside = 4, Left = 14, Top = 58, Width = 20, Height = 18 },
            new GeofenceZone { Name = "Riverside Customer Site", Type = "Customer", VehiclesInside = 1, Left = 68, Top = 62, Width = 18, Height = 16 }
        );

        // ---------- Live vehicles ----------
        db.LiveVehicles.AddRange(
            new LiveVehicle { Vehicle = v2201, Left = 61, Top = 34, SpeedKph = 32, Status = "Active", Heading = "NE", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2233, Left = 20, Top = 63, SpeedKph = 18, Status = "Active", Heading = "S", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2285, Left = 55, Top = 30, SpeedKph = 27, Status = "Active", Heading = "E", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2278, Left = 76, Top = 44, SpeedKph = 118, Status = "Overspeeding", Heading = "NE", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2214, Left = 39, Top = 48, SpeedKph = 0, Status = "Alert", Heading = "—", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2291, Left = 17, Top = 52, SpeedKph = 22, Status = "Active", Heading = "SW", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2262, Left = 25, Top = 30, SpeedKph = 0, Status = "Offline", Heading = "—", LastUpdated = DateTime.UtcNow },
            new LiveVehicle { Vehicle = v2256, Left = 44, Top = 20, SpeedKph = 0, Status = "Idle", Heading = "—", LastUpdated = DateTime.UtcNow }
        );

        // ---------- Audit log ----------
        db.AuditLog.AddRange(
            new AuditLogItem { Actor = "Alicia Ferreira", Action = "Updated role permissions", Target = "Role: Dispatcher", OccurredAt = new DateTime(2026, 7, 12, 9, 14, 0, DateTimeKind.Utc), IpAddress = "10.24.3.11" },
            new AuditLogItem { Actor = "Ben Okafor", Action = "Approved maintenance request", Target = "MMTA-2229 · M-3301", OccurredAt = new DateTime(2026, 7, 12, 8, 2, 0, DateTimeKind.Utc), IpAddress = "10.24.3.44" },
            new AuditLogItem { Actor = "System", Action = "Auto-suspended driver — safety score below threshold", Target = "Noor Haddad", OccurredAt = new DateTime(2026, 7, 11, 22, 10, 0, DateTimeKind.Utc), IpAddress = "—" },
            new AuditLogItem { Actor = "Carmen Ruiz", Action = "Reassigned vehicle to depot", Target = "MMTA-2262 → North Depot", OccurredAt = new DateTime(2026, 7, 11, 16, 47, 0, DateTimeKind.Utc), IpAddress = "10.24.3.19" },
            new AuditLogItem { Actor = "Ingrid Sørensen", Action = "Exported compliance report", Target = "Q2 Fleet Utilization.pdf", OccurredAt = new DateTime(2026, 7, 11, 14, 20, 0, DateTimeKind.Utc), IpAddress = "10.24.4.02" },
            new AuditLogItem { Actor = "Henry Walsh", Action = "Acknowledged critical alert", Target = "MMTA-2214 accident alert", OccurredAt = new DateTime(2026, 7, 12, 8, 16, 0, DateTimeKind.Utc), IpAddress = "10.24.3.28" },
            new AuditLogItem { Actor = "Alicia Ferreira", Action = "Invited new user", Target = "marcus.webb@mmta.gov", OccurredAt = new DateTime(2026, 7, 10, 11, 5, 0, DateTimeKind.Utc), IpAddress = "10.24.3.11" }
        );

        // ---------- Recent activity ----------
        db.ActivityLog.AddRange(
            new ActivityLog { IconType = "alert-triangle", Text = "Critical overspeeding alert raised for MMTA-2278", Actor = "System", OccurredAt = DateTime.UtcNow.AddMinutes(-2) },
            new ActivityLog { IconType = "wrench", Text = "Maintenance completed on MMTA-2291 — hydraulic lift service", Actor = "Harbor Terminal Bay", OccurredAt = DateTime.UtcNow.AddMinutes(-41) },
            new ActivityLog { IconType = "fuel", Text = "Fuel record logged for MMTA-2214 — 54.0 L Diesel", Actor = "Marcus Webb", OccurredAt = DateTime.UtcNow.AddHours(-1) },
            new ActivityLog { IconType = "route", Text = "Trip TRP-5498 completed — 41.2 km, Equipment Transfer", Actor = "Marcus Webb", OccurredAt = DateTime.UtcNow.AddHours(-1) },
            new ActivityLog { IconType = "driver", Text = "Driver Noor Haddad suspended — safety score review", Actor = "System", OccurredAt = DateTime.UtcNow.AddHours(-3) },
            new ActivityLog { IconType = "file-text", Text = "Insurance document renewed for MMTA-2201", Actor = "Ben Okafor", OccurredAt = DateTime.UtcNow.AddHours(-5) },
            new ActivityLog { IconType = "vehicle", Text = "New vehicle MMTA-2318 added to Riverside Yard", Actor = "Carmen Ruiz", OccurredAt = DateTime.UtcNow.AddDays(-1) }
        );

        // ---------- Notifications ----------
        db.Notifications.AddRange(
            new AppNotification { IconType = "alert-triangle", Severity = "Critical", Title = "Overspeeding — MMTA-2278", Detail = "118 km/h in a 90 km/h zone, Route 9", CreatedAt = DateTime.UtcNow.AddMinutes(-2), Unread = true },
            new AppNotification { IconType = "alert-octagon", Severity = "Critical", Title = "Possible accident — MMTA-2214", Detail = "Airbag deployment signal received", CreatedAt = DateTime.UtcNow.AddHours(-1), Unread = true },
            new AppNotification { IconType = "wrench", Severity = "Serious", Title = "Maintenance overdue — MMTA-2262", Detail = "Critical engine inspection, 2 days overdue", CreatedAt = DateTime.UtcNow.AddHours(-2), Unread = true },
            new AppNotification { IconType = "fuel", Severity = "Warning", Title = "Low fuel — MMTA-2233", Detail = "Fuel level at 11%, below reserve", CreatedAt = DateTime.UtcNow.AddHours(-4), Unread = false },
            new AppNotification { IconType = "file-text", Severity = "Warning", Title = "Registration expiring — MMTA-2262", Detail = "Expires in 25 days", CreatedAt = DateTime.UtcNow.AddDays(-1), Unread = false },
            new AppNotification { IconType = "check-circle", Severity = "Good", Title = "Refuel confirmed — MMTA-2291", Detail = "Fuel level restored to 96%", CreatedAt = DateTime.UtcNow.AddDays(-3), Unread = false }
        );

        // ---------- Weekly stats (historical trend series) ----------
        db.WeeklyStats.AddRange(
            new WeeklyStat { WeekLabel = "Wk22", WeekStart = new DateOnly(2026, 5, 25), DistanceKm = 91200, FuelCost = 4120, MaintenanceCost = 2100, UtilizationPct = 74 },
            new WeeklyStat { WeekLabel = "Wk23", WeekStart = new DateOnly(2026, 6, 1), DistanceKm = 94800, FuelCost = 4360, MaintenanceCost = 1850, UtilizationPct = 76 },
            new WeeklyStat { WeekLabel = "Wk24", WeekStart = new DateOnly(2026, 6, 8), DistanceKm = 98100, FuelCost = 3980, MaintenanceCost = 2620, UtilizationPct = 79 },
            new WeeklyStat { WeekLabel = "Wk25", WeekStart = new DateOnly(2026, 6, 15), DistanceKm = 96500, FuelCost = 4510, MaintenanceCost = 1920, UtilizationPct = 77 },
            new WeeklyStat { WeekLabel = "Wk26", WeekStart = new DateOnly(2026, 6, 22), DistanceKm = 101400, FuelCost = 4270, MaintenanceCost = 2340, UtilizationPct = 81 },
            new WeeklyStat { WeekLabel = "Wk27", WeekStart = new DateOnly(2026, 6, 29), DistanceKm = 104200, FuelCost = 4680, MaintenanceCost = 3100, UtilizationPct = 83 },
            new WeeklyStat { WeekLabel = "Wk28", WeekStart = new DateOnly(2026, 7, 6), DistanceKm = 99700, FuelCost = 4430, MaintenanceCost = 2780, UtilizationPct = 80 },
            new WeeklyStat { WeekLabel = "Wk29", WeekStart = new DateOnly(2026, 7, 13), DistanceKm = 97300, FuelCost = 4390, MaintenanceCost = 1990, UtilizationPct = 78.4 }
        );

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(FleetDbContext db)
    {
        var hasher = new PasswordHasher<AppUser>();
        AppUser MakeUser(string name, string initials, string email, string role, string dept, string status, DateTime? lastActive)
        {
            var u = new AppUser { Name = name, Initials = initials, Email = email, Username = email[..email.IndexOf('@')], Role = role, Department = dept, Status = status, LastActiveAt = lastActive };
            u.PasswordHash = hasher.HashPassword(u, DemoPassword);
            return u;
        }
        db.Users.AddRange(
            MakeUser("Alicia Ferreira", "AF", "alicia.ferreira@mmta.gov", "System Administrator", "IT & Systems", "Active", DateTime.UtcNow.AddMinutes(-3)),
            MakeUser("Ben Okafor", "BO", "ben.okafor@mmta.gov", "Fleet Manager", "Fleet Operations", "Active", DateTime.UtcNow.AddMinutes(-18)),
            MakeUser("Carmen Ruiz", "CR", "carmen.ruiz@mmta.gov", "Fleet Manager", "Fleet Operations", "Active", DateTime.UtcNow.AddHours(-1)),
            MakeUser("Elena Cho", "EC", "elena.cho@mmta.gov", "Driver", "Transit Operations", "Active", DateTime.UtcNow),
            MakeUser("Priya Nair", "PN", "priya.nair@mmta.gov", "Driver", "Sanitation", "Active", DateTime.UtcNow),
            MakeUser("Henry Walsh", "HW", "henry.walsh@mmta.gov", "Dispatcher", "Operations Control", "Active", DateTime.UtcNow.AddMinutes(-6)),
            MakeUser("Noor Haddad", "NH", "noor.haddad@mmta.gov", "Driver", "Transit Operations", "Suspended", DateTime.UtcNow.AddDays(-4)),
            MakeUser("Ingrid Sørensen", "IS", "ingrid.sorensen@mmta.gov", "Auditor", "Compliance", "Active", DateTime.UtcNow.AddHours(-2)),
            MakeUser("Marcus Webb", "MW", "marcus.webb@mmta.gov", "Driver", "Fleet Operations", "Invited", null)
        );
        await db.SaveChangesAsync();
    }
}
