// <copyright file="ColorToSolidColorBrushValueConverter.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DisplayPluginLibrary.Converters
{
    /// <summary>
    ///     Media color to solid color brush converter.
    /// </summary>
    public class ColorToSolidColorBrushValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}