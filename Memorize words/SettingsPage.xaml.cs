namespace Memorize_words;

public partial class SettingsPage : ContentPage
{

    public SettingsPage()
    {
        InitializeComponent();

        var mode = Preferences.Get("WeekStart", "Monday");
        WeekStartPicker.SelectedItem = mode;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string mode = WeekStartPicker.SelectedItem?.ToString() ?? "Monday";

        Preferences.Set("WeekStart", mode);

        MessagingCenter.Send<object>(this, "WeekStartChanged");

        await Navigation.PopAsync();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }


}