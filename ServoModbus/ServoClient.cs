﻿using NModbus;
using NModbus.Logging;
using NModbus.Serial;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;

namespace ServoModbus;

public class StringLogger : ModbusLogger
{
    public Action<string> LogAction { get; set; }
    private static readonly string BlankHeader = Environment.NewLine + new string(' ', 15);
    public StringLogger(LoggingLevel minimumLoggingLevel) : base(minimumLoggingLevel)
    {
    }

    protected override void LogCore(LoggingLevel level, string message)
    {
        message = message?.Replace(Environment.NewLine, BlankHeader);
        DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
        defaultInterpolatedStringHandler.AppendLiteral("[");
        defaultInterpolatedStringHandler.AppendFormatted(level);
        defaultInterpolatedStringHandler.AppendLiteral("]");
        LogAction?.Invoke(defaultInterpolatedStringHandler.ToStringAndClear().PadRight(15) + message);
    }
}
public class ServoClient
{

    public bool IsEnable { get; set; }
    public bool IsConnect { get; set; }

    private SerialPort _serialPort;
    private IModbusSerialMaster _modbusSerialMaster;
    byte _slaveAddress = 0;
    public ServoClient()
    {
    }

    public bool Connect(string ComName, byte slaveAddress = 0)
    {
        try
        {
            _slaveAddress = slaveAddress;
            _serialPort = new SerialPort(ComName);
            _serialPort.BaudRate = 115200;
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            logger = new StringLogger(LoggingLevel.Debug);
            factory = new ModbusFactory(logger: logger);
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.WriteTimeout = 500;
            _serialPort.ReadTimeout = 500;
            //factory.Logger.
            _modbusSerialMaster = factory.CreateRtuMaster(_serialPort);


            return true;
        }
        catch (Exception ex)
        {
            //Debug.WriteLine(ex.Message);
            logger.Error(ex.Message);
            return false;

        }

    }
    public void DisConnect()
    {
        _modbusSerialMaster.Dispose();
        _serialPort.Close();
        _serialPort.Dispose();

    }
    /// <summary>
    /// 初始化
    /// </summary>
    /// <returns></returns>
    public async Task Init()
    {
        return;
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

    public StringLogger logger { get; private set; }

    private ModbusFactory factory;

    public async Task SetDO(int idx, bool val)
    {

        await _modbusSerialMaster.WriteMultipleRegistersAsync(_slaveAddress, (0x31 << 8) + 0x4, new ushort[] {1});
    }
    /// <summary>
    /// 设置di
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    public async Task SetDI(int idx, bool val)
    {

        await _modbusSerialMaster.WriteMultipleRegistersAsync(_slaveAddress, (0x31 << 8) + 0x4, new ushort[] { 1 });
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
        IsEnable = val;
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
        try
        {
            await _modbusSerialMaster.WriteMultipleRegistersAsync(_slaveAddress, (ushort)((h1 << 8) + h2), data);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            logger.Error(ex.StackTrace);
        }

    }
    public async Task<ushort[]> ReadServoAsync(byte h1, byte h2, ushort length)
    {
        return await _modbusSerialMaster.ReadHoldingRegistersAsync(_slaveAddress, (ushort)((h1 << 8) + h2), length);
    }
    

}