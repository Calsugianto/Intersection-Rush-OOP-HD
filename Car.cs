namespace IntersectionRush
{
    // The fastest, most agile vehicle - quick to speed up and quick to stop.
    // Length matches the CarH/CarV sprite artwork (20px along the direction
    // of travel).
    public class Car : Vehicle
    {
        public override double MaxSpeed => 120;
        public override double Acceleration => 40;
        public override double Deceleration => 60;
        public override int Length => 20;

        protected override string HorizontalSpriteName => "CarH";
        protected override string VerticalSpriteName => "CarV";

        public Car(double spawnTime) : base(spawnTime) { }
    }
}