using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace U66MesPC.Model
{
    public class BKConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string msg = value.ToString().ToLower();
            if (msg.Contains("fail"))
                return new SolidColorBrush(Colors.DarkRed);
            else if (msg.Contains("error"))
                return new SolidColorBrush(Colors.Orange);
            else
                return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
