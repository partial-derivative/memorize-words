namespace Memorize_words
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

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
    }
}
