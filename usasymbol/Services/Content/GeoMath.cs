namespace USASymbol.Services.Content
{
    public static class GeoMath
    {
        public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadiusKm = 6371.0;
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }

        public static double HaversineMiles(double lat1, double lon1, double lat2, double lon2) =>
            HaversineKm(lat1, lon1, lat2, lon2) * 0.621371;

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;
    }
}
