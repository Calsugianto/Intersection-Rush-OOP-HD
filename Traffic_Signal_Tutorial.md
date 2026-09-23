# Building Realistic Traffic Physics and an Adaptive Signal Algorithm in SplashKit

*A walkthrough of the physics, data structures, and algorithm behind a small traffic-management simulation, written for SIT771's Something Awesome task.*

## What we're building

**Intersection Rush** is a small traffic-signal simulation with three intersections running side by side: one controlled by a naive fixed-timer light, one by an adaptive algorithm that reacts to queue length, and one controlled by the player. Vehicles queue up realistically — accelerating, braking, and keeping a safe following distance — rather than just teleporting through on a timer.

This article focuses on the two ideas that make it more than "a rectangle that changes colour every few seconds": **physics-based vehicle movement**, and a **queue-based scheduling algorithm** for the adaptive light. Both are built entirely with SplashKit primitives you already know from the rest of the unit — no new libraries required.

## Part 1: giving vehicles real physics

A vehicle in this simulation isn't just a position that jumps forward each frame. It has a **speed** and a **top speed**, and it can only change speed at a limited rate — its acceleration when speeding up, its deceleration when braking. That's the entire physics model: no forces, no mass, just kinematics.

The one piece of real physics worth knowing is the **braking distance formula**. From the constant-deceleration equation of motion:

```
v² = u² − 2as
```

where `u` is current speed, `v` is the speed you want to reach (0, if you're stopping), `a` is your deceleration, and `s` is the distance travelled while braking. Solving for `s` when `v = 0`:

```
s = u² / (2a)
```

That's the minimum safe following distance for a vehicle travelling at speed `u` with deceleration `a`. If the gap to whatever's ahead is smaller than that, the vehicle needs to brake *now* to avoid a collision. Here's the whole update method:

```csharp
public void UpdatePhysics(double deltaSeconds, double gapAhead)
{
    double safeDistance = (Speed * Speed) / (2 * Deceleration) + MIN_GAP;

    if (gapAhead < safeDistance)
        Speed -= Deceleration * deltaSeconds;
    else
        Speed += Acceleration * deltaSeconds;

    if (Speed < 0) Speed = 0;
    if (Speed > MaxSpeed) Speed = MaxSpeed;

    Position += Speed * deltaSeconds;
}
```

`MaxSpeed`, `Acceleration`, and `Deceleration` are abstract properties on a base `Vehicle` class, overridden differently by `Car`, `Bus`, and `Truck` — a truck has a much lower acceleration and deceleration than a car, so it needs far more following distance at the same speed. That's real physics falling naturally out of polymorphism: three subclasses, one shared update method, three noticeably different driving styles on screen.

## Part 2: queueing vehicles with `Queue<T>`

Every approach to an intersection needs to track vehicles in the order they arrived — first in, first out. C#'s built-in `Queue<T>` is a direct match for that, and it's a nice change of pace from the `List<T>` used everywhere else in the unit.

The one subtlety: you can safely `foreach` over a `Queue<T>` to read it (it enumerates front-to-back), but you still can't remove an item from it *during* that same `foreach` — the exact same rule that applies to `List<T>`. The fix is the same collect-then-remove idea used everywhere else in this unit, just simpler here, because only the vehicle at the very front of the queue can ever be ready to leave:

```csharp
public Vehicle Update(double deltaSeconds)
{
    Vehicle previous = null;

    foreach (Vehicle v in _queue)
    {
        double gap = previous == null
            ? (_light.IsGreenFor(Direction) ? 10000 : STOP_LINE_DISTANCE - v.Position)
            : previous.Position - previous.Length - v.Position;

        v.UpdatePhysics(deltaSeconds, gap);
        previous = v;
    }

    // Only ever remove the front vehicle, and only after the foreach above
    // has finished reading the whole queue.
    if (_queue.Count > 0 && _queue.Peek().Position >= STOP_LINE_DISTANCE)
        return _queue.Dequeue();

    return null;
}
```

Tracking a `previous` reference as the loop runs is what lets each vehicle work out the gap to whatever's directly ahead of it, without needing to index into the queue at all — `Queue<T>` doesn't support indexing, but it doesn't need to here.

## Part 3: two ways to run a traffic light

A traffic light is really just a decision: *which axis is green right now, and when should that change?* That decision is exactly the kind of thing that's worth pulling out behind an abstract class, so the rest of the program never needs to know which strategy is running:

```csharp
public abstract class TrafficLightController
{
    protected Axis _activeAxis = Axis.NorthSouth;
    protected bool _isYellow;
    protected double _phaseTimer;

    public bool IsGreenFor(Direction direction) => ...
    public abstract void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths);
}
```

The naive baseline just watches a clock:

```csharp
public class FixedTimeController : TrafficLightController
{
    private const double GREEN_DURATION = 6.0;

    public override void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths)
    {
        _phaseTimer += deltaSeconds;
        if (_phaseTimer >= GREEN_DURATION) { _isYellow = true; _phaseTimer = 0; }
        // ...yellow-phase handling omitted for brevity
    }
}
```

The adaptive version uses a small **greedy algorithm**: keep serving whichever axis currently has more vehicles queued, rather than switching on a fixed clock, with two guard rails so it never becomes unfair:

```csharp
public class AdaptiveQueueController : TrafficLightController
{
    private const double MIN_GREEN = 3.0; // give an axis a fair chance first
    private const double MAX_GREEN = 12.0; // never starve the other axis forever

    public override void Update(double deltaSeconds, IReadOnlyDictionary<Direction, int> queueLengths)
    {
        _phaseTimer += deltaSeconds;

        int thisAxisTotal = /* sum of the two directions on the active axis */;
        int otherAxisTotal = /* sum of the two directions on the other axis */;

        bool shouldSwitch = _phaseTimer >= MAX_GREEN ||
                             (_phaseTimer >= MIN_GREEN && otherAxisTotal > thisAxisTotal);

        if (shouldSwitch) { _isYellow = true; _phaseTimer = 0; }
    }
}
```

Because both controllers extend the same abstract class, an `Intersection` can hold either one — or a third, player-controlled one — and call `Update()` on it every frame without ever knowing which strategy it's actually running. Running all three side by side is what turns "I applied an algorithm" into something a viewer can actually *see* working: the adaptive intersection visibly keeps its queues shorter than the fixed-timer one once traffic gets busy.

## Part 4: wiring it into the game loop

None of this is SplashKit-specific until the very last step — drawing and timing. Each vehicle type loads a pair of sprite images (one for horizontal approaches, one for vertical) exactly the way Healthy Bites loaded its food images:

```csharp
private void LoadResources()
{
    foreach (string name in VehicleImageNames)
        SplashKit.LoadBitmap(name, name + ".png");
}
```

...and the main loop is the same `HandleInput → Update → Draw` structure used throughout the unit — the physics and the algorithm both just live inside `Update()`, one call per frame, same as everything else.

## Where this could go next

A few directions this could be pushed further, if you wanted to keep going: sending each intersection's live queue lengths over a small local web socket so a second screen (or a phone) could watch the simulation remotely — the "networking" advanced feature this task also suggests — or replacing the greedy algorithm with a lookahead one that predicts arrivals a few seconds ahead using each vehicle's current speed. Both are natural extensions of the `TrafficLightController` abstraction already in place; neither needs the existing classes to change at all, which is really the point of designing around an abstract base class in the first place.
