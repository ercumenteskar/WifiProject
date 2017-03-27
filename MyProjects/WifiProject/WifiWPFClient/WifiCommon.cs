using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using My;
using System.Net;

namespace WifiWPFClient
{
  public class WifiCommon
  {
    private MyDictionary dict;
    private WinFuncs wf;
    private WifiService.Service1Client _WCF;
    private string ProjectName;

    public WifiService.Service1Client WCF { get { _WCF = _WCF ?? new WifiService.Service1Client(); return _WCF; } }
    public WifiCommon(MyDictionary dictionary, String projectName)
    {
      ProjectName = projectName;
      dict = dictionary;
      wf = new WinFuncs(ProjectName);
    }

    public String WSRunner(String url)
    {
      string AFQ = null;
      while (CheckResult(ref AFQ))
      {
        if (CheckForInternetConnection())
        {
          switch (url.GetUrlParam("."))
          {
            case "Register": AFQ = WCF.Register(url.GetUrlParam("Email"), url.GetUrlParam("Pass"), AFQ, dict.LangCode); break;
            case "Remove": WCF.Remove(url.GetUrlParam("Email")); AFQ = ""; break;
            case "GetSecurityCode": AFQ = WCF.GetSecurityCode(url.GetUrlParam("EmailHash"), AFQ, dict.LangCode); break;
            case "Login": AFQ = WCF.Login(url.GetUrlParam("Evidence"), AFQ, dict.LangCode); break;
            case "SendResetPasswordCode": AFQ = WCF.SendResetPasswordCode(url.GetUrlParam("EmailHash"), AFQ, dict.LangCode); break;
          }
        }
        else // Please check your internet connection.
          AFQ = "¶E:" + dict.GetMessage(6);
      }
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

  }
}
