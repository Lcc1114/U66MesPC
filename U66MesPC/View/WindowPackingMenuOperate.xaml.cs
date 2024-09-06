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
using U66MesPC.Common.Station;
using U66MesPC.Model;
using U66MesPC.ViewModel;

namespace U66MesPC.View
{
    /// <summary>
    /// Interaction logic for WindowPackingMenuOperate.xaml
    /// </summary>
    public partial class WindowPackingMenuOperate : Window
    {
        private PackingMenuOperateViewModel Model;
        public WindowPackingMenuOperate(UserInfo userInfo,StationBase station)
        {
            InitializeComponent();
            Model = new PackingMenuOperateViewModel(userInfo,station);
            //if (Model.CloseWindowAction == null)
            //    Model.CloseWindowAction = new Action(Close);
            DataContext = Model;
            Loaded += WindowPackingMenuOperate_Loaded;
        }

        private void WindowPackingMenuOperate_Loaded(object sender, RoutedEventArgs e)
        {
            uiCmbLocationName.ItemsSource = Model.LocationIDList;
        }
    }
}
