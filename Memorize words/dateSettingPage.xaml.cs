namespace Memorize_words;

public partial class dateSettingsPage : ContentPage
{

    public dateSettingsPage()
    {
        InitializeComponent();

        // 读取已有日期
        var saved = Preferences.Get("TargetDate", "");
        if (DateTime.TryParse(saved, out var dt))
        {
            TargetDatePicker.Date = dt;
        }
        else
        {
            // 首次启动默认过去日期（示例：1天前）
            var defaultDate = DateTime.Today.AddDays(-1);

            TargetDatePicker.Date = defaultDate;
            Preferences.Set("TargetDate", defaultDate.ToString("yyyy-MM-dd"));
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selected = TargetDatePicker.Date;

        //// ✔ 只允许未来日期
        //if (selected <= DateTime.Today)
        //{
        //    await DisplayAlert("错误", "请选择未来日期", "OK");
        //    return;
        //}

        Preferences.Set("TargetDate", selected.ToString("yyyy-MM-dd"));
        Preferences.Set("WeekStart", "Sunday"); // or Sunday

        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
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