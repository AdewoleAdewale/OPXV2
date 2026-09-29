using CommunityToolkit.Maui;
using Controls.UserDialogs.Maui;

namespace Opx
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp() => BuildMauiApp();

        private static MauiApp BuildMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUserDialogs()
                .UseUserDialogs(true, () =>
                {
                    var fontFamily = GetFontFamily();
                    AlertConfig.DefaultMessageFontFamily = fontFamily;
                    AlertConfig.DefaultUserInterfaceStyle = UserInterfaceStyle.Dark;
                    AlertConfig.DefaultPositiveButtonTextColor = Colors.Purple;
                    ConfirmConfig.DefaultMessageFontFamily = fontFamily;
                    ActionSheetConfig.DefaultMessageFontFamily = fontFamily;
                    ToastConfig.DefaultMessageFontFamily = fontFamily;
                    SnackbarConfig.DefaultMessageFontFamily = fontFamily;
                    HudDialogConfig.DefaultMessageFontFamily = fontFamily;
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Default.ttf", "OpenSans");
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Register pages for DI
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<AddAccountPage>();
            // ... add other pages as needed

            return builder.Build();
        }

        private static string GetFontFamily()
        {
#if ANDROID
            return "OpenSans-Default.ttf";
#else
            return "OpenSans-Regular";
#endif
        }
    }
}