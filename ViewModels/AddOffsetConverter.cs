using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WPFTasks.ViewModels
{
    public class AddOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double posY && parameter is string offsetStr && double.TryParse(offsetStr, out double offset))
            {
                return posY + offset; // Добавляем смещение к Y координате
            }
            return value; // Если значение не подходящее, возвращаем его без изменений
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
