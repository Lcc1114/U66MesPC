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
using U66MesPC.Dal;
using U66MesPC.Model;
using U66MesPC.ViewModel;

namespace U66MesPC.View
{
    /// <summary>
    /// ToolingSNView.xaml 的交互逻辑
    /// </summary>
    public partial class ToolingSNView : Window
    {
        ToolingSNandVersionViewModel TSNVM;
        public ToolingSNView()
        {
            InitializeComponent();
            TSNVM = DataContext as ToolingSNandVersionViewModel;
            Loaded += ToolingSNView_Loaded;
        }

        private void ToolingSNView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = TSNVM = new ToolingSNandVersionViewModel();
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(HeadertxtBox.Text))
            {
                TSNVM.AddToolingSNandVersionModel(HeadertxtBox.Text);
                HeadertxtBox.Clear();
            }
            else
            {
                MessageBox.Show("Header Cannot be Empty");
            }
        }

        private void AddString_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            TextBox txt = (bt?.Parent as StackPanel)?.Children[0] as TextBox;
            ToolingSNandVersionModel toolingSNandVersionModel = txt.Tag as ToolingSNandVersionModel;
            if (toolingSNandVersionModel != null && !string.IsNullOrWhiteSpace(txt?.Text))
            {
                TSNVM.AddToolingSNandVersionModelList(toolingSNandVersionModel, txt.Text);
                txt.Clear();
            }
            else
            {
                MessageBox.Show("Severial Cannot be Empty");
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            TextBox txt = (bt?.Parent as StackPanel)?.Children[0] as TextBox;
            ToolingSNandVersionModel toolingSNandVersionModel = txt.Tag as ToolingSNandVersionModel;
            if (toolingSNandVersionModel != null && !string.IsNullOrWhiteSpace(txt?.Text))
            {
                TSNVM.DeleteToolingSNandVersionModelList(toolingSNandVersionModel, txt.Text);
                txt.Clear();
                MessageBox.Show("删除成功");
            }
            else
            {
                MessageBox.Show("删除失败");
            }
        }
    }
}
