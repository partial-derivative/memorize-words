namespace Memorize_words
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            InitializePreferences();

            MainPage = new NavigationPage(new MainPage());
        }
        private DateTime _lastActiveDate = DateTime.Today;

        protected override void OnStart()
        {
            _lastActiveDate = DateTime.Today;
        }

        protected override void OnResume()
        {
            if (_lastActiveDate != DateTime.Today)
            {
                MessagingCenter.Send(this, "DateChangedWhileBackgrounded");
            }

            _lastActiveDate = DateTime.Today;
        }
        private void InitializePreferences()
        {
            if (!Preferences.ContainsKey("TargetDate"))
            {
                Preferences.Set(
                    "TargetDate",
                    DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd")
                );
            }

            if (!Preferences.ContainsKey("WeekStart"))
            {
                Preferences.Set("WeekStart", "Sunday");
            }
        }
    }
}
