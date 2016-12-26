using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WifiWPFClient
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    public App()
      : base()
    {
      this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
    }

    void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(e.Exception, true);
      string errorMessage = e.Exception.InnerException + "Source : " + e.Exception.Source + " Line Number : " + trace.GetFrame(0).GetFileLineNumber();// Exception.Message;
      MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      e.Handled = true;
    }
  }
}
