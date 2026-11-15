using System;
using Mapbox.Unity.Location;
using Mapbox.Utils;

namespace Odyssey.Cyclops
{
    internal sealed class CyclopsLocation
    {
        internal const double EntryRadius = 25;
        private const double EarthRadius = 6371000;
        private const double Radians = Math.PI / 180;
        private const double MaxFixAge = 20;
        private readonly double entryRadius;
        private readonly double maxAccuracy;
        private double lastTimestamp;
        private int insideCount;
        private Location latest;

        internal CyclopsLocation(double entryRadius = EntryRadius, double maxAccuracy = 25)
        {
            this.entryRadius = entryRadius;
            this.maxAccuracy = maxAccuracy;
        }

        internal static bool IsUsable(Location fix, double now, double maxAccuracy = 25)
        {
            return fix.IsLocationServiceEnabled && !fix.IsLocationServiceInitializing
                && fix.HasGpsFix != false && fix.Accuracy > 0 && fix.Accuracy <= maxAccuracy
                && fix.Timestamp > 0 && now >= fix.Timestamp && now - fix.Timestamp <= MaxFixAge
                && Math.Abs(fix.LatitudeLongitude.x) <= 90 && Math.Abs(fix.LatitudeLongitude.y) <= 180;
        }

        internal void Update(Location fix, Vector2d cave, double now)
        {
            latest = fix;
            if (!IsUsable(fix, now, maxAccuracy) || Distance(fix.LatitudeLongitude, cave) > entryRadius)
            {
                insideCount = 0;
                lastTimestamp = fix.Timestamp;
                return;
            }

            if (fix.Timestamp < lastTimestamp || fix.Timestamp - lastTimestamp > MaxFixAge)
                insideCount = 0;

            if (fix.Timestamp > lastTimestamp)
                insideCount = Math.Min(insideCount + 1, 2);

            lastTimestamp = fix.Timestamp;
        }

        internal bool CanEnter(double now)
        {
            return insideCount == 2 && IsUsable(latest, now, maxAccuracy);
        }

        internal void Reset()
        {
            insideCount = 0;
            lastTimestamp = 0;
        }

        internal static double Distance(Vector2d from, Vector2d to)
        {
            double latitude = (to.x - from.x) * Radians;
            double longitude = (to.y - from.y) * Radians;
            double a = Math.Pow(Math.Sin(latitude / 2), 2)
                + Math.Cos(from.x * Radians) * Math.Cos(to.x * Radians)
                * Math.Pow(Math.Sin(longitude / 2), 2);
            return 2 * EarthRadius * Math.Asin(Math.Sqrt(Math.Min(1, a)));
        }

        internal static Vector2d Offset(Vector2d origin, double meters, double bearingDegrees)
        {
            double latitude = origin.x * Radians;
            double bearing = bearingDegrees * Radians;
            double angle = meters / EarthRadius;
            double targetLatitude = Math.Asin(Math.Sin(latitude) * Math.Cos(angle)
                + Math.Cos(latitude) * Math.Sin(angle) * Math.Cos(bearing));
            double longitudeDelta = Math.Atan2(Math.Sin(bearing) * Math.Sin(angle) * Math.Cos(latitude),
                Math.Cos(angle) - Math.Sin(latitude) * Math.Sin(targetLatitude));
            double longitude = (origin.y + longitudeDelta / Radians + 540) % 360 - 180;
            return new Vector2d(targetLatitude / Radians, longitude);
        }
    }
}
