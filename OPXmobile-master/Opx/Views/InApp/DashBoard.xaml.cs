using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Application = Microsoft.Maui.Controls.Application;
using Plat = Microsoft.Maui.Controls.PlatformConfiguration;
using CommunityToolkit.Maui.Views;

namespace Opx.Views;

public enum DashTab { Home, Escrow, Create, Wallet, Profile }

public partial class DashBoard : ContentView
{
    private readonly DashTab _active;
    private bool _busy;

    public DashBoard(DashTab active)
    {
        InitializeComponent();
        _active = active;
        Highlight(IconHome, LabelHome, DotHome, active == DashTab.Home);
        Highlight(IconEscrow, LabelEscrow, DotEscrow, active == DashTab.Escrow);
        Highlight(IconWallet, LabelWallet, DotWallet, active == DashTab.Wallet);
        Highlight(IconProfile, LabelProfile, DotProfile, active == DashTab.Profile);
    }

    /// <summary>Adds the bar under the page's existing content (one line per page).</summary>
    public static void Attach(ContentPage page, DashTab active)
    {
        var content = page.Content;
        var host = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            BackgroundColor = page.BackgroundColor ?? Colors.White
        };

        page.Content = null;
        if (content != null)
        {
            Grid.SetRow(content, 0);
            host.Children.Add(content);
        }

        var bar = new DashBoard(active);
        Grid.SetRow(bar, 1);
        host.Children.Add(bar);
        page.Content = host;
    }

    private static void Highlight(Image icon, Label label, BoxView dot, bool on)
    {
        icon.Opacity = on ? 1 : 0.65;
        icon.Scale = on ? 1.12 : 1;
        label.Opacity = on ? 1 : 0.7;
        label.FontAttributes = on ? FontAttributes.Bold : FontAttributes.None;
        dot.Opacity = on ? 1 : 0;
    }

    private async void OnTabTapped(object? sender, TappedEventArgs e)
    {
        if (_busy || sender is not Element tab) return;
        _busy = true;
        try
        {
            switch (tab.ClassId)
            {
                case "home":
                    if (_active != DashTab.Home) await GoHomeAsync();
                    break;
                case "escrow":
                    if (_active != DashTab.Escrow) await OpenAsync(new ContractList());
                    break;
                case "wallet":
                    if (_active != DashTab.Wallet) await OpenAsync(new CurrentBalance());
                    break;
                case "profile":
                    if (_active != DashTab.Profile) await OpenAsync(new ProfilePage());
                    break;
                case "create":
                    var page = FindPage();
                    if (page != null) await page.ShowPopupAsync(new CreateContract());
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tab navigation error: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    private Page? FindPage()
    {
        Element? e = this;
        while (e != null && e is not Page) e = e.Parent;
        return e as Page;
    }

    /// <summary>
    /// Close every page opened on top of Home. Home itself is never rebuilt: it is already underneath, so
    /// it only refreshes its virtual card and recent-transactions list (silently) when something was closed.
    /// Every page's back action should come through here instead of doing "MainPage = new Home()".
    /// </summary>
    public static async Task GoHomeAsync(bool refreshHome = true)
    {
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav == null) return;

        bool closedAny = false;
        while (nav.ModalStack.Count > 0 && nav.ModalStack[^1] is not Home)
        {
            await nav.PopModalAsync(false);
            closedAny = true;
        }

        if (closedAny && refreshHome)
            _ = Home.NotifyReturnedAsync();
    }

    private static async Task OpenAsync(Page page)
    {
        await GoHomeAsync(refreshHome: false);   // about to open another tab page, no need to refresh Home yet
        var nav = Application.Current?.MainPage?.Navigation;
        if (nav != null) await nav.PushModalAsync(page, false);
    }
}
