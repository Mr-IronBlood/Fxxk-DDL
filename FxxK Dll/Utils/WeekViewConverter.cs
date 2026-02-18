using System;
using System.Globalization;
using System.Windows.Data;

namespace FxxkDDL
{
    public class WeekViewWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values[0] is int itemCount && itemCount > 0)
                {
                    // 每个日期列宽度130px + 边距
                    double columnWidth = 130 + 2; // 130宽度 + 2边距
                    return itemCount * columnWidth;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
