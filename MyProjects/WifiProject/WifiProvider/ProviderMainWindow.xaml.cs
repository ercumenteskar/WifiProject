using System;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using System.Text;
using NetFwTypeLib;
using System.Linq;
using System.ComponentModel;

namespace WifiSolution.WifiProvider
{

  public partial class MainWindow
  {
    public class TextBoxOutputter : TextWriter
    {
      TextBox textBox = null;

      public TextBoxOutputter(TextBox output)
      {
        textBox = output;
      }

      public override void Write(char value)
      {
        base.Write(value);
        textBox.Dispatcher.BeginInvoke(new Action(() =>
        {
          textBox.AppendText(value.ToString());
        }));
      }

      public override Encoding Encoding
      {
        get { return System.Text.Encoding.UTF8; }
      }
    }
    public ProviderViewModel vm;
    TextBoxOutputter outputter;
    private WifiCommon wc = new WifiCommon();
    private WinFuncs wf = new WinFuncs();
    public MainWindow()
    {
      InitializeComponent();
      vm = new ProviderViewModel();
      DataContext = vm;
      vm.AfterCtor(wc, wf);
      outputter = new TextBoxOutputter(tb_Log);
      Console.SetOut(outputter);
    }

    public string vPassword
    {
      get { return vm.Account.Password; }
      set
      {
        if (vm.Account.Password != value) vm.Account.Password = value;
        if (GetPasswordBox(tb_Password) != value) SetPasswordBox(tb_Password, value);
      }
    }
    public string rPassword1
    {
      get { return vm.Account.RegisterPassword1; }
      set
      {
        if (vm.Account.RegisterPassword1 != value) vm.Account.RegisterPassword1 = value;
        if (GetPasswordBox(tb_RegisterPassword1) != value) SetPasswordBox(tb_RegisterPassword1, value);
      }
    }
    public string rPassword2
    {
      get { return vm.Account.RegisterPassword2; }
      set
      {
        if (vm.Account.RegisterPassword2 != value) vm.Account.RegisterPassword2 = value;
        if (GetPasswordBox(tb_RegisterPassword2) != value) SetPasswordBox(tb_RegisterPassword2, value);
      }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      SetPasswordBox(tb_Password, vm.Account.Password);
      if (vm.Account.Email != "")
        Dispatcher.BeginInvoke(
          DispatcherPriority.ContextIdle,
          new Action(delegate ()
          {
            tb_Password.Focus();
          }));
      else if (vm.Account.Password != "")
        Dispatcher.BeginInvoke(
          DispatcherPriority.ContextIdle,
          new Action(delegate ()
          {
            tb_Email.Focus();
          }));

    }

    public delegate void UpdateTextCallbackpassb(PasswordBox tb, string str);
    private void _setpasstbox(PasswordBox tb, string str)
    {
      tb.Password = str;
      Thread.Sleep(100);
    }
    private void SetPasswordBox(PasswordBox tb, string str)
    {
      tb.Dispatcher.Invoke(
              new UpdateTextCallbackpassb(this._setpasstbox),
              new object[] { tb, str }
          );
    }
    private String GetPasswordBox(PasswordBox tb)
    {
      string result = "";
      System.Windows.Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Normal,
        (ThreadStart)delegate { result = tb.Password; });
      return result;
    }

    private void tb_Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
      vPassword = GetPasswordBox(tb_Password);
    }
    private void tb_RegisterPassword1_PasswordChanged(object sender, RoutedEventArgs e)
    {
      rPassword1 = GetPasswordBox(tb_RegisterPassword1);
    }
    private void tb_RegisterPassword2_PasswordChanged(object sender, RoutedEventArgs e)
    {
      rPassword2 = GetPasswordBox(tb_RegisterPassword2);
    }
    private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      vm.Disconnect();
    }
  }

}
