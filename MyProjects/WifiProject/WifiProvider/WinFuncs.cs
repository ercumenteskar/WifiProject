using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WifiProvider
{
  class WinFuncs
  {
    private String ProjectName = "";
    private String RegistryPath = "";
    public WinFuncs(String projectName)
    {
      this.ProjectName = projectName;
      this.RegistryPath = @"Software\"+projectName;
    }

    public void WriteToRegistry(String name, object value)
    {
      Registry.CurrentUser.CreateSubKey(RegistryPath).SetValue(name, value);
    }

    public object ReadFromRegistry(String name)
    {
      return Registry.CurrentUser.OpenSubKey(RegistryPath)?.GetValue(name) ?? "";
    }

    public int ShowMessageBox(String msg, String caption="", int buttons = 0, int msgimg=0)
    {
      return (int)MessageBox.Show(msg, caption, (MessageBoxButton)buttons, (MessageBoxImage)msgimg);
    }

    public void Shutdown()
    {
      Application.Current.Shutdown();
    }
  }
}
