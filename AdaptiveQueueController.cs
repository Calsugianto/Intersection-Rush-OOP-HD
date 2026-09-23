using System.Collections.Generic;

namespace IntersectionRush
{
    // A simple greedy scheduling algorithm: keep serving whichever axis has
    // more vehicles waiting on it, rather than switching on a fixed clock.
    // Two safety limits stop this being unfair - MIN_GREEN gives an axis a
    // fair chance before it can be judged, and MAX_GREEN guarantees the
    // other axis is never starved indefinitely.
    public class AdaptiveQueueController : TrafficLightController
    {
        private const double MIN_GREEN = 3.0;
        private const double MAX_GREEN = 12.0;

        public override void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths)
        {
            _phaseTimer += deltaSeconds;

            if (_isYellow)
            {
                if (_phaseTimer >= YELLOW_DURATION)
                {
                    _isYellow = false;
                    _phaseTimer = 0;
                    SwitchAxis();
                }
                return;
            }

            int northSouthTotal = queueLengths[Direction.North] + queueLengths[Direction.South];
            int eastWestTotal = queueLengths[Direction.East] + queueLengths[Direction.West];

            int thisAxisTotal = _activeAxis == Axis.NorthSouth ? northSouthTotal : eastWestTotal;
            int otherAxisTotal = _activeAxis == Axis.NorthSouth ? eastWestTotal : northSouthTotal;

            bool otherAxisIsBusier = otherAxisTotal > thisAxisTotal;
            bool shouldSwitch = _phaseTimer >= MAX_GREEN ||
                                 (_phaseTimer >= MIN_GREEN && otherAxisIsBusier);

            if (shouldSwitch)
            {
                _isYellow = true;
                _phaseTimer = 0;
            }
        }
    }
}