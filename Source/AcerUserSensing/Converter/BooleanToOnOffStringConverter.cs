using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Data;

namespace AcerUserSensing.Converter
{
    class BooleanToOnOffStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str_on = "On";
            string str_off = "Off";
            try
            {
                ResourceManager rm = new ResourceManager("AcerUserSensing.Properties.Resources", Assembly.GetExecutingAssembly());
                str_on = rm.GetString("strOn");
                str_off = rm.GetString("strOff");
            }
            catch
            {

            }

            return (value is Boolean && (Boolean)value == true) ? str_on : str_off;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
