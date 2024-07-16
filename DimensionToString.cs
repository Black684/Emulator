using System.Windows.Data;
using System;
using System.Globalization;
using Sensors;

namespace Emulator
{
    public class DimensionToString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Dimension)value)
            {
                case Dimension.Percent:
                    return "%";
                default:
                    return "123";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();

        }
    }
}