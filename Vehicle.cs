using SplashKitSDK;

namespace IntersectionRush
{
    // Base class for every vehicle in the simulation. Owns the physics that is
    // shared by every vehicle - accelerating, braking to keep a safe following
    // distance, and moving forward - but leaves how fast a vehicle can go, how
    // sharply it can brake, and what it looks like entirely up to its
    // subclasses. Approach only ever works with this abstract type, so it
    // never needs to know whether it's holding a Car, a Bus, or a Truck.
    public abstract class Vehicle
    {
        // Minimum bumper-to-bumper gap once a vehicle has come to a stop.
        private const double MIN_GAP = 12;

        // Distance travelled along the lane so far, in pixels. 0 is the spawn
        // point; STOP_LINE_DISTANCE is the stop line.
        public double Position { get; private set; }

        // Current speed, in pixels/second.
        public double Speed { get; private set; }

        // The simulation time this vehicle was created - used to work out how
        // long it waited once it passes through.
        public double SpawnTime { get; }

        public abstract double MaxSpeed { get; }       // pixels/second
        public abstract double Acceleration { get; }   // pixels/second^2
        public abstract double Deceleration { get; }   // pixels/second^2 (braking)
        public abstract int Length { get; }             // vehicle length, in pixels

        protected abstract string HorizontalSpriteName { get; }
        protected abstract string VerticalSpriteName { get; }

        protected Vehicle(double spawnTime)
        {
            SpawnTime = spawnTime;
        }

        // Moves this vehicle for one frame of simulation time. gapAhead is the
        // free road space between this vehicle and whatever is directly in
        // front of it - another vehicle, or the stop line.
        //
        // The physics: braking distance for a given speed and deceleration is
        // speed^2 / (2 * deceleration) (from v^2 = u^2 - 2as, solved for s with
        // v = 0). If the gap ahead is smaller than that safe braking distance,
        // brake; otherwise accelerate towards this vehicle's top speed.
        public void UpdatePhysics(double deltaSeconds, double gapAhead)
        {
            double safeDistance = (Speed * Speed) / (2 * Deceleration) + MIN_GAP;

            if (gapAhead < safeDistance)
            {
                Speed -= Deceleration * deltaSeconds;
            }
            else
            {
                Speed += Acceleration * deltaSeconds;
            }

            if (Speed < 0) Speed = 0;
            if (Speed > MaxSpeed) Speed = MaxSpeed;

            Position += Speed * deltaSeconds;
        }

        // Draws this vehicle at the given screen position. horizontal picks
        // which pre-rotated sprite to use - vehicles on an East/West approach
        // need the horizontal artwork, North/South need the vertical artwork.
        public void Draw(double x, double y, bool horizontal)
        {
            Bitmap bitmap = SplashKit.BitmapNamed(horizontal ? HorizontalSpriteName : VerticalSpriteName);
            bitmap.Draw(x - bitmap.Width / 2, y - bitmap.Height / 2);
        }
    }
}