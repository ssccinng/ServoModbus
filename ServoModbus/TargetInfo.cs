namespace ServoModbus
{
    public class TargetInfo
    {
        public int Pos { get; set; }

        public ushort StartSpeed { get; set; } = 250;
        public ushort MaxSpeed { get; set; } = 500;

        public ushort MaxAccTime { get; set; } = 50;
        public ushort StopSpeed { get; set; } = 0;
    }
}