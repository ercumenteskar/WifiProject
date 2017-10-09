using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WifiWPFClient
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private void Application_Startup(object sender, StartupEventArgs e)
    {
      AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Exception ex = ((Exception)e.ExceptionObject);
      MessageBox.Show(ex.StackTrace.ToString());
      MessageBox.Show(ex.ToString());
    }
  }
}
