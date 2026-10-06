using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Opx.Views.InApp
{
    public class ContractPreviewSheet : Popup
    {
        public const decimal VatRate = 0.01m;   // 1% of the contract amount

        private readonly Frame _mainFrame;

        /// <summary>VAT for an amount, rounded to kobo.</summary>
        public static decimal CalculateVat(decimal amount) => Math.Round(amount * VatRate, 2, MidpointRounding.AwayFromZero);

        public ContractPreviewSheet(string receiverPhone, string description, decimal amount)
        {
            var vat = CalculateVat(amount);
            var total = amount + vat;

            Size = new Size(340, 470);
            Color = Colors.Transparent;
            CanBeDismissedByTappingOutsideOfPopup = false;

            var ng = CultureInfo.GetCultureInfo("en-NG");
            string Naira(decimal v) => "₦" + v.ToString("N2", ng);

            var title = new Label
            {
                Text = "Contract Preview",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1F1147"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 24, 0, 4)
            };

            var subtitle = new Label
            {
                Text = "Please review the details before you continue.",
                FontSize = 13,
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 0, 20, 16)
            };

            var details = new VerticalStackLayout
            {
                Spacing = 0,
                Margin = new Thickness(20, 0),
                Children =
            {
                Row("Receiver phone", receiverPhone),
                Row("Description", description, wrap: true),
                Row("Contract amount", Naira(amount)),
                Row("VAT (1%)", Naira(vat)),
                Divider(),
                Row("Total", Naira(total), bold: true, accent: true)
            }
            };

            var cancel = new Button
            {
                Text = "CANCEL",
                BackgroundColor = Color.FromArgb("#EEEAF8"),
                TextColor = Color.FromArgb("#3D2493"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 48
            };
            cancel.Clicked += async (_, _) => await CloseWith(false);

            var proceed = new Button
            {
                Text = "CONTINUE",
                BackgroundColor = Color.FromArgb("#6B4CE6"),
                TextColor = Colors.White,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                HeightRequest = 48
            };
            proceed.Clicked += async (_, _) => await CloseWith(true);

            var buttons = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12,
                Margin = new Thickness(20, 22, 20, 22)
            };
            buttons.Add(cancel, 0, 0);
            buttons.Add(proceed, 1, 0);

            _mainFrame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 24,
                Padding = 0,
                HasShadow = true,
                Content = new VerticalStackLayout { Children = { title, subtitle, details, buttons } }
            };

            Content = _mainFrame;
            AnimateIn();
        }

        private static View Row(string label, string value, bool bold = false, bool accent = false, bool wrap = false)
        {
            var grid = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 16,
                Padding = new Thickness(0, 9)
            };
            grid.Add(new Label
            {
                Text = label,
                FontSize = 13,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Start
            }, 0, 0);
            grid.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "-" : value,
                FontSize = bold ? 17 : 14,
                FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
                TextColor = accent ? Color.FromArgb("#6B4CE6") : Colors.Black,
                HorizontalTextAlignment = TextAlignment.End,
                LineBreakMode = wrap ? LineBreakMode.WordWrap : LineBreakMode.TailTruncation,
                MaxLines = wrap ? 3 : 1
            }, 1, 0);
            return grid;
        }

        private static View Divider() => new BoxView
        {
            HeightRequest = 1,
            Color = Color.FromArgb("#E4E0F2"),
            Margin = new Thickness(0, 4)
        };

        private async void AnimateIn()
        {
            try
            {
                _mainFrame.TranslationY = 400;
                _mainFrame.Opacity = 0;
                await Task.WhenAll(
                    _mainFrame.TranslateTo(0, 0, 300, Easing.CubicOut),
                    _mainFrame.FadeTo(1, 300));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ContractPreviewSheet animation error: {ex.Message}");
            }
        }

        private async Task CloseWith(bool proceed)
        {
            try
            {
                await Task.WhenAll(
                    _mainFrame.TranslateTo(0, 400, 220, Easing.CubicIn),
                    _mainFrame.FadeTo(0, 220));
            }
            catch { /* animation is cosmetic */ }

            await CloseAsync(proceed);
        }
    }
}
