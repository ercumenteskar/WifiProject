/*
 * clsUDP
 * shall start a listner, and raise an event every time data arrives on a port
 * shall also be able to send data via udp protocol
 * .Dispose shall remove all resources associated with the class
 */

//Modified 01/08/07 to start working with call back events rather than blocking

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace SmallDHCPServer_C
{
    class clsUDP: clsStream     //clsUdP inherits stream class
    {
        #region "Class Variables"
            private Int32 PortToListenTo, PortToSendTo = 0;
            private string rcvCardIP;
            private Thread ListenThrd;
            private UdpClient UListener;
            private IPEndPoint RemoteIpEndPoint;
            public bool IsListening;
        #endregion

        #region "Class Events"
            public delegate void DataRcvdEventHandler(byte[] DData, IPEndPoint RIpEndPoint);
            public event DataRcvdEventHandler DataRcvd;
            public delegate void ErrEventHandler(string Msg);
            public event ErrEventHandler ErrOccurred;
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
                //if the listener is active destroy and re-create
                //StartListener();
                ListenThrd = new Thread(StartListener);
                ListenThrd.Priority = ThreadPriority.AboveNormal;
                ListenThrd.Start();
            }
            catch(Exception ex) {
                           AppendError(ex.Message, ClassName, "clsUDP(Int32 PortToListenTo,Int32 PortToSendTo)");
            }
        }

        //string property to contain the class name
        private string ClassName
        {
            get {return "clsUDP";
            }
        }

        //function to send data as a byte stream to a remote socket
        public void SendData(byte[] Data)
        {
            try {
                UListener.Send(Data, Data.Length, "255.255.255.255", PortToSendTo);
            }
            catch (Exception ex) {
                AppendError(ex.Message, ClassName, "SendData(byte[] Data)");}
        }
           

        
   

        //function to start the listener 
        //if the the listner is active, destroy it and restart
        // shall mark the flag that the listner is active
        private void StartListener()
        {
            byte[] receiveBytes; // array of bytes where we shall store the data recieved
            IPAddress ipAddress;
            IPEndPoint ipLocalEndPoint;


            try
            {
                //resolve the net card ip address
                ipAddress = IPAddress.Parse(rcvCardIP);
                //get the ipEndPoint
                ipLocalEndPoint =  new IPEndPoint(ipAddress, PortToListenTo);
                // if the udpclient interface is active destroy
                if (UListener != null) UListener.Close();
                UListener = null;
                //re initialise the udp client
                UListener = new UdpClient(ipLocalEndPoint);
                IsListening = true;
                do
                {
                    // Blocks until a message returns on this socket from a remote host.
                    receiveBytes = UListener.Receive(ref RemoteIpEndPoint);
                    DataRcvd(receiveBytes, RemoteIpEndPoint);
                } while (IsListening == true);
            }
            catch (Exception ex)
            {
                AppendError(ex.Message, ClassName, "StartListner");
               // ErrOccurred("An Error Occurred While the UDP Listner was Listnening!");
            }
            finally
            {
                receiveBytes = null;
                if (UListener != null) UListener.Close();
                UListener = null;
                ipAddress = null;
                ipLocalEndPoint = null;
            }
        }



        //stop the listener thread
        public void StopListener()
        {
            try
            {
                IsListening = true;
                if (UListener != null) UListener.Close();
                UListener = null;
                if (ListenThrd != null)
                {
                    if (ListenThrd.IsAlive == true) ListenThrd.Abort();
                }
                ListenThrd = null;
            }
            catch (Exception ex)
            {
                AppendError(ex.Message, ClassName, "StopListener");
            }
        }

        //dispose of all resources
        ~clsUDP()
        {
            try
            {
                StopListener();
                ListenThrd = null;
                if (UListener != null) UListener.Close();
                UListener = null;
                RemoteIpEndPoint = null;
                rcvCardIP = null;
            }
            catch (Exception ex)
            {
                AppendError(ex.Message, ClassName, "Dispose");
            }
        }


    }
}
