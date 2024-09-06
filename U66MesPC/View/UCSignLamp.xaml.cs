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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace U66MesPC.View
{
    /// <summary>
    /// UCSignLamp.xaml 的交互逻辑
    /// </summary>
    public partial class UCSignLamp : UserControl
    {
        public UCSignLamp()
        {
            InitializeComponent();
        }
        public bool LampState
        {
            get { return (bool)GetValue(LampStateProperty); }
            set { SetValue(LampStateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LampState.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LampStateProperty =
            DependencyProperty.Register("LampState", typeof(bool), typeof(UCSignLamp), new PropertyMetadata(false, OnLampStateChanged));
        private static void OnLampStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UCSignLamp ucinput = (UCSignLamp)d;
            Ellipse ellipse = ucinput.bLamp;
            if ((bool)e.NewValue == true)
            {
                ellipse.Fill = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ellipse.Fill = new SolidColorBrush(Colors.Gray);
            }
        }
    }
}
