namespace IntersectionRush
{
    // A long, heavy vehicle - slow to build up speed and slow to stop.
    // Length matches the BusH/BusV sprite artwork (30px along the direction
    // of travel).
    public class Bus : Vehicle
    {
        public override double MaxSpeed => 80;
        public override double Acceleration => 20;
        public override double Deceleration => 35;
        public override int Length => 30;

        protected override string HorizontalSpriteName => "BusH";
        protected override string VerticalSpriteName => "BusV";

        public Bus(double spawnTime) : base(spawnTime) { }
    }
}