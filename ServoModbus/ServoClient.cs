using NModbus;
using NModbus.Logging;
using NModbus.Serial;
using System.IO.Ports;

namespace ServoModbus;
public class ServoClient
{

    private SerialPort _serialPort;
    private IModbusSerialMaster _modbusSerialMaster;
    byte slaveAddress = 0;
    public ServoClient(string ComName, byte slaveAddress = 0)
    {

        _serialPort = new SerialPort(ComName);
        _serialPort.BaudRate = 115200;
        _serialPort.DataBits = 8;
        _serialPort.Parity = Parity.None;
        _serialPort.StopBits = StopBits.One;
        
    }

    public void Connect()
    {
        _serialPort.Open();
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        _serialPort.WriteTimeout = 500;
        _serialPort.ReadTimeout = 500;
        var factory = new ModbusFactory(logger: new ConsoleModbusLogger(LoggingLevel.Trace));
        //factory.Logger.
        _modbusSerialMaster = factory.CreateRtuMaster(_serialPort);
    }
    public void DisConnect()
    {
        _modbusSerialMaster.Dispose();
        _serialPort.Close();

    }
    /// <summary>
    /// 初始化
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// 移动结束事件
    /// </summary>
    public Action OnMoveEnd;
    public async Task SetDO(int idx, bool val)
    {

        await _modbusSerialMaster.WriteMultipleRegistersAsync(slaveAddress, (0x31 << 8) + 0x4, new ushort[] {1});
    }
    /// <summary>
    /// 设置di
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public async Task SetDI(int idx, bool val)
    {

        await _modbusSerialMaster.WriteMultipleRegistersAsync(slaveAddress, (0x31 << 8) + 0x4, new ushort[] { 1 });
    }
    /// <summary>
    /// 设置使能
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public async Task SetEnableAsync(bool val)
    {
        await WriteToServoAsync(0x03, 10, new ushort[] {1});
        await WriteToServoAsync(0x03, 11, new ushort[] { (ushort)(val ? 1 : 0) });
    }
    public async Task SetTargetAsync(byte idx, byte pos, TargetInfo targetInfo)
    {
        await WriteToServoAsync(11, (byte)((idx + 1) * 10), targetInfo.StartSpeed, 
                                                targetInfo.StopSpeed, pos, targetInfo.MaxSpeed, targetInfo.MaxAccTime, 0);
    }
    // 让移动轴移动到指定位置
    public async Task MoveToAsync(int target)
    {
        //await WriteToServoAsync(0x03, 00, new ushort[] {1});
        //await WriteToServoAsync(0x03, 01, new ushort[] { (ushort)pos});
         
    }

    /// <summary>
    /// 归原
    /// </summary>
    /// <returns></returns>
    public async Task ReturnZeroAsync()
    {

    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="h1">Hxx（16进制）</param>
    /// <param name="h2">.xx(10进制)</param>
    /// <param name="data">数据</param>
    /// <returns></returns>

    public async Task WriteToServoAsync(byte h1, byte h2, params ushort[] data)
    {
        await _modbusSerialMaster.WriteMultipleRegistersAsync(slaveAddress, (ushort)((h1 << 8) + h2), data);

    }
    public async Task<ushort[]> ReadServoAsync(byte h1, byte h2, ushort length)
    {
        return await _modbusSerialMaster.ReadHoldingRegistersAsync(slaveAddress, (ushort)((h1 << 8) + h2), length);
    }
    

}