using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace Loci.Converters
{
    public class CollectionToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var infos = value as ObservableCollection<CultureInfo>;
            if (infos != null)
            {
                return infos.Count != 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
