using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WifiSolution
{
  public class WinFuncs
  {
    private String ProjectName = "";
    private String RegistryPath { get { return @"Software\" + ProjectName;  } }
    public WinFuncs(String projectName)
    {
      ProjectName = projectName;
    }

    public void WriteToRegistry(String name, object value)
    {
      Registry.CurrentUser.CreateSubKey(RegistryPath).SetValue(name, value);
    }

    public object ReadFromRegistry(String name, String HKEY = "U", String Path = null)
    {
      RegistryKey key = Registry.CurrentUser;
      if (HKEY == "M") key = Registry.LocalMachine;
      return key.OpenSubKey(Path ?? RegistryPath)?.GetValue(name) ?? "";
    }

    public int ShowMessageBox(String msg, String caption="", int buttons = 0, int msgimg=0)
    {
      return (int)MessageBox.Show(msg, caption, (MessageBoxButton)buttons, (MessageBoxImage)msgimg);
    }

    public void Shutdown()
    {
      Application.Current.Shutdown();
    }
    public String Netsh(String args)
    {
      return CmdExec("netsh.exe", args);
    }
    public String CmdExec(String app, String args)
    {
      using (Process p1 = new Process())
      {
        p1.StartInfo.FileName = app;
        p1.StartInfo.Arguments = args;
        p1.StartInfo.UseShellExecute = false;
        p1.StartInfo.RedirectStandardOutput = true;
        p1.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        p1.StartInfo.CreateNoWindow = true;
        p1.StartInfo.Verb = "runas";
        p1.Start();
        String rtn = p1.StandardOutput.ReadToEnd();
        return rtn;
      }
    }

  }
}
