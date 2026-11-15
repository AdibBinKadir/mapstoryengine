using System;
using System.Collections.Generic;
using Mapbox.Unity.Location;
using Mapbox.Utils;

namespace Odyssey.Cyclops
{
    internal sealed class CyclopsLocation
    {
        internal const double EntryRadius = 25;
        private const double MaxFixAge = 20;
        private double lastTimestamp;
        private int insideCount;
        private Location latest;

        internal static bool IsUsable(Location fix, double now)
        {
            return fix.IsLocationServiceEnabled && !fix.IsLocationServiceInitializing
                && fix.HasGpsFix != false && fix.Accuracy > 0 && fix.Accuracy <= 25
                && fix.Timestamp > 0 && now >= fix.Timestamp && now - fix.Timestamp <= MaxFixAge
                && Math.Abs(fix.LatitudeLongitude.x) <= 90 && Math.Abs(fix.LatitudeLongitude.y) <= 180;
        }

        internal void Update(Location fix, Vector2d cave, double now)
        {
            latest = fix;
            if (!IsUsable(fix, now) || Distance(fix.LatitudeLongitude, cave) > EntryRadius)
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
            return insideCount == 2 && IsUsable(latest, now);
        }

        internal static double Distance(Vector2d from, Vector2d to)
        {
            const double radians = Math.PI / 180;
            double latitude = (to.x - from.x) * radians;
            double longitude = (to.y - from.y) * radians;
            double a = Math.Pow(Math.Sin(latitude / 2), 2)
                + Math.Cos(from.x * radians) * Math.Cos(to.x * radians)
                * Math.Pow(Math.Sin(longitude / 2), 2);
            return 12742000 * Math.Asin(Math.Sqrt(Math.Min(1, a)));
        }

        internal static List<Vector2d> Candidates(IEnumerable<Vector2d> landmarks,
            IList<Vector2d> storyStops, Vector2d player)
        {
            var result = new List<Vector2d>();
            foreach (Vector2d landmark in landmarks)
            {
                double distance = Distance(player, landmark);
                if (distance < 40 || distance > 250)
                    continue;

                bool overlaps = false;
                foreach (Vector2d stop in storyStops)
                    if (Distance(stop, landmark) <= 55)
                    {
                        overlaps = true;
                        break;
                    }

                if (!overlaps && !result.Contains(landmark))
                    result.Add(landmark);
            }

            return result;
        }
    }
}
