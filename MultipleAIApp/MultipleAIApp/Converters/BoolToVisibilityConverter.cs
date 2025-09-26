using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MultipleAIApp.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool CollapseOnFalse { get; set; } = true;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool flag)
        {
            return flag ? Visibility.Visible : (CollapseOnFalse ? Visibility.Collapsed : Visibility.Visible);
        }

        return CollapseOnFalse ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility visibility && visibility == Visibility.Visible;
}
