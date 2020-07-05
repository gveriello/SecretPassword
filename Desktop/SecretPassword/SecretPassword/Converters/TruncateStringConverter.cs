using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SecretPassword.Converters
{
    public class TruncateStringConverter : IValueConverter
    {
        public TruncateStringConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringToTruncate = value?.ToString();

            if (string.IsNullOrEmpty(stringToTruncate))
                return stringToTruncate;

            int maxLength = 100;
            int.TryParse(parameter.ToString(), out maxLength);

            if (string.IsNullOrEmpty(stringToTruncate))
                return stringToTruncate;

            if (stringToTruncate.Length > maxLength)
                return stringToTruncate.Substring(0, maxLength) + "...";

            return stringToTruncate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
