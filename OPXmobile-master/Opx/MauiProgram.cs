using CommunityToolkit.Maui;
using Controls.UserDialogs.Maui;
namespace Opx
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                 .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                  .UseUserDialogs(true, () =>
                  {
#if ANDROID
                      var fontFamily = "OpenSans-Default.ttf";
#else
                      var fontFamily = "OpenSans-Regular";
#endif
                      AlertConfig.DefaultMessageFontFamily = fontFamily;
                      AlertConfig.DefaultUserInterfaceStyle = UserInterfaceStyle.Light;
                      AlertConfig.DefaultBackgroundColor = Colors.White;
                      AlertConfig.DefaultMessageColor = Colors.Black;
                      AlertConfig.DefaultTitleColor = Colors.Purple;
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
                    //fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
               .ConfigureEffects(effects =>
               {
               });
            builder.Services.AddTransient<MainPage>();
            return builder.Build();
        }
    }
}