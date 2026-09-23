using System.Collections.Generic;
using SplashKitSDK;

namespace IntersectionRush
{
    // The four compass directions traffic can approach an intersection from.
    public enum Direction { North, South, East, West }

    // One lane of traffic approaching an intersection from a single direction.
    // Vehicles are held in a Queue<Vehicle> in the order they arrived - a new
    // vehicle joins the back, and only the vehicle at the very front can ever
    // reach the stop line and leave the queue. This mirrors a real queue of
    // traffic exactly: first in, first out.
    public class Approach
    {
        // Distance (in pixels) from where a vehicle spawns to the stop line.
        // Public because Intersection uses the same figure to draw the road.
        public const double STOP_LINE_DISTANCE = 160;

        // If this many vehicles are waiting at once, the approach is gridlocked.
        private const int MAX_QUEUE_LENGTH = 8;

        private readonly Queue<Vehicle> _queue = new Queue<Vehicle>();
        private readonly TrafficLightController _light;

        public Direction Direction { get; }
        public int QueueLength => _queue.Count;
        public bool IsGridlocked => _queue.Count >= MAX_QUEUE_LENGTH;

        public Approach(Direction direction, TrafficLightController light)
        {
            Direction = direction;
            _light = light;
        }

        public void Spawn(Vehicle vehicle)
        {
            _queue.Enqueue(vehicle);
        }

        // Moves every queued vehicle for one frame (front to back), then lets the
        // front vehicle leave the queue once it has reached the stop line.
        // Returns the vehicle that just passed through, or null if none did.
        //
        // Reading every vehicle with foreach and only ever removing the single
        // front one afterwards - never during the foreach itself - is the same
        // "don't mutate a collection while a foreach is reading it" rule from
        // my Something Awesome video, just applied to a Queue instead of a List.
        public Vehicle Update(double deltaSeconds)
        {
            Vehicle previous = null;

            foreach (Vehicle v in _queue)
            {
                double gap;

                if (previous == null)
                {
                    // Front of the queue: the obstacle ahead is the stop line -
                    // unless the light is green, in which case there's nothing
                    // in the way at all.
                    gap = _light.IsGreenFor(Direction) ? 10000 : STOP_LINE_DISTANCE - v.Position;
                }
                else
                {
                    // Not at the front: the obstacle ahead is the back bumper of
                    // the vehicle in front of it in the queue.
                    gap = previous.Position - previous.Length - v.Position;
                }

                v.UpdatePhysics(deltaSeconds, gap);
                previous = v;
            }

            if (_queue.Count > 0 && _queue.Peek().Position >= STOP_LINE_DISTANCE)
            {
                return _queue.Dequeue();
            }

            return null;
        }

        public void Draw(double centerX, double centerY, double halfSize)
        {
            bool horizontal = Direction == Direction.East || Direction == Direction.West;
            const double LANE_OFFSET = 12;

            foreach (Vehicle v in _queue)
            {
                double x, y;

                switch (Direction)
                {
                    case Direction.North:
                        x = centerX - LANE_OFFSET;
                        y = (centerY - halfSize - STOP_LINE_DISTANCE) + v.Position;
                        break;
                    case Direction.South:
                        x = centerX + LANE_OFFSET;
                        y = (centerY + halfSize + STOP_LINE_DISTANCE) - v.Position;
                        break;
                    case Direction.East:
                        x = (centerX + halfSize + STOP_LINE_DISTANCE) - v.Position;
                        y = centerY - LANE_OFFSET;
                        break;
                    default: // West
                        x = (centerX - halfSize - STOP_LINE_DISTANCE) + v.Position;
                        y = centerY + LANE_OFFSET;
                        break;
                }

                v.Draw(x, y, horizontal);
            }
        }
    }
}