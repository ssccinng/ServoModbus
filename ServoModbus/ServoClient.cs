using NModbus;
using NModbus.Logging;
using NModbus.Serial;
using System.Collections.Generic;
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
public record DIInfo
{
    public int Idx { get; set; }
    public int Val { get; set; }

    public DIInfo(int idx, int val)
    {
        Val = val;
        Idx = idx;
    }
}
public class ServoClient
{

    public bool IsEnable { get; set; }
    public bool IsConnect { get; set; }

    protected SerialPort _serialPort = new();
    protected IModbusSerialMaster _modbusSerialMaster;
    protected byte _slaveAddress = 0;
    public Dictionary<DIFuncType, DIInfo> DIFunTable { get; set; } = new();
    public Dictionary<DOFuncType, DIInfo> DOFunTable { get; set; } = new();

    public Dictionary<int, int> TargetPosTable { get; set; } = new();
    public ServoClient()
    {
    }

    public virtual void Init()
    {
        _serialPort.BaudRate = 115200;
        _serialPort.DataBits = 8;
        _serialPort.Parity = Parity.None;
        _serialPort.StopBits = StopBits.One;
    }
    /// <summary>
    /// 控制日志输出位置（未完成）
    /// </summary>
    /// <param name="action"></param>
    /// <param name="loggingLevel"></param>
    public void LogTo(Action<string> action, LoggingLevel loggingLevel = LoggingLevel.Debug)
    {
        logger = new StringLogger(loggingLevel) { LogAction = action };

    }
    /// <summary>
    /// 设定DI功能（需重启plc）
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="dIFuncType"></param>
    /// <param name="high"></param>
    public async Task SetVDIAsync(int idx, DIFuncType dIFuncType, bool high = false)
    {
        DIFunTable.TryAdd(dIFuncType, new DIInfo(idx, 0));
        await WriteToServoAsync(0x17, (byte)(idx * 2), (ushort)dIFuncType);
        await WriteToServoAsync(0x17, (byte)((idx * 2) + 1), (ushort)(high ? 1 : 0));
    }
    public async Task SetVDOAsync(int idx, DOFuncType dIFuncType, bool high = false)
    {
        DOFunTable.TryAdd(dIFuncType, new DIInfo(idx, 0));
        await WriteToServoAsync(0x17, (byte)(idx * 2 + 33), (ushort)dIFuncType);
        await WriteToServoAsync(0x17, (byte)((idx * 2) + 34), (ushort)(high ? 1 : 0));
    }
    /// <summary>
    /// 没时间构思了 先用hset试一试
    /// </summary>
    public HashSet<DIFuncType> dIFuncTypes = new HashSet<DIFuncType>();
    public async Task AddVDI(params DIFuncType[] dIFuncType)
    {
        foreach (var item in dIFuncType)
        {
            DIFunTable[item].Val = 1;
        }
        await RefrshDI();

    }
    public async Task RemoveVDI(params DIFuncType[] dIFuncType)
    {
        foreach (var item in dIFuncType)
        {
            DIFunTable[item].Val = 0;
        }
        await RefrshDI();
    }

    public async Task RefrshDI()
    {
        int aa = 0;
        foreach (var item in DIFunTable.Keys)
        {
            aa |= (DIFunTable[item].Val << GetFuncIdx(item));
        }
        await WriteToServoAsync(0x31, 0, (ushort)aa);
    }

    public int GetFuncIdx(DIFuncType dIFuncType)
    {
        var res = DIFunTable.TryGetValue(dIFuncType, out var idx);
        if (res == false) return -1;
        return idx.Idx;
    }

    public virtual bool Connect(string ComName, byte slaveAddress = 1)
    {
        try
        {
            _slaveAddress = slaveAddress;
            _serialPort.PortName = ComName;
            factory = new ModbusFactory(logger: logger);
            _serialPort.Open();
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();
            _serialPort.WriteTimeout = 500;
            _serialPort.ReadTimeout = 500;
            _modbusSerialMaster = factory.CreateRtuMaster(_serialPort);
            return IsConnect = true;
        }
        catch (Exception ex)
        {

            //Debug.WriteLine(ex.Message);
            logger.Error(ex.Message);
            return IsConnect = false;

        }

    }

    public virtual async Task SetInitParam()
    {
        await SetVDIEnable(true);
        await SetVDOEnable(true);

        // 设置功能表
    }

    public virtual async Task SetVDIOFunc(H17 h17)
    {
        ushort[] ushorts = new ushort[32];
        ushort[] ushorts1 = new ushort[32];
        for (int i = 0; i < 16; i++)
        {
            ushorts[2 * i] = (ushort)h17.VDITable.VDIInfo[i].DIFuncType;
            ushorts[2 * i + 1] = (ushort)h17.VDITable.VDIInfo[i].High;

            ushorts1[2 * i] = (ushort)h17.VDOTable.VDOInfo[i].DOFuncType;
            ushorts1[2 * i + 1] = (ushort)h17.VDOTable.VDOInfo[i].High;
        }
        await WriteToServoAsync(0x0C, 0, ushorts);
        await WriteToServoAsync(0x0C, 33, ushorts1);
    }

    public void DisConnect()
    {
        // 清除表

        IsConnect = false;
        _modbusSerialMaster.Dispose();
        _serialPort.Close();
        _serialPort.Dispose();

        DIFunTable.Clear();
        DOFunTable.Clear();
        TargetPosTable.Clear();
    }
    /// <summary>
    /// 初始化
    /// </summary>
    /// <returns></returns>
    //public async Task Init()
    //{
    //    return;
    //    await WriteToServoAsync(0x02, 00, 1);
    //    await WriteToServoAsync(0x03, 08, 0);
    //    await WriteToServoAsync(0x03, 10, 0);
    //    await WriteToServoAsync(0x05, 00, 2);
    //    await WriteToServoAsync(0x05, 30, 1);

    //    await WriteToServoAsync(0x05, 31, 1);
    //    await WriteToServoAsync(0x05, 32, 1);

    //    await WriteToServoAsync(0x0c, 09, 1);
    //    await WriteToServoAsync(0x0c, 13, 1);
    //    await WriteToServoAsync(0x0c, 15, 0);

    //    await WriteToServoAsync(0x11, 00, 1);
    //    await WriteToServoAsync(0x11, 01, 1);
    //    await WriteToServoAsync(0x11, 04, 1);
    //    await WriteToServoAsync(0x11, 05, 1);
    //    await WriteToServoAsync(0x11, 16, 0);

    //    await WriteToServoAsync(0x17, 00, 1);
    //    await WriteToServoAsync(0x17, 02, 18);
    //    await WriteToServoAsync(0x17, 04, 19);
    //    await WriteToServoAsync(0x17, 06, 28);
    //    await WriteToServoAsync(0x17, 08, 32);
    //    await WriteToServoAsync(0x17, 10, 34);
    //    await WriteToServoAsync(0x17, 12, 2);


    //    //await WriteToServoAsync(0x0c, 00, 1);
    //    //await WriteToServoAsync(0x0c, 08, 115200);

    //}
    /// <summary>
    /// 移动结束事件
    /// </summary>
    public Action OnMoveEnd;

    public StringLogger logger { get; protected set; }

    protected ModbusFactory factory;

    public async Task SetDO(int idx, bool val)
    {

        await _modbusSerialMaster.WriteMultipleRegistersAsync(_slaveAddress, (0x31 << 8) + 0x4, new ushort[] { 1 });
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

        await WriteToServoAsync(0x03, 10, new ushort[] { 1 });
        await WriteToServoAsync(0x03, 11, new ushort[] { (ushort)(val ? 1 : 0) });
        // todo: 这个需要实际读取！
        IsEnable = val;
    }

    public async Task SetMultiSpeed(ushort startSpeed, ushort stopSpeed)
    {
        await WriteToServoAsync(0x11, 10, startSpeed, stopSpeed);
    }

    // 设置目标
    public async Task SetTargetAsync(byte idx, int pos, TargetInfo targetInfo)
    {
        TargetPosTable.TryAdd(idx, pos);
        TargetPosTable[idx] = pos;
        // 记录一下
        await WriteToServoAsync(0x11, (byte)(idx * 5 + 12),
                                                (ushort)(pos & ((1 << 16) - 1)), (ushort)(pos >> 16),
                                                targetInfo.MaxSpeed,
                                                targetInfo.MaxAccTime,
                                                0);
    }

    public async Task SetTargetAsync(byte idx, TargetInfo targetInfo)
    {
        TargetPosTable.TryAdd(idx, targetInfo.Pos);
        TargetPosTable[idx] = targetInfo.Pos;
        // 记录一下
        await WriteToServoAsync(0x11, (byte)(idx * 5 + 12),
                                                (ushort)(targetInfo.Pos & ((1 << 16) - 1)), (ushort)(targetInfo.Pos >> 16),
                                                targetInfo.MaxSpeed,
                                                targetInfo.MaxAccTime,
                                                0);
    }
    // 让移动轴移动到指定位置
    public async Task MoveToAsync(int target)
    {
        List<DIFuncType> list1 = new List<DIFuncType> { DIFuncType.多段位置指令使能, 
            
            DIFuncType.多段运行指令切换CM01 ,
            DIFuncType.多段运行指令切换CM02 ,
            DIFuncType.多段运行指令切换CM03,
            DIFuncType.多段运行指令切换CM04 ,
        };
        await RemoveVDI(list1.ToArray());

        List<DIFuncType> list = new List<DIFuncType> { };
        for (int i = 0; i < 4; i++)
        {
            if ((target & (1 << i)) != 0)
            {
                list.Add(DIFuncType.多段运行指令方向选择 + i + 1);
            }
        }
        await AddVDI(list.ToArray());
        await Task.Delay(100);
        await AddVDI(DIFuncType.多段位置指令使能);
        await WaitForFunc(DOFuncType.定位完成);
        await RemoveVDI(list1.ToArray());





    }


    /// <summary>
    /// 归原
    /// </summary>
    /// <returns></returns>
    public async Task ReturnZeroAsync()
    {
        //await Task.Delay(100);
        await AddVDI(DIFuncType.原点复归使能);
        await WaitForFunc(DOFuncType.定位完成);
        await RemoveVDI(DIFuncType.原点复归使能);
        await RemoveVDI(DIFuncType.以当前位置为原点);
        await Task.Delay(100);
        await AddVDI(DIFuncType.以当前位置为原点);
    }

    public async Task<bool> WaitForFunc(DOFuncType dIFuncType, int val = 1, int period = 500, int timeout = 20000)
    {
        await Task.Delay(period);
        Stopwatch stopwatch = new();
        stopwatch.Start();
        while (true)
        {
            if (stopwatch.ElapsedMilliseconds > timeout)
            {
                return false;
            }
            if (DOFunTable[dIFuncType].Val == val)
            {
                await Task.Delay(200);
                if (DOFunTable[dIFuncType].Val == val)
                    return true;
                logger.Warning("存在误判到位");
            }
            await Task.Delay(10);
        }
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
    /// <summary>
    /// 设置虚拟DI通信使能
    /// </summary>
    /// <param name="enable"></param>
    /// <returns></returns>
    public async Task SetVDIEnable(bool enable)
    {
        await WriteToServoAsync(0x0C, 9, (ushort)(enable ? 1 : 0));
    }
    /// <summary>
    /// 设置虚拟DO通信使能
    /// </summary>
    /// <param name="enable"></param>
    /// <returns></returns>
    public async Task SetVDOEnable(bool enable)
    {
        await WriteToServoAsync(0x0C, 11, (ushort)(enable ? 1 : 0));
    }
    public async Task<ushort[]> ReadServoAsync(byte h1, byte h2, ushort length)
    {
        return await _modbusSerialMaster.ReadHoldingRegistersAsync(_slaveAddress, (ushort)((h1 << 8) + h2), length);
    }

    public async Task StopMove()
    {
        await RemoveVDI(DIFuncType.正向点动, DIFuncType.反向点动);
        //await RemoveVDI();
    }

    public async Task PosMove()
    {
        await AddVDI(DIFuncType.正向点动);
    }

    public async Task NagMove()
    {
        await AddVDI(DIFuncType.反向点动);
    }

    public async Task SetMoveType(ServoMoveType moveType)
    {
        await WriteToServoAsync(0x11, 04, (ushort)moveType);
    }

    public async Task SetMultiMoveMode(MultiMoveMode multiMoveMode)
    {
        await WriteToServoAsync(0x11, 00, (ushort)multiMoveMode);
    }

    public async Task<ushort> ReadDoAsync()
    {
        var res = (await ReadServoAsync(0x17, 32, 1))[0];

        for (int i = 0; i < 16; i++)
        {
            var dot = DOFunTable.Values.FirstOrDefault(s => s.Idx == i);
            if  (dot != null)
            {
                dot.Val = (res & (1 << i)) >> (i);
            }

        }
        // 记得加log
        // 更新那什么 本地缓存io
        return res;
    }

    public async Task ResetAlarm()
    {
        await AddVDI(DIFuncType.报警复位信号);
        await Task.Delay(100);
        await RemoveVDI(DIFuncType.报警复位信号);
    }

    /// <summary>
    /// 获取错误码
    /// </summary>
    /// <returns></returns>
    public async Task<ushort> GetErrorCode()
    {
        return (await ReadServoAsync(0x31, 34, 1))[0];
    }



}