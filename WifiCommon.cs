﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using My;
using System.Net;
using System.ComponentModel;
using System.Threading;
using System.Globalization;

namespace WifiSolution
{
  public class WifiCommon
  {
    public MyDictionary dict;
    public WinFuncs wf;
    public string ProjectName;
    public string AType = "";
    private readonly string WifiServicePrefix = "http://70.35.206.36:1004/Request.aspx?Command=";

    public WifiCommon(String atype, String projectName, String resource_data)// MyDictionary dictionary, String projectName)
    {
      AType = atype;
      ProjectName = projectName;
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, resource_data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
      wf = new WinFuncs(projectName);
    }

    public String GetWebstring(String Url, String ProviderIp = "")
    {
      string AFQ = null;
      WebClient wc = new WebClient();
      try
      {
        String command = Url.GetUrlParam(".");
        if (!Url.ToLower().StartsWith("http:"))
        {
          if (CheckForInternetConnection())
            Url = WifiServicePrefix + command + "&" + Url.Substring(command.Length + 2);
          else if (ProviderIp != "")
            Url = "http://" + ProviderIp + "/Request.aspx?Command=" + command + "&" + Url.Substring(command.Length + 2);
          else
            AFQ = "¶E:" + dict.GetMessage(6);
        }
        while (CheckResult(ref AFQ))
        {
          AFQ = wc.DownloadString(Url);
        }
      }
      catch (Exception e)
      {
        AFQ = "¶E:" + e.Message;
        //AFQ = null;
      }
      wc.Dispose();
      AFQ = AFQ ?? "¶E:ERROR";
      return AFQ;
    }

    public bool CheckResult(ref string result)
    {
      // rtn true ise checkresult sonrasında komut (Ör: WCF.Register) çalıştırılır, false ise çalıştırılmaz
      // result null döndürülürse komut çalıştırlmadığı gibi dış method da sonlandırılır (Ör: bt_registerclick)
      bool rtn = false;
      if (result == null)
      {
        result = "";
        rtn = true;
      }
      else if (result.Contains("¶")) //¶ : ASCII 20
      {
        string[] sl = result.Split('¶');
        result = result.Substring(0, result.IndexOf("¶"));
        for (int i = 1; i < sl.Length; i++)
        {
          if (sl[i].StartsWith("M:")) // Message
          {
            Console.WriteLine(sl[i]);
            wf.ShowMessageBox(sl[i].Substring(sl[i].IndexOf(":") + 1), "", 0, 64);//MessageBoxButton.OK: 0 ; MessageBoxImage.Asterisk : 64
          }
          else if (sl[i].StartsWith("Q:")) // Question
          {
            Console.WriteLine(sl[i]); // //§ : ASCII 21
            bool cevaplimi = sl[i].Substring(sl[i].IndexOf("§") + 1).StartsWith("-");
            int mbb = sl[i].Substring(sl[i].IndexOf("§") + 1) == "OK" ? 0 : sl[i].Substring(sl[i].IndexOf("§") + 1) == "OKCancel" ? 1 : sl[i].Substring(sl[i].IndexOf("§") + 1) == "YesNo" ? 4 : sl[i].Substring(sl[i].IndexOf("§") + 1) == "YesNoCancel" ? 3 : 0;
            int mbr = 0;
            while (mbr == 0)
              mbr = cevaplimi ?
                 (
                    sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == "2" ? 2
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == "7" ? 7
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == "1" ? 1
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == "6" ? 6
                  : 0
                )
                : wf.ShowMessageBox(sl[i].OrtasiniGetir(":", "§"), "", mbb, 32); // MessageBoxImage.Question : 32
            result = result + "¶Q:" + sl[i].OrtasiniGetir(":", "§") + "§-" + (mbr == 1 ? "OK" : mbr == 2 ? "Cancel" : mbr == 6 ? "Yes" : mbr == 7 ? "No" : "None").ToString(); // (ör: soru1§cevap1¶soru2§cevap2)
            rtn = rtn || !cevaplimi;
          }
          else if (sl[i].StartsWith("E:")) // Error
          {
            Console.WriteLine(sl[i]);
            wf.ShowMessageBox(sl[i].Substring(sl[i].IndexOf(":") + 1), "", 0, 16); // MessageBoxImage.Error : 16
            result = null;
          }
        }
      }
      //else rtn = false;
      return rtn;
    }
    public static bool CheckForInternetConnection()
    {
      try
      {
        using (var client = new WebClient())
        {
          using (var stream = client.OpenRead("http://www.google.com"))
          {
            return true;
          }
        }
      }
      catch
      {
        return false;
      }
    }
    public void RuninThread(DoWorkEventHandler work, RunWorkerCompletedEventHandler afterThat)
    {
      AutoResetEvent _resetEvent = new AutoResetEvent(false);
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += work;
      if (afterThat != null)
        bw.RunWorkerCompleted += afterThat;// delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; };
      //bw.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) { Account.Waitcount--; };
      bw.RunWorkerAsync();
    }

  }
}
