using System.Globalization;

namespace Opx.Renderers
{
    public class StatusToGradientConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var status = value?.ToString()?.ToLower();
                return status switch
                {
                    "active" => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Colors.Red, Offset = 0.0f },
                            new GradientStop { Color = Colors.Black, Offset = 1.0f }
                        }
                    },
                    "pending" => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Colors.Purple, Offset = 0.0f },
                            new GradientStop { Color = Colors.Black, Offset = 1.0f }
                        }
                    },
                    "completed" => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Colors.ForestGreen, Offset = 0.0f },
                            new GradientStop { Color = Colors.Black, Offset = 1.0f }
                        }
                    },
                    "cancelled" => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Colors.Black, Offset = 0.0f },
                            new GradientStop { Color = Colors.DarkGray, Offset = 1.0f }
                        }
                    },
                    "expired" => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Color.FromArgb("#9E9E9E"), Offset = 0.0f },
                            new GradientStop { Color = Color.FromArgb("#616161"), Offset = 1.0f }
                        }
                    },
                    _ => new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = Color.FromArgb("#607D8B"), Offset = 0.0f },
                            new GradientStop { Color = Color.FromArgb("#455A64"), Offset = 1.0f }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Status gradient converter error: {ex.Message}");
                return new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#607D8B"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#455A64"), Offset = 1.0f }
                    }
                };
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
