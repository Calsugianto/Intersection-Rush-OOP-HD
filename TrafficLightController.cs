using System.Collections.Generic;

namespace IntersectionRush
{
    // A real traffic light controls two directions at once - North and South
    // together, or East and West together - never all four independently.
    public enum Axis { NorthSouth, EastWest }

    // Base class for every traffic-light strategy. An Intersection only ever
    // talks to this abstract type, so a Fixed-time, Adaptive, or Manual
    // (player-controlled) controller can all be dropped in without the
    // Intersection needing to know - or care - which one it actually has.
    public abstract class TrafficLightController
    {
        // How long the amber/yellow warning phase lasts before the axis
        // actually switches. Protected (not private) because every subclass
        // needs it to time its own yellow phase, but nothing outside this
        // class hierarchy needs to see it.
        protected const double YELLOW_DURATION = 0.5;

        protected Axis _activeAxis = Axis.NorthSouth;
        protected bool _isYellow = false;
        protected double _phaseTimer = 0;

        public Axis ActiveAxis => _activeAxis;

        // True only while this direction has an actual green light - not
        // yellow, not red.
        public bool IsGreenFor(Direction direction)
        {
            if (_isYellow) return false;
            return AxisOf(direction) == _activeAxis;
        }

        public bool IsYellowFor(Direction direction)
        {
            if (!_isYellow) return false;
            return AxisOf(direction) == _activeAxis;
        }

        // Called once per frame. queueLengths gives the number of vehicles
        // currently waiting on each of the four approaches - the adaptive
        // controller uses this to decide when to switch; the fixed-time
        // controller ignores it completely.
        public abstract void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths);

        protected void SwitchAxis()
        {
            _activeAxis = _activeAxis == Axis.NorthSouth ? Axis.EastWest : Axis.NorthSouth;
        }

        private static Axis AxisOf(Direction direction)
        {
            return (direction == Direction.North || direction == Direction.South)
                ? Axis.NorthSouth
                : Axis.EastWest;
        }
    }
}