using System.Globalization;

namespace Opx.Renderers
{
    public class StatusToGradientStartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status?.ToLowerInvariant() switch
                {
                    "Confirmed" => Colors.ForestGreen, // Green for completed
                    "Cancelled" => Colors.Purple,    // Red for failed
                    "pending" => Colors.Red,   // Orange for pending
                    _ => Color.FromArgb("#A25AC4")            // Default purple
                };
            }
            return Color.FromArgb("#A25AC4"); // Default purple
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for text color in date section
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status?.ToLowerInvariant() switch
                {
                    "Confirmed" => Colors.ForestGreen, // Green for completed
                    "Cancelled" => Colors.Purple,    // Red for failed
                    "pending" => Colors.Red,   // Orange for pending
                    _ => Color.FromArgb("#A25AC4")            // Default purple
                };
            }
            return Color.FromArgb("#A25AC4"); // Default purple
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}