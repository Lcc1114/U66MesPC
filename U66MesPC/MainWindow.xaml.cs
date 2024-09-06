using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using U66MesPC.Common;
using U66MesPC.Common.Station;
using U66MesPC.Dal;
using U66MesPC.View;
using U66MesPC.ViewModel;

namespace U66MesPC
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //private Timer timer = new Timer();
        public MainWindowViewModel Model;

        public MainWindow()
        {
            InitializeComponent();
            Model = new MainWindowViewModel();
            //if (Model.CloseWindowAction == null)
            //    Model.CloseWindowAction = new Action(Close);
            DataContext = Model;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("确定退出软件吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LogsUtil.Instance.WriteInfo($"软件退出......");
                foreach (TabItem tabItem in uiTabControl.Items)
                {
                    UCStation content = (UCStation)tabItem.Content;
                    //content?.Model.Release();
                }
            }
            else
            {
                e.Cancel = true;
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            using (DBContext db = new DBContext())
            {
                db.SysConfigs.ToList().ForEach(item =>
                {
                    if (item.StationType == StationType.None) { return; }
                    TabItem tabItem = new TabItem() { Header = item.StationID };
                    UCStation content = new UCStation(item) { StationType = item.StationType };
                    tabItem.Content = content;
                    uiTabControl.Items.Add(tabItem);
                });
            }
            uiTabControl.SelectedIndex = 0;
            uiDataGridMesLogInfo.ItemsSource = LogsUtil.Instance.LogMesInfoList;
            uiDataGridLogInfo.ItemsSource = LogsUtil.Instance.LogMsgInfoList;
            uiMesConnStatusListBox.ItemsSource = Model.StationInfoList;
            LogsUtil.Instance.eventHandlerMESTextChang += MESContextChang;
            LogsUtil.Instance.eventHandlerLogTextChang += LogContextChang;
            IPAddress[] iPAddresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray();
            string ip = "";
            for (int i = 0; i < iPAddresses.Length; i++)
            {
                ip += iPAddresses[i] + ";";
            }
            Model.MES_IP = "工站   ||   " + ip;
        }

        private void MESContextChang(object sender, EventArgs e)
        {
            try
            {
                uiDataGridMesLogInfo.ScrollIntoView(uiDataGridMesLogInfo.Items[Convert.ToInt32(sender)]);
            }
            catch (Exception ex) { }
        }
        private void LogContextChang(object sender, EventArgs e)
        {
            try
            {
                uiDataGridLogInfo.ScrollIntoView(uiDataGridLogInfo.Items[Convert.ToInt32(sender)]);
            }
            catch (Exception ex) { }
        }
    }
}
