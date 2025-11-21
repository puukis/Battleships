using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using BattleshipsLan.Core.Models;

namespace BattleshipsLan.UI.Converters;

public class TileStatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TileStatus status)
        {
            return status switch
            {
                TileStatus.Empty => Brushes.LightBlue,
                TileStatus.Ship => Brushes.Gray,
                TileStatus.Hit => Brushes.Red,
                TileStatus.Miss => Brushes.White,
                _ => Brushes.Transparent
            };
        }
        return Brushes.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
