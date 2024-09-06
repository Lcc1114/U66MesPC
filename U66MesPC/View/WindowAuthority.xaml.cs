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

namespace U66MesPC.View
{
    /// <summary>
    /// Interaction logic for WindowAuthority.xaml
    /// </summary>
    public partial class WindowAuthority : Window
    {
        public WindowAuthority()
        {
            InitializeComponent();
        }
        
        private void OnConfirmBtnClick(object sender, RoutedEventArgs e)
        {
            if(uiTxtPwd.Text=="")//149823
            {
                Close();
                WindowSysConfigsManagement window = new WindowSysConfigsManagement();
                window.ShowDialog();
                
            }
            else
            {
                MessageBox.Show("密码错误，请重新输入！", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                uiTxtPwd.Text = "";
            }
        }
    }
}
