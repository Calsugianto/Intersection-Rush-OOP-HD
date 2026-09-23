namespace IntersectionRush
{
    // The fastest, most agile vehicle - quick to speed up and quick to stop.
    public class Car : Vehicle
    {
        public override double MaxSpeed => 120;
        public override double Acceleration => 40;
        public override double Deceleration => 60;
        public override int Length => 30;

        protected override string HorizontalSpriteName => "CarH";
        protected override string VerticalSpriteName => "CarV";

        public Car(double spawnTime) : base(spawnTime) { }
    }
}