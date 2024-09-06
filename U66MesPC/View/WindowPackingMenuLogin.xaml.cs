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
    /// Interaction logic for WindowPackingMenuLogin.xaml
    /// </summary>
    public partial class WindowPackingMenuLogin : Window
    {
        public PackingMenuViewModel Model;
        public WindowPackingMenuLogin(StationBase station)
        {
            InitializeComponent();
            Model = new PackingMenuViewModel(station);
            if (Model.CloseWindowAction == null)
                Model.CloseWindowAction = new Action(Close);
            DataContext = Model;
        }
    }
}
