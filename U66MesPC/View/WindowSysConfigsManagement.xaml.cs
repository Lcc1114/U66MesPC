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
    /// WindowSysConfigsManagement.xaml 的交互逻辑
    /// </summary>
    public partial class WindowSysConfigsManagement : Window
    {
        private SysconfigsManagementViewModel _model;
        public WindowSysConfigsManagement()
        {
            InitializeComponent();
            Loaded += WindowSysConfigsManagement_Loaded;
        }

        private void WindowSysConfigsManagement_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = _model = new SysconfigsManagementViewModel();
        }

        private void OnRemoveBtnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (DataContext is SysconfigsManagementViewModel model)
            {
                model.OnDeleteSysConfigCommand.Execute(null);
            }
        }

        private void OnEditBtnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (DataContext is SysconfigsManagementViewModel model)
            {
                model.OnEditSysConfigCommand.Execute(null);
            }
        }
    }
}
