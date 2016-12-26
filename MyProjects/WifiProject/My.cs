﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace My
{
  #region MyFunctioNs
  public struct mfn // MyFunctioNs
  {
    public static string GetSqlResult(string Sql, string ConnectionName = "")
    {
      SqlConnection conn = GetConnection(ConnectionName);
      SqlCommand cmd = new SqlCommand(Sql, conn);
      conn.Open();
      SqlDataReader rdr = cmd.ExecuteReader();
      string ret = null;
      try
      {
        if (rdr.HasRows)
        {
          rdr.Read();
          ret = rdr[0].ToString();
        }
      }
      finally
      {
        rdr.Dispose();
        cmd.Dispose();
        conn.Dispose();
      }
      return ret;
    }
    public static DataSet ExecSP(string SPName, List<SqlParameter> Params = null, string ConnectionName = "")
    {
      SqlConnection conn = GetConnection(ConnectionName);
      DataSet ds = new DataSet();
      SqlDataAdapter rdr = new SqlDataAdapter();
      SqlCommand cmd = new SqlCommand(SPName, conn);
      try
      {
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandTimeout = 0;
        if (Params != null)
          foreach (var P in Params)
          {
            cmd.Parameters.Add(P);
          }
        conn.Open();
        rdr.SelectCommand = cmd;
        rdr.Fill(ds);
        conn.Close();
        try
        {
          return TranslateDS(ds);
        }
        catch (Exception ex)
        {
          return SelectToDS("Select -1 as Result, '" + ex.Message.SqlControls() + "' as Msg, '' as Args", null, ConnectionName);
        }
      }
      finally
      {
        ds.Dispose();
        rdr.Dispose();
        cmd.Dispose();
        conn.Dispose();
      }
    }
    public static DataSet SelectToDS(string Sql, List<SqlParameter> Params = null, string ConnectionName = "")
    {
      SqlConnection conn = GetConnection(ConnectionName);
      DataSet ds = new DataSet();
      SqlDataAdapter rdr = new SqlDataAdapter();
      SqlCommand cmd = new SqlCommand(Sql, conn);
      try
      {
        cmd.CommandTimeout = 0;
        if (Params != null)
          foreach (var P in Params)
          {
            cmd.Parameters.Add(P);
          }
        conn.Open();
        rdr.SelectCommand = cmd;
        rdr.Fill(ds);
        conn.Close();
        try
        {
          return TranslateDS(ds);
        }
        catch (Exception ex)
        {
          return TranslateDS(SelectToDS("Select -1 as Result, '" + ex.Message.SqlControls() + "' as Msg, '' as Args", null, ConnectionName));
          //return SelectToDS("Select -1 as Result, '" + ex.Message.SqlControls() + "' as Msg, '' as Args");
        }
      }
      finally
      {
        ds.Dispose();
        rdr.Dispose();
        cmd.Dispose();
        conn.Dispose();
      }
    }
    public static string TranslateString(string Text, string Args, string ConnectionName = "")
    {
      string ts = Text;
      try
      {
        ts = GetSqlResult("Exec spr_TranslateThis '" + Text.SqlControls() + "', '" + Args.SqlControls() + "', 2", ConnectionName);
      }
      catch (Exception) { }

      return ts;
    }
    public static DataSet TranslateDS(DataSet ds, string ConnectionName = "")
    {
      int i = 0;
      while (i < ds.Tables.Count)
      {
        if ((ds.Tables[i].Columns.Contains("Result")) && ((ds.Tables[i].Columns.Contains("Msg"))) && ((ds.Tables[i].Columns.Contains("Args"))))
        {
          if (Convert.ToInt32(ds.Tables[i].Rows[0]["Result"]) > 0)
          {
            ds.Tables[i].Rows[0]["Msg"] = TranslateString(ds.Tables[i].Rows[0]["Msg"].ToString(), ds.Tables[i].Rows[0]["Args"].ToString(), ConnectionName);
          }
          else if (ds.Tables.Count > i + 1) { ds.Tables.Remove(ds.Tables[i].TableName); i--; }
        }
        i++;
      }
      ds.AcceptChanges();
      return ds;
    }

    public static string subIf(bool Sart, string Truysa, string Falsse = "")
    {
      if (Sart) return Truysa; else return Falsse;
    }

    public static T subIf<T>(bool Sart, T Truysa, T Falsse)
    {
      if (typeof(T) == Type.GetType("string"))
        return subIf(Sart, Truysa, Falsse);
      else
      {
        if (Sart) return Truysa; else return Falsse;
      }
    }

    public static SqlConnection GetConnection(string ConnectionName = "")
    {
      string connstr = null;
      //try {connstr = ConfigurationManager.ConnectionStrings[ConnectionName + "ConnectionString"].ConnectionString;}
      //catch (Exception) {} 
      if (connstr == null) // From Test Unit
        connstr = "Server=.; Database=Wifi; uid=sa; pwd=e;pooling=true; connection lifetime=10; connection timeout=0; packet size=1024;";
      return new SqlConnection(connstr); // ConfigurationManager.ConnectionStrings[ConnectionName + "ConnectionString"].ConnectionString
      //"Server=" + DBServer + "; Database=" + DBDatabase + "; uid=" + DBUsername + "; " + "pwd=" + DBPassword + ";pooling=true; connection lifetime=10; connection timeout=0; packet size=1024;"
    }
  }
  #endregion
  #region StringExtension
  public static class StringExtension
  {
    public static string ReverseString(this string s)
    {
      char[] arr = s.ToCharArray();
      Array.Reverse(arr);
      return new string(arr);
    }

    public static String mySubString(this String Gelen, int baslangic, int toplam)
    {
      if (Gelen.Length + 1 < (baslangic + toplam))
        return Gelen.Substring(baslangic, Gelen.Length - baslangic);
      else
        return Gelen.Substring(baslangic, toplam);
    }
    public static String Copy(this String Gelen, int baslangic, int toplam)
    {
      if (Gelen.Length + 1 < (baslangic + toplam))
        return Gelen.Substring(baslangic - 1, Gelen.Length - (baslangic - 1));
      else
        return Gelen.Substring(baslangic - 1, toplam);
    }
    public static String UnFormatFloatForm(this String Gelen)
    {
      return Gelen.Replace(".", ",");
    }
    public static string SqlControls(this string str)
    {
      return str.Replace("'", "''").Replace("--", "");
    }
    public static string ConvertNonTurkishChars(this string str)
    {
      return str.
        Replace("ü", "u").Replace("Ü", "U").
        Replace("ğ", "G").Replace("Ğ", "G").
        Replace("ı", "i").Replace("İ", "I").
        Replace("ş", "s").Replace("Ş", "S").
        Replace("ç", "c").Replace("Ç", "C").
        Replace("ö", "o").Replace("Ö", "O")
        ;
    }
    public static string subIf(this string Gelen, string Deger, string Truysa, string Falsse)
    {
      if (Gelen.Equals(Deger)) return Truysa; else return Falsse;
    }
    public static string OrtasiniGetir(this string Main, string Bas, string Son, bool Dahil = false)
    {
      string ret = "";
      if (Dahil)
      {
        if (Main.IndexOf(Bas) < 0) ret = "";
        else ret = Main.mySubString(Main.IndexOf(Bas), Main.Length);
        if (ret.IndexOf(Son) < 0) ret = "";
        else ret = ret.mySubString(0, ret.IndexOf(Son) + Son.Length);
      }
      else
      {
        if (Main.IndexOf(Bas) < 0) ret = "";
        else ret = Main.mySubString(Main.IndexOf(Bas) + Bas.Length, Main.Length);
        if (ret.IndexOf(Son) < 0) ret = "";
        else ret = ret.mySubString(0, ret.IndexOf(Son));
      }
      return ret;
    }
    public static string HashMD5(this string Main)
    {
      MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
      byte[] btr = Encoding.Unicode.GetBytes(Main);
      btr = md5.ComputeHash(btr);
      StringBuilder sb = new StringBuilder();
      foreach (byte ba in btr)
      {
        sb.Append(ba.ToString("x2").ToUpper());
      }
      md5.Dispose();
      return sb.ToString();
    }
  }
  #endregion
  #region StreamExtension
  public static class StreamExtension
  {
    const int BufferSize = 8192;

    public static void CopyTo(this Stream input, Stream output)
    {
      byte[] buffer = new byte[BufferSize];
      int read;
      while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
      {
        output.Write(buffer, 0, read);
      }
    }

    public static string Tostring(this Stream input)
    {
      long i = 0;
      try { i = input.Position; input.Position = 0; }
      catch (Exception) { }
      string result = "";
      using (StreamReader reader = new StreamReader(input, Encoding.UTF8)) { result = reader.ReadToEnd(); }
      try { input.Position = i; }
      catch (Exception) { }
      return result;
    }
    public static byte[] ToByteArr(this Stream input)
    {
      using (MemoryStream tempStream = new MemoryStream())
      {
        CopyTo(input, tempStream);
        if (tempStream.Length == tempStream.GetBuffer().Length)
        {
          return tempStream.GetBuffer();
        }
        return tempStream.ToArray();
      }
    }
  }
  #endregion
  #region ConvertExtension
  public static class DataTableExtension
  {
    /// <summary>
    /// Converts a DataTable to a list with generic objects
    /// </summary>
    /// <typeparam name="T">Generic object</typeparam>
    /// <param name="table">DataTable</param>
    /// <returns>List with generic objects</returns>
    public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
    {
      try
      {
        List<T> list = new List<T>();

        foreach (var row in table.AsEnumerable())
        {
          T obj = new T();
          string typestr = obj.GetType().ToString();// typeOf(T).GetType().ToString();
          typestr = typestr.Split('.')[typestr.Split('.').Length - 1].Replace("ViewModel", "").Replace("Model", "");


          foreach (var prop in obj.GetType().GetProperties())
          {
            try
            {
              PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
              if (prop.Name.Contains("__"))
                propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name.Replace("__", ".")], propertyInfo.PropertyType), null);
              else
              {
                propertyInfo.SetValue(obj, (row[typestr + "." + prop.Name] == null) ? null : Convert.ChangeType(row[typestr + "." + prop.Name], (Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType)), null);
              }
            }
            catch
            {
              continue;
            }
          }

          list.Add(obj);
        }

        return list;
      }
      catch
      {
        return null;
      }
    }
  }
  #endregion
  #region BoolExtension
  public static class BoolExtension
  {
    public static string subIf(this bool Gelen, string Truysa, string Falssa)
    {
      if (Gelen) return Truysa; else return Falssa;
    }
  }
  #endregion
  #region DateTimeExtension
  public static class DateTimeExtension {
    public static DateTime SetTime(this DateTime dt, DateTime? ts)
    {
      if (ts == null)
        dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
      else
        dt = dt.SetTime(ts.Value);
      return dt;
    }
    public static DateTime SetTime(this DateTime dt, TimeSpan ts)
    {
      if (ts == null)
        dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
      else
        dt = new DateTime(dt.Year, dt.Month, dt.Day, ts.Hours, ts.Minutes, ts.Seconds);
      return dt;
    }
    public static DateTime SetDate(this DateTime dt, DateTime? ts)
    {
      if (ts == null)
        dt = new DateTime(1900, 1, 1, dt.Hour, dt.Minute, dt.Second);
      else
        dt = new DateTime(ts.Value.Year, ts.Value.Month, ts.Value.Day, dt.Hour, dt.Minute, dt.Second);
      return dt;
    }
  }
  #endregion
  /*
  #region SoapExtension
  public class SoapMessageLogger : SoapExtension
  {
    Stream oldStream;
    Stream newStream;
    string SessionId = "";
    private struct MySoapPacket
    {
      public string sid;
      public string env;
      public int msgtype;
    }
    public override Stream ChainStream(Stream stream)
    {
      oldStream = stream;
      newStream = new MemoryStream();
      return newStream;
    }
    public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute)
    {
      return null;
    }
    public override object GetInitializer(Type WebServiceType)
    {
      return null;
    }
    public override void Initialize(object initializer)
    {
    }
    public override void ProcessMessage(SoapMessage message)
    {
      LogaYaz(message);
    }
    void Copy(Stream from, Stream to)
    {
      TextReader reader = new StreamReader(from);
      TextWriter writer = new StreamWriter(to);
      writer.WriteLine(reader.ReadToEnd());
      writer.Flush();
    }
    private void LogaYaz(SoapMessage message)
    {
      int MsgType = 0;
      string Envelope = "";
      switch (message.Stage)
      {
        case SoapMessageStage.BeforeDeserialize: // Request
          MsgType = 1;
          break;
        case SoapMessageStage.AfterSerialize: // Response
          MsgType = 2;
          break;
        case SoapMessageStage.AfterDeserialize: break;
        case SoapMessageStage.BeforeSerialize: break;
        default: break;
      }
      if ((MsgType == 1) || (MsgType == 2))
      {
        if (MsgType == 1)
        {
          Copy(oldStream, newStream);
          newStream.Position = 0;
          using (MemoryStream fs = new MemoryStream())
          {
            using (StreamWriter w = new StreamWriter(fs))
            {
              Copy(newStream, fs);
              fs.Position = 0;
              Envelope = fs.Tostring();
              w.Close();
            }
          }
          newStream.Position = 0;
        }
        else if (MsgType == 2)
        {
          newStream.Position = 0;
          using (MemoryStream fs = new MemoryStream())
          {
            using (StreamWriter w = new StreamWriter(fs))
            {
              Copy(newStream, fs);
              fs.Position = 0;
              Envelope = fs.Tostring(); // newStream
              w.Close();
            }
          }
          newStream.Position = 0;
          Copy(newStream, oldStream);
        }
        if (SessionId == "") SessionId = Guid.NewGuid().ToString("D");
        MySoapPacket m;// = new msj();
        m.sid = SessionId;
        m.env = Envelope;
        m.msgtype = MsgType;
//        ThreadPool.QueueUserWorkItem(new WaitCallback(DBYaz), m); // Threading için her şey hazır. Bu satırı açıp bir alt satırı kapatınca DBYaz thread olarak çalışıyor. Yaklaşık 4 kat hızlı ancak 10.000 kayıttan 34'ünde response null olarak kaldı. Sebebini bilmiyorum. Düşün bir oran. Hata vs yok. Sistem biraz yoğunlaşınca thread e geçebiliriz. - Erci-25.09.2014-Koh Samui
        DBYaz(m);
      }
    }
    static void DBYaz(Object o) {
      string SessionId = ((MySoapPacket)o).sid;
      string Envelope = ((MySoapPacket)o).env;
      int MsgType = ((MySoapPacket)o).msgtype;
      if ((!Envelope.Contains("<GetSecurityControl ")) && (SessionId != "-1"))
      {
        List<SqlParameter> Params = new List<SqlParameter>();
        Params.Add(new SqlParameter("@Id", SessionId));
        Params.Add(new SqlParameter("@MsgTime", DateTime.Now));
        if (Envelope.Contains("<SessionId>"))
          Params.Add(new SqlParameter("@SessionId", Envelope.OrtasiniGetir("<SessionId>", "</SessionId>")));
        if (Envelope.Contains("<SOAP-ENV:Body>"))
          Envelope = "<Body>" + Envelope.OrtasiniGetir("<SOAP-ENV:Body>", "</SOAP-ENV:Body>", false) + "</Body>";
        else if (Envelope.Contains("<soap:Body>"))
          Envelope = "<Body>" + Envelope.OrtasiniGetir("<soap:Body>", "</soap:Body>", false) + "</Body>";
        if (MsgType == 1)
        {
          Params.Add(new SqlParameter("@MethodName", Envelope.OrtasiniGetir("Body><", " ")));
          Params.Add(new SqlParameter("@Request", Envelope));
        }
        else Params.Add(new SqlParameter("@Response", Envelope));
        if (Envelope.Contains("<soap:Fault>"))
          Params.Add(new SqlParameter("@hasError", true));
        else Params.Add(new SqlParameter("@hasError", false));

        DataSet ds = mfn.ExecSP("spr_AU_SoapMessageLog", Params);
        //Id = (ds.Tables[0].Columns.Contains("Id")) ? Convert.ToInt32(ds.Tables[0].Rows[0]["Id"].ToString()) : 0;
      }
      else { SessionId = "-1"; }
    }
  }
  #endregion
*/
}
