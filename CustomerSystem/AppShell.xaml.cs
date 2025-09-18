using CustomerSystem.Pages;

namespace CustomerSystem
{

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // 註冊頁面
            Routing.RegisterRoute(nameof(Pages.HomePage), typeof(Pages.HomePage));
        }

    }
}
