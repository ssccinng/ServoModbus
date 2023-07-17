using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;
using Microsoft.Extensions.Logging;
using RPDelectPallet.Meditor.Commands;
using RPDelectPallet.MVVM.Model;
using ServoModbus;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RPDelectPallet.MVVM.ViewModel;


public partial class ServoPanelViewModel: ObservableRecipient
{
    [NotifyPropertyChangedFor("ServoText")]
    [ObservableProperty]
    bool _isEnable = true;
    [ObservableProperty]
    bool _isConnect;

    public ObservableCollection<bool> DoFuncValue { get; set; } = new(new bool[40]); 

    public string ServoText => IsEnable ? "停止" : "使能" ;

    public string[] COMS { get; set; } = SerialPort.GetPortNames();
    private int chooseIdx = 1;
    public int ChooseIdx
    {
        get => chooseIdx;
        set
        {
            chooseIdx = value;
            if (value >= 0) MainViewModel.Saves.ComName = COMS[value];
            else MainViewModel.Saves.ComName = string.Empty;

            MainViewModel.Saves.Save();
            // 记得去保存
        }
    }

    public SV660PClient ServoClient { get; set; }

    private ILogger<ServoPanelViewModel> _logger;
    private readonly IMediator _mediator;
    private Thread doListenThread;

    public ServoPanelViewModel(SV660PClient servoClient, ILogger<ServoPanelViewModel> logger, IMediator mediator)
    {
        ServoClient = servoClient;
        // Todo: 暂时的输出
        ServoClient.LogTo(Console.WriteLine);
        _logger = logger;
        this._mediator = mediator;
        ChooseIdx = Array.IndexOf(COMS, MainViewModel.Saves.ComName);
        // 循环读取do
        doListenThread = new Thread(DoListen);
        doListenThread.Start();
    }
    public async Task<bool> Conn()
    {
        // Todo: 为啥要在这里  有问题 放到vm里去
        if (IsConnect)
        {
             _logger.LogWarning("已经连接");
            return true;
            //ViewModel.DisConnect();
        }
        else
        {
            if (await Connect())
            {

                // 回0然后回原
                await _mediator.Send(new ServoMoveToTargetCommand(0));
                await _mediator.Send(new ServoReturnZeroCommand());
                _logger.LogWarning("设置初始化参数");
                return true;
            }
            else
            {
                _logger.LogWarning("连接失败");
                MessageBox.Show("移动轴连接失败");
                //MessageBox.Show("连接失败", "提示", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions
                //    .ServiceNotification);
                return false;
            }
        }
    }

    // 轮询
    public async void DoListen()
    {
        while (true)
        {
            if (IsConnect)
            {
                // 尝试函数化

                try
                {
                    //var data = await ServoClient.ReadServoAsync(0x17, 32, 1);
                    var data = await ServoClient.ReadDoAsync();
                    for (int i = 0; i < 16; i++)
                    {

                        DoFuncValue[i] = (data & (1 << i)) != 0;
                        
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("读取do失败");
                    _logger.LogError(ex.Message);
                    //Reconnect();
                }


            }
                await Task.Delay(200);

        }
    }

    private void Reconnect()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Connect()
    {
        //return IsEnable = true;
        IsConnect = ServoClient.Connect(MainViewModel.Saves.ComName, MainViewModel.Saves.SlaveAddress);
        if (IsConnect)
        {
            _logger.LogInformation($"连接移动轴modbus: {IsEnable}");
            await ServoClient.SetInitParam().ConfigureAwait(false);
        }

        else
        {
            _logger.LogWarning("连接移动轴modbus失败");
        }

        // 这里尝试开一个重连线程
        return IsConnect;
    }

    internal void DisConnect()
    {
        ServoClient.DisConnect();
        IsConnect = false;
        _logger.LogInformation("断联移动轴modbus");

    }
}
