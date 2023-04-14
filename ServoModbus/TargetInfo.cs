namespace ServoModbus
{
    public class TargetInfo
    {
        public ushort StartSpeed { get; set; }
        public ushort MaxSpeed { get; set; }

        public ushort MaxAccTime { get; set; }
        public ushort StopSpeed { get; set; } = 0;
    }
}