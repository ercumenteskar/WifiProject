﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WifiSolution.WifiProvider
{
  public class MyWebServer
  {
    public HttpListener _listener = new HttpListener();
    private readonly Func<HttpListenerRequest, string> _responderMethod;

    public MyWebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
    {
      if (!HttpListener.IsSupported)
        throw new NotSupportedException(
            "Needs Windows XP SP2, Server 2003 or later.");

      // URI prefixes are required, for example 
      // "http://localhost:1234/".
      if (prefixes == null || prefixes.Length == 0)
        throw new ArgumentException("prefixes");

      // A responder method is required
      if (method == null)
        throw new ArgumentException("method");

      foreach (string s in prefixes)
        _listener.Prefixes.Add(s);

      _responderMethod = method;
      _listener.Start();
    }

    public MyWebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
      : this(prefixes, method) { }

    public void Run()
    {
      ThreadPool.QueueUserWorkItem((o) =>
      {
        try
        {
          while (_listener.IsListening)
          {
            ThreadPool.QueueUserWorkItem((c) =>
            {
              var ctx = c as HttpListenerContext;
              try
              {
                string rstr = _responderMethod(ctx.Request);
                if (rstr == "ŞİMDİLİK KAPALI")
                {
                  rstr = ctx.Request.Url.ToString();
                  ctx.Response.Redirect(rstr);
                }
                else
                {
                  byte[] buf = Encoding.UTF8.GetBytes(rstr);
                  ctx.Response.ContentLength64 = buf.Length;
                  ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                }
                
              }
              //catch { } // suppress any exceptions SendResponse içindeki tüm hataları yutuyordu, debug etmeden hatanın ne olduğunu anlamıyordum...
              finally
              {
                // always close the stream
                ctx.Response.OutputStream.Close();
              }
            }, _listener.GetContext());
          }
        }
        catch { } // suppress any exceptions
      });
    }

    public void Stop()
    {
      _listener.Stop();
      _listener.Close();
    }
  }
}
