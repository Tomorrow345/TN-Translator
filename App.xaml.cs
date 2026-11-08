using System.Configuration;
using System.Data;
using System.Windows;

namespace TN_Translator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            var notifyIcons = System.Windows.Application.Current.Windows.OfType<MainWindow>();
            foreach (var window in notifyIcons)
            {
                window.TrayIcon?.Dispose();
            }
            base.OnExit(e);
        }
    }

}
