using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MonLingo.Core.Converters
{
    /// <summary>
    /// Boolean 反轉轉換器
    /// true -> false, false -> true
    /// </summary>
    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return true;
        }
    }

    /// <summary>
    /// 多個 Boolean 值到 Visibility 的轉換器
    /// 所有值都為 true 時 -> Visible，否則 -> Collapsed
    /// </summary>
    public class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return Visibility.Collapsed;

            // 檢查所有值是否都為 true
            foreach (var value in values)
            {
                if (!(value is bool boolValue) || !boolValue)
                {
                    return Visibility.Collapsed;
                }
            }

            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("MultiBooleanToVisibilityConverter 不支援反向轉換");
        }
    }

    /// <summary>
    /// Boolean 到 Visibility 的轉換器
    /// true -> Visible, false -> Collapsed
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 檢查是否需要反轉 (參數為 "Invert" 或 "Reverse")
                bool invert = parameter != null && 
                             (parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                              parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase));

                if (invert)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                // 檢查是否需要反轉
                bool invert = parameter != null && 
                             (parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                              parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase));

                if (invert)
                {
                    return visibility != Visibility.Visible;
                }
                else
                {
                    return visibility == Visibility.Visible;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 反向的 Boolean 到 Visibility 轉換器
    /// true -> Collapsed, false -> Visible
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }

            return true;
        }
    }

    /// <summary>
    /// Null 到 Visibility 的轉換器
    /// null -> Collapsed, 非null -> Visible
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 檢查是否需要反轉
            bool invert = parameter != null && 
                         (parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                          parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase));

            bool isNull = value == null || 
                         (value is string str && string.IsNullOrEmpty(str)) ||
                         (value is int intVal && intVal == 0) ||
                         (value is double doubleVal && doubleVal == 0.0);

            if (invert)
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isNull ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NullToVisibilityConverter 不支援反向轉換");
        }
    }

    /// <summary>
    /// 字串到 Visibility 的轉換器
    /// 空字串或null -> Collapsed, 非空字串 -> Visible
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter != null && 
                         (parameter.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
                          parameter.ToString().Equals("Reverse", StringComparison.OrdinalIgnoreCase));

            bool isEmpty = string.IsNullOrEmpty(value?.ToString());

            if (invert)
            {
                return isEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StringToVisibilityConverter 不支援反向轉換");
        }
    }
}
