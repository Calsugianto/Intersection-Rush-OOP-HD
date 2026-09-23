using System.Collections.Generic;

namespace IntersectionRush
{
    // A player-driven strategy. There's no algorithm here at all - it just
    // waits for the player to request an axis change, then runs the same
    // yellow-transition timing every other controller uses before actually
    // switching. This is the clearest proof that Intersection genuinely
    // doesn't care which concrete controller it holds: the same abstract
    // Update() call drives an algorithm in the other two controllers and a
    // person's keypresses here.
    public class ManualController : TrafficLightController
    {
        private Axis _pendingAxis;

        // Called from the game's input handling when the player asks for a
        // different axis to go green.
        public void RequestAxis(Axis axis)
        {
            if (_isYellow || axis == _activeAxis) return;

            _isYellow = true;
            _phaseTimer = 0;
            _pendingAxis = axis;
        }

        public override void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths)
        {
            if (!_isYellow) return; // nothing changes until the player asks for it

            _phaseTimer += deltaSeconds;

            if (_phaseTimer >= YELLOW_DURATION)
            {
                _isYellow = false;
                _phaseTimer = 0;
                _activeAxis = _pendingAxis;
            }
        }
    }
}