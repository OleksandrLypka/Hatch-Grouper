using System.Windows;
using System.Windows.Threading;

namespace HatchGrouper
{
    public partial class App : Application
    {
        public App()
        {
            // Last line of defence: show a readable message instead of crashing
            DispatcherUnhandledException += OnUnhandledException;
        }

        private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                Loc.T("err.unhandled", e.Exception.Message),
                Loc.T("err.title"), MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
