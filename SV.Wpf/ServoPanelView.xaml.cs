using Microsoft.Extensions.Logging;
using RPDelectPallet.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RPDelectPallet.MVVM.View;

/// <summary>
/// ServoPanelView.xaml 的交互逻辑
/// </summary>
public partial class ServoPanelView : UserControl
{
    public ServoPanelViewModel ViewModel { get; }
    private ILogger<ServoPanelView> _logger;

    public ServoPanelView(ILogger<ServoPanelView> logger)
    {
        DataContext = this;
        ViewModel = App.GetService<ServoPanelViewModel>();
        _logger = logger;

        InitializeComponent();
    }


    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // 连接 使能
        // 要可屏蔽
     

        for (int i = 0; i < 4; i++)
        {
            Button button = new Button
            {
                Content = $"目标点{i}",
                Margin = new Thickness(0,5,0,0),
                Tag = i
            };
            button.Click += Button_Click;
            button.SetBinding(Button.IsEnabledProperty, new Binding($"ViewModel.IsEnable"));
            GotoStack.Children.Add(button);
        }

        // 生成DO表显示
       
        //try
        //{
        //    //ViewModel.ServoClient.Init();
        //    await Conn();

        //}
        //catch (Exception ex)
        //{



        //}

        foreach (var item in ViewModel.ServoClient.DOFunTable)
        {
            // 显示do功能和其值
            Label label = new Label { Content = item.Key };
            Label label1 = new Label { Content = item.Value, Height=30 };
            label1.SetBinding(Label.ContentProperty, new Binding($"ViewModel.DoFuncValue[{item.Value.Idx}]"));
            //DockPanel dockPanel = new DockPanel();
            DOTable.Children.Add(label);
            DOTable.Children.Add(label1);
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ServoClient.MoveToAsync((int)(sender as ContentControl).Tag);
    }

 


    private async void EnableBtn_Click(object sender, RoutedEventArgs e)
    {
        if (EnableBtn.IsChecked ?? false)
        {
            // 想好异常处理模式！！！
            await ViewModel.ServoClient.AddVDI( ServoModbus.DIFuncType.伺服使能);
            _logger.LogInformation("使能成功");
            ViewModel.IsEnable = true;
            //EnableBtn.IsChecked = false;
        }
        else
        {
            _logger.LogInformation("取消");
            await ViewModel.ServoClient.RemoveVDI(ServoModbus.DIFuncType.伺服使能);

            ViewModel.IsEnable = false;

        }
    }

    private void EnableBtn_Checked(object sender, RoutedEventArgs e)
    {
       
    }

    private void EnableBtn_Unchecked(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("取消");
        ViewModel.DisConnect();
    }

    private void PosMove_Checked(object sender, RoutedEventArgs e)
    {

    }

    private async void PosMove_MouseDown(object sender, MouseButtonEventArgs e)
    {
        await ViewModel.ServoClient.PosMove();

    }

    private async void NagMove_MouseDown(object sender, MouseButtonEventArgs e)
    {

        await ViewModel.ServoClient.NagMove();
    }

    private async void PosMove_MouseUp(object sender, MouseButtonEventArgs e)
    {
        await ViewModel.ServoClient.StopMove();

    }

    private async void NagMove_MouseUp(object sender, MouseButtonEventArgs e)
    {
        await ViewModel.ServoClient.StopMove();

    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void ReturnZero_Click(object sender, RoutedEventArgs e)
    {
        // 归原时其他都不允许动
        _logger.LogInformation("手动归原");
        await ViewModel.ServoClient.ReturnZeroAsync();
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("手动断开连接");

        // 尝试去连接
        if (ViewModel.IsConnect)
        {
            ViewModel.DisConnect();
        }
        else
        {
            if (await ViewModel.Conn())
            {
                _logger.LogInformation("连接成功");
            }
            else
            {
                _logger.LogInformation("连接失败");
                ConnectBtn.IsChecked = false;
            }
        }
        
    }

    private async void ResetBtn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ServoClient.ResetAlarm();

    }
}
