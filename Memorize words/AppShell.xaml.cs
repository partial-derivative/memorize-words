namespace Memorize_words
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Shell.SetFlyoutBehavior(this, FlyoutBehavior.Disabled);
            Shell.SetNavBarIsVisible(this, true);
            FlyoutBehavior = FlyoutBehavior.Disabled;
        }
    }
}
