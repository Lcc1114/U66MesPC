using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using U66MesPC.Model;
using U66MesPC.ViewModel;

namespace U66MesPC.View
{
    /// <summary>
    /// WindowConfiguration.xaml 的交互逻辑
    /// </summary>
    public partial class WindowSysConfigs : Window
    {
        private SysConfigsViewModel _model;
        public WindowSysConfigs(SysConfigs sysConfigs)
        {
            InitializeComponent();
            _model = new SysConfigsViewModel(sysConfigs);
            if (_model.CloseWindowAction == null)
                _model.CloseWindowAction = new Action(Close);
            DataContext = _model;
        }

        private void Button_Click()
        {

        }
    }
}
