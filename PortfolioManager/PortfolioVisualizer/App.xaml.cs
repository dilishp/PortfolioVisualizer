using System.Net.WebSockets;

namespace PortfolioVisualizer
{
    public partial class App : Application
    {
        private ClientWebSocket _ws = new ClientWebSocket();

        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }
    }
}