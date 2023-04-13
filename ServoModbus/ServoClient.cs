using NModbus;
using NModbus.Logging;
using NModbus.Serial;
using System.IO.Ports;

namespace ServoModbus;
public class ServoClient
{
    SerialPort _serialPort { get; set; }
    IModbusSerialMaster modbusSerialMaster { get; set; }
    byte slaveAddress = 0;
    public ServoClient(string ComName, byte slaveAddress = 0)
    {

        _serialPort = new SerialPort(ComName);
        _serialPort.BaudRate = 115200;
        _serialPort.DataBits = 8;
        _serialPort.Parity = Parity.None;
        _serialPort.StopBits = StopBits.One;
        _serialPort.Open();
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        _serialPort.WriteTimeout = 500;
        _serialPort.ReadTimeout = 500;
        var factory = new ModbusFactory(logger: new ConsoleModbusLogger(LoggingLevel.Trace));
        //factory.Logger.
        modbusSerialMaster = factory.CreateRtuMaster(_serialPort);
    }

    public async Task Init()
    {
        await WriteToServoAsync(0x02, 00, 1);
        await WriteToServoAsync(0x03, 08, 0);
        await WriteToServoAsync(0x03, 10, 0);
        await WriteToServoAsync(0x05, 00, 2);
        await WriteToServoAsync(0x05, 30, 1);

        await WriteToServoAsync(0x05, 31, 1);
        await WriteToServoAsync(0x05, 32, 1);
        
        await WriteToServoAsync(0x0c, 09, 1);
        await WriteToServoAsync(0x0c, 13, 1);
        await WriteToServoAsync(0x0c, 15, 0);

        await WriteToServoAsync(0x11, 00, 1);
        await WriteToServoAsync(0x11, 01, 1);
        await WriteToServoAsync(0x11, 04, 1);
        await WriteToServoAsync(0x11, 05, 1);
        await WriteToServoAsync(0x11, 16, 0);

        await WriteToServoAsync(0x17, 00, 1);
        await WriteToServoAsync(0x17, 02, 18);
        await WriteToServoAsync(0x17, 04, 19);
        await WriteToServoAsync(0x17, 06, 28);
        await WriteToServoAsync(0x17, 08, 32);
        await WriteToServoAsync(0x17, 10, 34);
        await WriteToServoAsync(0x17, 12, 2);


        //await WriteToServoAsync(0x0c, 00, 1);
        //await WriteToServoAsync(0x0c, 08, 115200);

    }

    public async Task SetDo(int idx, bool val)
    {

        await modbusSerialMaster.WriteMultipleRegistersAsync(slaveAddress, (0x31 << 8) + 0x4, new ushort[] {1});
    }

    public async Task SetEnable(bool val)
    {
        await WriteToServoAsync(0x03, 10, new ushort[] {1});
        await WriteToServoAsync(0x03, 11, new ushort[] { (ushort)(val ? 1 : 0) });
    }

    public async Task WriteToServoAsync(byte h1, byte h2, params ushort[] data)
    {
        await modbusSerialMaster.WriteMultipleRegistersAsync(slaveAddress, (ushort)((h1 << 8) + h2), data);

    }

}