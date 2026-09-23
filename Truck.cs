namespace IntersectionRush
{
    // The heaviest vehicle - a middling top speed, but the slowest to speed up
    // or slow down of the three, so it needs the most road space to be safe.
    public class Truck : Vehicle
    {
        public override double MaxSpeed => 90;
        public override double Acceleration => 15;
        public override double Deceleration => 30;
        public override int Length => 65;

        protected override string HorizontalSpriteName => "TruckH";
        protected override string VerticalSpriteName => "TruckV";

        public Truck(double spawnTime) : base(spawnTime) { }
    }
}