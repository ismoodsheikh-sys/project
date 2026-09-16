namespace VehicleFleetMS.Helpers;

public static class OtpHelper
{
    public static string GenerateCode() => Random.Shared.Next(0, 1_000_000).ToString("D6");
}
