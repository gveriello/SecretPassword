using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SecretPassword.Converters
{
    public class VisibleIfValidString : IValueConverter
    {
        public VisibleIfValidString()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringToCheck = value.ToString();
            if (string.IsNullOrEmpty(stringToCheck))
                return Visibility.Collapsed;

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
