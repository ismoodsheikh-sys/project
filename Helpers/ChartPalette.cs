namespace VehicleFleetMS.Helpers;

/// <summary>Fixed category → chart-hue assignment, shared by Dashboard and Reports so the same
/// vehicle category always reads the same color across the app.</summary>
public static class ChartPalette
{
    public static string BucketCategory(string category) => category switch
    {
        "Transit Bus" => "Transit Bus",
        "Utility Van" => "Utility Van",
        "Patrol SUV" => "Patrol SUV",
        "Refuse Truck" => "Refuse Truck",
        _ => "Other",
    };

    public static string ColorFor(string bucketedCategory) => bucketedCategory switch
    {
        "Transit Bus" => "var(--chart-1)",
        "Utility Van" => "var(--chart-2)",
        "Patrol SUV" => "var(--chart-3)",
        "Other" => "var(--chart-4)",
        "Refuse Truck" => "var(--chart-5)",
        _ => "var(--chart-4)",
    };

    public static readonly string[] CategoryOrder = ["Transit Bus", "Refuse Truck", "Patrol SUV", "Utility Van", "Other"];
}
