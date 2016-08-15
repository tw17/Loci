using System;
using System.Globalization;
using System.Windows.Data;

namespace Loci.Converters
{
    public class ValidStringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var typedValue = value as string;
            return !string.IsNullOrEmpty(typedValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
