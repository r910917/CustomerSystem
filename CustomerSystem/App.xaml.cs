namespace CustomerSystem
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
            var navigationPage = new NavigationPage(new MainPage());
            navigationPage.BarBackgroundColor = Colors.Transparent;
            navigationPage.BarTextColor = Colors.Transparent;
            MainPage = navigationPage;

            // 檢查自動登入
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            if (isLoggedIn)
            {
                MainPage = new NavigationPage(new Pages.HomePage());
            }
            else
            {
                MainPage = new NavigationPage(new MainPage());
            }
        }
    }
}
