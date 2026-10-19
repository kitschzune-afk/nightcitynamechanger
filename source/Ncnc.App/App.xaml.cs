using System.Windows;
namespace Ncnc;
public partial class App : Application
{
    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    protected override void OnStartup(StartupEventArgs e) { SetCurrentProcessExplicitAppUserModelID("NcNc.NightCityNameChanger"); base.OnStartup(e); }
}
