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
using U66MesPC.ViewModel;

namespace U66MesPC.View
{
    /// <summary>
    /// WindowLogin.xaml 的交互逻辑
    /// </summary>
    public partial class WindowLogin : Window
    {
        public LoginViewModel Model;
        public WindowLogin()
        {
            InitializeComponent();
            Model = new LoginViewModel();
            if (Model.CloseWindowAction == null)
                Model.CloseWindowAction = new Action(Close);
            DataContext = Model;
        }
    }
}
