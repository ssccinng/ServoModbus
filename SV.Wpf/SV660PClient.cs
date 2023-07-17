using NModbus;
using NModbus.Logging;
using NModbus.Serial;
using RPDelectPallet.MVVM.ViewModel;
using ServoModbus;
using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RPDelectPallet.MVVM.Model;

public class SV660PClient: ServoClient
{
    //public override async void Init()
    //{

    //    base.Init();
    //}

    public override async Task SetInitParam()
    {
        await RefrshDI().ConfigureAwait(false);
        // 取消使能
        //await SetVDIAsync(0, DIFuncType.伺服使能).ConfigureAwait(false);
        await SetVDIAsync(0, DIFuncType.报警复位信号, true).ConfigureAwait(false);
        await SetVDIAsync(1, DIFuncType.正向点动).ConfigureAwait(false);
        await SetVDIAsync(2, DIFuncType.反向点动).ConfigureAwait(false);

        await SetVDIAsync(3, DIFuncType.多段位置指令使能).ConfigureAwait(false);
        await SetVDIAsync(4, DIFuncType.多段运行指令切换CM01).ConfigureAwait(false);
        await SetVDIAsync(5, DIFuncType.多段运行指令切换CM02).ConfigureAwait(false);
        await SetVDIAsync(6, DIFuncType.多段运行指令切换CM03).ConfigureAwait(false);
        await SetVDIAsync(7, DIFuncType.多段运行指令切换CM04).ConfigureAwait(false);
        await SetVDIAsync(8, DIFuncType.原点复归使能).ConfigureAwait(false);
        await SetVDIAsync(9, DIFuncType.以当前位置为原点, true).ConfigureAwait(false);

        // 运动模式
        await WriteToServoAsync(0x05, 00, 2);
        await WriteToServoAsync(0x05, 30, 1);
        await WriteToServoAsync(0x05, 31, 1);

        // 归原时间
        await WriteToServoAsync(0x05, 35, 60000);
        // 运动模式
        //await WriteToServoAsync(0x05, 02, 1000);

        // 取消物理报警复位
        await WriteToServoAsync(0x03, 08, 0);


        await SetVDOAsync(0, DOFuncType.伺服准备好);
        await SetVDOAsync(1, DOFuncType.定位完成);
        await SetVDOAsync(2, DOFuncType.警告);
        await SetVDOAsync(3, DOFuncType.故障);
        await SetVDOAsync(4, DOFuncType.输出3位报警代码_1);
        await SetVDOAsync(5, DOFuncType.输出3位报警代码_2);
        await SetVDOAsync(6, DOFuncType.输出3位报警代码_3);

        await SetMultiSpeed(200, 0);
        await SetMultiMoveMode(MultiMoveMode.DI切换运行);
        await SetMoveType(ServoMoveType.绝对定位);
        //await SetMoveType(ServoMoveType.相对定位);

        for (byte i = 0; i < MainViewModel.Saves.TargetInfos.Length; i++)
        {
            await SetTargetAsync(i, MainViewModel.Saves.TargetInfos[i]);
        }

        //await SetTargetAsync(0, -0, new TargetInfo
        //{
        //    MaxAccTime = 50,
        //    MaxSpeed = 1000,
        //    StartSpeed = 250,
        //    StopSpeed = 0
        //});

        //await SetTargetAsync(1, -5000, new TargetInfo
        //{
        //    MaxAccTime = 50,
        //    MaxSpeed = 500,
        //    StartSpeed = 250,
        //    StopSpeed = 0
        //});

        //await SetTargetAsync(2, -10000, new TargetInfo
        //{
        //    MaxAccTime = 50,
        //    MaxSpeed = 500,
        //    StartSpeed = 250,
        //    StopSpeed = 0
        //});

        //await SetTargetAsync(3, -15000, new TargetInfo
        //{
        //    MaxAccTime = 50,
        //    MaxSpeed = 500,
        //    StartSpeed = 250,
        //    StopSpeed = 0
        //});
        //// 后续需要根据这个表去直接搜索功能

        await base.SetInitParam();
    }

    public override bool Connect(string ComName, byte slaveAddress = 0)
    {
        if (ComName == string.Empty)
        {
            logger.Warning("串口号为空");
            return false;
        }

        try
        {
            logger = new StringLogger(LoggingLevel.Debug);

            _slaveAddress = slaveAddress;
            _serialPort = new SerialPort(ComName);
            _serialPort.BaudRate = 57600;
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.Two;
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
}

