using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MultipleAIApp.Models;

namespace MultipleAIApp.Converters;

public sealed class ChatRoleToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatRole role)
        {
            var resourceKey = role switch
            {
                ChatRole.User => "SystemFillColorAttentionBrush",
                ChatRole.Assistant => "SystemFillColorSuccessBrush",
                ChatRole.System => "SystemFillColorNeutralBrush",
                _ => "SystemFillColorSubtleBrush"
            };

            if (Application.Current.Resources.TryGetValue(resourceKey, out var brush) && brush is Brush typedBrush)
            {
                return typedBrush;
            }
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => DependencyProperty.UnsetValue;
}

