using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using U66MesPC.Model;
using U66MesPC.ViewModel;

namespace U66MesPC.View
{
    /// <summary>
    /// UCStation.xaml 的交互逻辑
    /// </summary>
    public partial class UCStation : UserControl
    {
        private bool _bLoaded;
        public StationViewModel Model;
        private SysConfigs _sysConfig;
        public UCStation(SysConfigs sysConfigs)
        {
            InitializeComponent();
            _sysConfig = sysConfigs;
            Unloaded += UCStation_Unloaded;
            Loaded += UCStation_Loaded;
            Dispatcher.ShutdownStarted += (object sender, EventArgs e) =>
            {
                Model?.Release();
            };
        }

        private void UCStation_Loaded(object sender, RoutedEventArgs e)
        {
            if (_bLoaded) return;
            DataContext = Model = new StationViewModel(StationType, _sysConfig);
            var parent = Window.GetWindow(this) as MainWindow;
            parent.Model.AddStationInfo(Model.Station);
            if (StationType == StationType.Binning)
                uiOperateBtn.Visibility = Visibility.Visible;
            var bHiddenManualBtn = Convert.ToInt32(ConfigurationManager.AppSettings["ManualButton"]) == 0;
            if (bHiddenManualBtn)
                uiManualBtn.Visibility = Visibility.Collapsed;
            _bLoaded = true;
        }
        private void UCStation_Unloaded(object sender, RoutedEventArgs e)
        {

        }
        public StationType StationType
        {
            get { return (StationType)GetValue(StationTypeProperty); }
            set { SetValue(StationTypeProperty, value); }
        }

        public static readonly DependencyProperty StationTypeProperty =
            DependencyProperty.Register("StationType", typeof(StationType), typeof(UCStation), new PropertyMetadata(StationType.None, null));

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Model?.GlueCheckOutCommand.Execute(null);
        }

        /// <summary>
        /// 更新配置文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void UpdateConfig(string key, string value)
        {
            Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfa.AppSettings.Settings[key].Value = value;
            cfa.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        private void WriteSN(object sender, RoutedEventArgs e)
        {
            //FeedingCheckRequest request = new FeedingCheckRequest(_sysConfig, txtToolingSN.Text.Trim());
            //Task<FeedingCheckResponse> response = HttpClientHelper.SNFeedingCheckAsync(request, _sysConfig.Url);
            //bool ret = response?.Result.Result.ToLower() == "pass";
            //if (!ret)
            //{
            //    Task.Run(() => { MessageBox.Show($"工站：{_sysConfig.StationID}{Environment.NewLine}事件：({response.Result.EventID}){Environment.NewLine}结果:{response.Result.Result};{Environment.NewLine}返回信息:{response.Result.Msg};", "错误", MessageBoxButton.OK, MessageBoxImage.Error); });
            //}
            //else
            //{
            //    Task.Run(new Action(() =>
            //    {
            //        MessageBox.Show($"工站：{_sysConfig.StationID}{Environment.NewLine}事件：({response.Result.EventID}){Environment.NewLine}" +
            //            $"结果:{response.Result.Result};{Environment.NewLine}返回信息:{response.Result.Msg};", "Tooling寿命管控");
            //    }));
            //}
        }
    }
}
