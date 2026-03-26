using System.Globalization;

namespace AwsSaaC03Practice;

/// <summary>Returns Visibility=True when string is non-empty.</summary>
public class StringHelper : IValueConverter
{
    public static readonly StringHelper IsNotEmpty = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Returns true when collection Count > 0.</summary>
public class CountToVisibility : IValueConverter
{
    public static readonly CountToVisibility Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int count && count > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Inverts a boolean.</summary>
public class InvertBool : IValueConverter
{
    public static readonly InvertBool Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>Maps a 0–100 percent to a pixel width (max 200) for progress bars.</summary>
public class PercentToWidth : IValueConverter
{
    public static readonly PercentToWidth Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? d * 2.0 : 0.0; // 100% = 200px

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
