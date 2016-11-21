#region Copyright Information
/*
 * (C)  2005-2007, Marcello Cauchi Savona
 *
 * For terms and usage, please see the LICENSE file
 * provided alongwith or contact marcello_c@hotmail.com
 * http://www.cheekyneedle.com
 * 
 * 
 */
#endregion


/*
 * clsUDP
 * shall start a listner, and raise an event every time data arrives on a port
 * shall also be able to send data via udp protocol
 * .Dispose shall remove all resources associated with the class
 */


using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using Microsoft.Win32;


namespace SmallDHCPServer_C
{
   class clsUDP

    {
        #region "Class Variables"
            private Int32 PortToListenTo, PortToSendTo = 0;
            private string rcvCardIP;
            public bool IsListening;
            // call backs for send/recieve!
            public UdpState s;
        #endregion

        #region "Class Events"
            public delegate void DataRcvdEventHandler(byte[] DData, IPEndPoint RIpEndPoint);
            public event DataRcvdEventHandler DataRcvd;
            public delegate void ErrEventHandler(string Msg);
        #endregion

            //class constructors
        public clsUDP()
        {
            IsListening = false;
        }
        //overrides pass the port to listen to/sendto and startup
        public clsUDP(Int32 PortToListenTo,Int32 PortToSendTo, string rcvCardIP) 
        {
            try
            {
                IsListening = false;
                this.PortToListenTo = PortToListenTo;
                this.PortToSendTo = PortToSendTo;
                this.rcvCardIP = rcvCardIP;
                StartListener();
            }
            catch(Exception ex) 
            {
               Console.WriteLine (ex.Message);
               
            }
        }

        //string property to contain the class name
        private string ClassName
        {
            get {return "clsUDP";
            }
        }

        //function to send data as a byte stream to a remote socket
       // modified to work as a callback rather than a block
        public void SendData(byte[] Data)
        {

            try
            {
                s.u.BeginSend(Data, Data.Length, "255.255.255.255", PortToSendTo, new AsyncCallback(OnDataSent), s);
                //s.u.Send(Data, Data.Length, "255.255.255.255", PortToSendTo);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // This is the call back function, which will be invoked when a client is connected
        public void OnDataSent(IAsyncResult asyn)
        {

            try
            {
                //get the data
                UdpClient ii = (UdpClient)asyn;
                ii.EndSend(asyn); // stop the send call back
            }
            catch (Exception ex)
            {
                if (IsListening == true)
                    Console.WriteLine(ex.Message);
            }
        }



       //function to start the listener call back everytime something is recieved
        private void IniListnerCallBack()
        {
            try
            {
               // start teh recieve call back method
                s.u.BeginReceive(new AsyncCallback(OnDataRecieved), s);
            }
            catch (Exception ex)
            {
                if (IsListening == true)
                    Console.WriteLine(ex.Message);
            }
        }


        // This is the call back function, which will be invoked when a client is connected
       public void OnDataRecieved(IAsyncResult asyn)
       {
           Byte[] receiveBytes;
           UdpClient u;
           IPEndPoint e;

           try
           {

               u = (UdpClient)((UdpState)(asyn.AsyncState)).u;
               e = (IPEndPoint)((UdpState)(asyn.AsyncState)).e;

               receiveBytes = u.EndReceive(asyn, ref e);
               //raise the event with the data recieved
               DataRcvd(receiveBytes, e);
            }
           catch (Exception ex)
           {
               if (IsListening == true)
                   Console.WriteLine(ex.Message);
           }
           finally
           {
               u = null;
               e = null;
               receiveBytes = null;
               // recall the call back
               IniListnerCallBack();
           }
          
       }
           
       



        //function to start the listener 
        //if the the listner is active, destroy it and restart
        // shall mark the flag that the listner is active
        private void StartListener()
        {
           // byte[] receiveBytes; // array of bytes where we shall store the data recieved
            IPAddress ipAddress;
            IPEndPoint ipLocalEndPoint;


            try
            {

                IsListening = false;
                //resolve the net card ip address
                ipAddress = IPAddress.Parse(rcvCardIP);
                //get the ipEndPoint
                ipLocalEndPoint = new IPEndPoint(ipAddress, PortToListenTo);
                // if the udpclient interface is active destroy
                if (s.u != null) s.u.Close();
                s.u = null; s.e = null;
                //re initialise the udp client
                             
                s = new UdpState();
                s.e = ipLocalEndPoint;
                s.u = new UdpClient(ipLocalEndPoint);
               
                IsListening = true; // set to start listening
                // wait for data
                IniListnerCallBack();        
            }
            catch (Exception ex)
            {
                if (IsListening == true)
                    Console.WriteLine(ex.Message);
            }
            finally
            {
                 if (s.u == null) {
                    Thread.Sleep(1000);
                    StartListener(); }
                 else {
                    ipAddress = null;
                    ipLocalEndPoint = null; }
            }
        }



        //stop the listener thread
        public void StopListener()
        {
            try
            {
                IsListening = false;
                if (s.u != null) s.u.Close();
                s.u = null; s.e = null;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //dispose of all resources
        ~clsUDP()
        {
            try
            {
                StopListener();
                if (s.u != null) s.u.Close();
                s.u = null; s.e = null;
                rcvCardIP = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

       //class that shall hold the reference of the call backs
       public struct UdpState
       {
           public IPEndPoint e; //define an end point
           public UdpClient u; //define a client
      }


    }
}
