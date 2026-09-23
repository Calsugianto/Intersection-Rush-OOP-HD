using System.Collections.Generic;

namespace IntersectionRush
{
    // The simplest possible strategy: switch axis every GREEN_DURATION
    // seconds, no matter how busy either axis actually is. This is the
    // baseline every real adaptive system is compared against, which is
    // exactly the role it plays in this simulation too.
    public class FixedTimeController : TrafficLightController
    {
        private const double GREEN_DURATION = 6.0;

        public override void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths)
        {
            _phaseTimer += deltaSeconds;

            if (_isYellow && _phaseTimer >= YELLOW_DURATION)
            {
                _isYellow = false;
                _phaseTimer = 0;
                SwitchAxis();
            }
            else if (!_isYellow && _phaseTimer >= GREEN_DURATION)
            {
                _isYellow = true;
                _phaseTimer = 0;
            }
        }
    }
}