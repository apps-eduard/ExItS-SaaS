namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// V1 straight-line (Haversine) distance in kilometers. Must stay aligned with Platform
/// <c>HaversineDeliveryDistanceCalculator</c> for fee preview consistency.
/// </summary>
public static class StraightLineDeliveryDistance
{
    private const double EarthRadiusKm = 6371.0;

    public static decimal CalculateKm(
        decimal latitude1,
        decimal longitude1,
        decimal latitude2,
        decimal longitude2)
    {
        var lat1 = DegreesToRadians((double)latitude1);
        var lat2 = DegreesToRadians((double)latitude2);
        var dLat = DegreesToRadians((double)(latitude2 - latitude1));
        var dLon = DegreesToRadians((double)(longitude2 - longitude1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var km = EarthRadiusKm * c;
        return decimal.Round((decimal)km, 3, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
