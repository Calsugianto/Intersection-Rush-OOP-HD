using System;
using System.Collections.Generic;
using SplashKitSDK;

namespace IntersectionRush
{
    // One intersection: four approaches, one per compass direction, all
    // sharing a single traffic-light controller. The controller can be any
    // concrete TrafficLightController - Intersection only ever calls the
    // abstract members, so swapping Fixed/Adaptive/Manual in and out never
    // changes a single line of this class.
    public class Intersection
    {
        // Half the width/height of the intersection box itself, in pixels.
        public const double HALF_SIZE = 25;

        // How thick the road is drawn. Kept equal to 2 * HALF_SIZE so the
        // approach roads and the intersection box line up as one continuous
        // width, and comfortably wider than any vehicle sprite (10px) so a
        // vehicle is never drawn overhanging the road's edge.
        private const double ROAD_WIDTH = 50;

        // How much extra road is drawn past the point where a vehicle
        // actually spawns, so a vehicle appears just inside the visible road
        // rather than exactly on its pixel edge.
        private const double ROAD_END_MARGIN = 20;

        private readonly Dictionary<Direction, Approach> _approaches = new Dictionary<Direction, Approach>();
        private readonly TrafficLightController _controller;

        public string Label { get; }
        public TrafficLightController Controller => _controller;

        public int VehiclesPassed { get; private set; }
        public double TotalWaitSeconds { get; private set; }

        // A computed property, never stored directly - always derived from the
        // two counters above, so it can never fall out of sync with them.
        public double AverageWaitSeconds => VehiclesPassed == 0 ? 0 : TotalWaitSeconds / VehiclesPassed;

        public bool IsGridlocked
        {
            get
            {
                foreach (Approach approach in _approaches.Values)
                {
                    if (approach.IsGridlocked) return true;
                }
                return false;
            }
        }

        public Intersection(string label, TrafficLightController controller)
        {
            Label = label;
            _controller = controller;

            foreach (Direction d in Enum.GetValues(typeof(Direction)))
            {
                _approaches[d] = new Approach(d, controller);
            }
        }

        public void TrySpawn(Direction direction, Vehicle vehicle)
        {
            _approaches[direction].Spawn(vehicle);
        }

        public void Update(double deltaSeconds, double currentTime)
        {
            var queueLengths = new Dictionary<Direction, int>();
            foreach (var pair in _approaches)
            {
                queueLengths[pair.Key] = pair.Value.QueueLength;
            }

            _controller.Update(deltaSeconds, queueLengths);

            foreach (Approach approach in _approaches.Values)
            {
                Vehicle passed = approach.Update(deltaSeconds);
                if (passed != null)
                {
                    VehiclesPassed++;
                    TotalWaitSeconds += currentTime - passed.SpawnTime;
                }
            }
        }

        public void Draw(double centerX, double centerY)
        {
            // How far the road extends either side of the intersection box.
            // A vehicle spawns at HALF_SIZE + STOP_LINE_DISTANCE from centre -
            // that's the biggest distance either end of the road actually
            // needs (further than the smaller gap a vehicle leaves on the
            // exit side before it disappears), so the road is sized against
            // that, plus a small margin so the spawn point sits just inside
            // the road's edge rather than exactly on it.
            double roadHalfLength = HALF_SIZE + Approach.STOP_LINE_DISTANCE + ROAD_END_MARGIN;
            double roadHalfWidth = ROAD_WIDTH / 2;

            SplashKit.FillRectangle(Color.RGBColor(60, 60, 60),
                centerX - roadHalfLength, centerY - roadHalfWidth, roadHalfLength * 2, ROAD_WIDTH);
            SplashKit.FillRectangle(Color.RGBColor(60, 60, 60),
                centerX - roadHalfWidth, centerY - roadHalfLength, ROAD_WIDTH, roadHalfLength * 2);
            SplashKit.FillRectangle(Color.RGBColor(80, 80, 80),
                centerX - HALF_SIZE, centerY - HALF_SIZE, HALF_SIZE * 2, HALF_SIZE * 2);

            foreach (Approach approach in _approaches.Values)
            {
                approach.Draw(centerX, centerY, HALF_SIZE);
            }

            DrawLight(centerX, centerY - HALF_SIZE - 14, Direction.North);
            DrawLight(centerX, centerY + HALF_SIZE + 14, Direction.South);
            DrawLight(centerX + HALF_SIZE + 14, centerY, Direction.East);
            DrawLight(centerX - HALF_SIZE - 14, centerY, Direction.West);

            SplashKit.DrawText(Label, Color.White, centerX - Label.Length * 4, centerY - roadHalfLength - 20);
            SplashKit.DrawText($"Avg wait: {AverageWaitSeconds:F1}s", Color.White,
                centerX - 55, centerY + roadHalfLength + 10);
        }

        private void DrawLight(double x, double y, Direction direction)
        {
            Color c = _controller.IsGreenFor(direction)
                ? Color.Lime
                : (_controller.IsYellowFor(direction) ? Color.Yellow : Color.Red);
            SplashKit.FillCircle(c, x, y, 6);
        }
    }
}