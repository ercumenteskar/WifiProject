#region Copyright Information
/*
 * (C)  2005-2007, Marcello Cauchi Savona
 *
 * For terms and usage, please see the LICENSE file
 * provided alongwith or contact marcello_c@hotmail.com
 * http://www.cheekyneedle.com
 * 
 */
#endregion



/* DHCP CLASS
 * A MAC ADDRESS REQUESTS AN IP ADDRESS
 * CHECK THE MAC ADDRESS AND SEE IF THE MASKS AND TOGETHER
 * MAC ALLOWED ASSIGN AN IP ADDRESS
 * PING TO SEE IF THE BASE IP ADDRESS IS IN USE
 * IF IT IS IN USE INCREMENT THE IP ADDRESS AND, PING AND IF ALLOWED TO ASSIGN
 */ 

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace SmallDHCPServer_C
{
     
    #region "Enums" 
    //msgtype indentifier
    public enum DHCPMsgType
    {
        DHCPDISCOVER = 1,
        DHCPOFFER = 2,
        DHCPREQUEST = 3,
        DHCPDECLINE = 4,
        DHCPACK = 5,
        DHCPNAK = 6,
        DHCPRELEASE = 7,
        DHCPINFORM = 8
    }
    public enum DHCPOptionEnum  //refer to the rfc2132.txt for vendor specific info
{
        SubnetMask = 1,
        TimeOffset = 2,
        Router = 3,
        TimeServer = 4,
        NameServer = 5,
        DomainNameServer = 6,
        LogServer = 7,
        CookieServer = 8,
        LPRServer = 9,
        ImpressServer = 10,
        ResourceLocServer = 11,
        HostName = 12,
        BootFileSize = 13,
        MeritDump = 14,
        DomainName = 15,
        SwapServer = 16,
        RootPath = 17,
        ExtensionsPath = 18,
        IpForwarding = 19,
        NonLocalSourceRouting = 20,
        PolicyFilter = 21,
        MaximumDatagramReAssemblySize = 22,
        DefaultIPTimeToLive = 23,
        PathMTUAgingTimeout = 24,
        PathMTUPlateauTable = 25,
        InterfaceMTU = 26,
        AllSubnetsAreLocal = 27,
        BroadcastAddress = 28,
        PerformMaskDiscovery = 29,
        MaskSupplier = 30,
        PerformRouterDiscovery = 31,
        RouterSolicitationAddress = 32,
        StaticRoute = 33,
        TrailerEncapsulation = 34,
        ARPCacheTimeout = 35,
        EthernetEncapsulation = 36,
        TCPDefaultTTL = 37,
        TCPKeepaliveInterval = 38,
        TCPKeepaliveGarbage = 39,
        NetworkInformationServiceDomain = 40,
        NetworkInformationServers = 41,
        NetworkTimeProtocolServers = 42,
        VendorSpecificInformation = 43,
        NetBIOSoverTCPIPNameServer = 44,
        NetBIOSoverTCPIPDatagramDistributionServer = 45,
        NetBIOSoverTCPIPNodeType = 46,
        NetBIOSoverTCPIPScope = 47,
        XWindowSystemFontServer = 48,
        XWindowSystemDisplayManager = 49,
        RequestedIPAddress = 50,
        IPAddressLeaseTime = 51,
        OptionOverload = 52,
        DHCPMessageTYPE = 53,
        ServerIdentifier = 54,
        ParameterRequestList = 55,
        Message = 56,
        MaximumDHCPMessageSize = 57,
        RenewalTimeValue_T1 = 58,
        RebindingTimeValue_T2 = 59,
        Vendorclassidentifier = 60,
        ClientIdentifier = 61,
        NetworkInformationServicePlusDomain = 64,
        NetworkInformationServicePlusServers = 65,
        TFTPServerName = 66,
        BootfileName = 67,
        MobileIPHomeAgent = 68,
        SMTPServer = 69,
        POP3Server = 70,
        NNTPServer = 71,
        DefaultWWWServer = 72,
        DefaultFingerServer = 73,
        DefaultIRCServer = 74,
        StreetTalkServer = 75,
        STDAServer = 76,
        END_Option = 255
}
        
   #endregion

#region "Class Structures"


    
    public class cDHCPStruct 
    {
        public DHCPstruct dStruct;
        public DHCPData dData;
        public const int OPTION_OFFSET = 240;
       
        public cDHCPStruct(byte[] Data)
        {
           
            
            System.IO.BinaryReader rdr;
            System.IO.MemoryStream stm = new System.IO.MemoryStream(Data, 0, Data.Length);

           try
           {
               //ini the binary reader
               
               rdr = new System.IO.BinaryReader(stm);
               //read data
               dStruct.D_op = rdr.ReadByte();
               dStruct.D_htype = rdr.ReadByte();
               dStruct.D_hlen = rdr.ReadByte();
               dStruct.D_hops = rdr.ReadByte();
               dStruct.D_xid = rdr.ReadBytes(4);
               dStruct.D_secs = rdr.ReadBytes(2);
               dStruct.D_flags = rdr.ReadBytes(2);
               dStruct.D_ciaddr = rdr.ReadBytes(4);
               dStruct.D_yiaddr = rdr.ReadBytes(4);
               dStruct.D_siaddr = rdr.ReadBytes(4);
               dStruct.D_giaddr = rdr.ReadBytes(4);
               dStruct.D_chaddr = rdr.ReadBytes(16);
               dStruct.D_sname = rdr.ReadBytes(64);
               dStruct.D_file = rdr.ReadBytes(128);
               dStruct.M_Cookie = rdr.ReadBytes(4);
               dStruct.D_options = rdr.ReadBytes(Data.Length - OPTION_OFFSET);
           }
           catch
           {
              // AppendError(ex.Message, ClassName, "cDHCPStruct(byte[] Data)");
           }
           finally
           {
               if (stm != null) stm.Dispose();
               stm = null;
               rdr = null;
           }
        }
        


        public struct DHCPData
        {
            public string IPAddr;
            public string SubMask;
            public uint  LeaseTime;
            public string ServerName;
            public string MyIP;
            public string RouterIP;
            public string DomainIP;
            public string LogServerIP;
        }
        public struct DHCPstruct
        {
            public byte D_op;   //Op code:   1 = bootRequest, 2 = BootReply
            public byte D_htype;      //Hardware Address Type: 1 = 10MB ethernet
            public byte D_hlen;       //hardware address length: length of MACID
            public byte D_hops;      //Hw options
            public byte[] D_xid;      //transaction id (5)
            public byte[] D_secs;    //elapsed time from trying to boot (3)
            public byte[] D_flags;    //flags (3)
            public byte[] D_ciaddr;  // client IP (5)
            public byte[] D_yiaddr;   // your client IP (5)
            public byte[] D_siaddr;   // Server IP  (5)
            public byte[] D_giaddr;   // relay agent IP (5)
            public byte[] D_chaddr;   // Client HW address (16)
            public byte[] D_sname;    // Optional server host name (64)
            public byte[] D_file;     // Boot file name (128)
            public byte[] M_Cookie;     // Magic cookie (4)
            public byte[] D_options;  //options (rest)
            

        }
    }

    
    
    public struct tcDHCPStruct
    {
        public byte D_op;       //Op code:   1 = bootRequest, 2 = BootReply
        public byte  D_htype;      //Hardware Address Type: 1 = 10MB ethernet
        public byte  D_hlen;       //hardware address length: length of MACID
        public byte  D_hops;      //Hw options
        public  byte[] D_xid;      //transaction id
        public  byte[] D_secs;    //elapsed time from trying to boot
        public  byte[] D_flags;    //flags
        public  byte[]   D_ciaddr;  // client IP
        public  byte[]   D_yiaddr;   // your client IP
        public  byte[]   D_siaddr;   // Server IP
        public  byte[]   D_giaddr;   // relay agent IP
        public  byte[]   D_chaddr;   // Client HW address
        public  byte[]   D_sname;    // Optional server host name
        public  byte[]   D_file;     // Boot file name
        public byte[]  D_options;  //options
    }
#endregion


    class clsDHCP 
    {

        #region "Events to Raise" 
        //an event has to call a delegate (function pointer)
            #region "event Delegates" 
                    public delegate void AnnouncedEventHandler(cDHCPStruct d_DHCP,string MacId);
                    public delegate void ReleasedEventHandler();//(cDHCPStruct d_DHCP);
                    public delegate void RequestEventHandler(cDHCPStruct d_DHCP, string MacId);
                    public delegate void AssignedEventHandler(string IPAdd,string MacID );
             #endregion
            public event AnnouncedEventHandler Announced;
            public event RequestEventHandler Request;
        #endregion
        #region "Variables to Call"
            private clsUDP cUdp; // the udp snd/rcv class
            private string NetCard;
        #endregion


        //string property to contain the class name
        private string ClassName
        {
            get{return "clsDHCP"; }
        }

        public void Dispose()
        {
            if (cUdp != null )     cUdp.StopListener();
            cUdp = null;

        }
          ~clsDHCP()
        {
             
            cUdp = null;
        }

        public clsDHCP(string NetCard1)
        {
            NetCard = NetCard1;
        }

        //function to start the DHCP server
        //port 67 to recieve, 68 to send
        public void StartDHCPServer()
        {
            try {   // start the DHCP server
                //assign the event handlers
                cUdp = new clsUDP(67, 68, NetCard);
                cUdp.DataRcvd += new clsUDP.DataRcvdEventHandler(cUdp_DataRcvd);
       
            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        //pass the option type that you require
        //parse the option data
        //return the data in a byte of what we need
        private byte[] GetOptionData(DHCPOptionEnum DHCPTyp, cDHCPStruct cdDHCPs)
        {
            int DHCPId = 0;
            byte DDataID, DataLength = 0;
            byte[] dumpData;

            try
            {
                DHCPId = (int)DHCPTyp;
                //loop through look for the bit that states that the identifier is there
                for (int i = 0; i < cdDHCPs.dStruct.D_options.Length; i++)
                {
                    //at the start we have the code + length
                    //i has the code, i+1 = length of data, i+1+n = data skip
                    DDataID = cdDHCPs.dStruct.D_options[i];
                    if (DDataID == DHCPId)
                    {
                        DataLength = cdDHCPs.dStruct.D_options[i + 1];
                        dumpData = new byte[DataLength];
                        Array.Copy(cdDHCPs.dStruct.D_options, i+2, dumpData, 0, DataLength);
                        return dumpData;
                    }
                    else
                    {
                        DataLength = cdDHCPs.dStruct.D_options[i + 1]; //'length of code
                        i += 1 + DataLength;
                    } //.endif
                } //for
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                dumpData = null;
            }
            return null;
        }

        public string ByteToString(byte[] dByte, byte hLength)
        {
            string dString;

            try
            {
                dString = string.Empty;
                if (dByte != null)
                {
                    for (int i = 0; i < hLength; i++)
                    {
                        dString += dByte[i].ToString("X2");
                    }
                }
                return dString;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
            finally
            {
                dString = null;
            }
           
        }

        //get the Message type
        //located in the options stream
        public DHCPMsgType GetMsgType(cDHCPStruct cdDHCPs)
        {
            byte[] DData;

            try
            {
                DData = GetOptionData(DHCPOptionEnum.DHCPMessageTYPE, cdDHCPs);
                if (DData != null)
                {
                    return (DHCPMsgType)DData[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }


        //private void 
        public void cUdp_DataRcvd(byte[] DData, IPEndPoint RIpEndPoint)
        {
            cDHCPStruct ddHcpS;
            DHCPMsgType MsgTyp;
            //clsDHCP ccDHCP;
            string MacID;

           try
            {
               ddHcpS = new cDHCPStruct(DData);
               //ccDHCP = new clsDHCP();
                

               //data is now in the structure
               //get the msg type
               MsgTyp = GetMsgType(ddHcpS);
               MacID = ByteToString(ddHcpS.dStruct.D_chaddr, ddHcpS.dStruct.D_hlen);// (string)ddHcpS.dStruct.D_chaddr;
               switch( MsgTyp )
               {
                   case DHCPMsgType.DHCPDISCOVER:
                       //myClass.Process(myLogger);
                       // a Mac has requested an IP
                       // discover Msg Has been sent
                       Announced(ddHcpS, MacID);
                       break;
                   case DHCPMsgType.DHCPREQUEST:
                       Request(ddHcpS, MacID);
                       break;
               }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



       private void CreateOptionStruct(ref cDHCPStruct ddHcps, DHCPMsgType OptionReplyMsg)
        {
            byte[] PReqList, t1, LeaseTime, MyIp;

            try
            {
                //we look for the parameter request list
                PReqList = GetOptionData(DHCPOptionEnum.ParameterRequestList, ddHcps);
                //erase the options array, and set the message type to ack
                ddHcps.dStruct.D_options = null;
                CreateOptionElement(DHCPOptionEnum.DHCPMessageTYPE, new byte[] { (byte)OptionReplyMsg }, ref ddHcps.dStruct.D_options);
                //server identifier, my IP
                MyIp = IPAddress.Parse(ddHcps.dData.MyIP).GetAddressBytes();
                CreateOptionElement(DHCPOptionEnum.ServerIdentifier, MyIp, ref ddHcps.dStruct.D_options);
               

                //PReqList contains the option data in a byte that is requested by the unit
                foreach (byte i in PReqList)
                {
                    t1 = null;
                    switch ((DHCPOptionEnum)i)
                    {
                        case DHCPOptionEnum.SubnetMask:
                            t1 = IPAddress.Parse(ddHcps.dData.SubMask).GetAddressBytes();
                            break;
                        case DHCPOptionEnum.Router:
                            t1 = IPAddress.Parse(ddHcps.dData.RouterIP).GetAddressBytes();
                            break;
                        case DHCPOptionEnum.DomainNameServer:
                            t1 = IPAddress.Parse(ddHcps.dData.DomainIP).GetAddressBytes();
                            break;
                        case DHCPOptionEnum.DomainName:
                            t1 = System.Text.Encoding.ASCII.GetBytes(ddHcps.dData.ServerName);
                            break;
                        case DHCPOptionEnum.ServerIdentifier:
                            t1 = IPAddress.Parse(ddHcps.dData.MyIP).GetAddressBytes();
                            break;
                        case DHCPOptionEnum.LogServer:
                            t1 = System.Text.Encoding.ASCII.GetBytes(ddHcps.dData.LogServerIP);
                            break;
                        case DHCPOptionEnum.NetBIOSoverTCPIPNameServer:
                            break;

                    }
                    if (t1 != null)
                        CreateOptionElement((DHCPOptionEnum)i, t1, ref ddHcps.dStruct.D_options);
                }
                
                //lease time
                LeaseTime = new byte[4];
                LeaseTime[3] = (byte)(ddHcps.dData.LeaseTime);
                LeaseTime[2] = (byte)(ddHcps.dData.LeaseTime >> 8);
                LeaseTime[1] = (byte)(ddHcps.dData.LeaseTime >> 16);
                LeaseTime[0] = (byte)(ddHcps.dData.LeaseTime >> 24);
                CreateOptionElement(DHCPOptionEnum.IPAddressLeaseTime, LeaseTime, ref ddHcps.dStruct.D_options);
                CreateOptionElement(DHCPOptionEnum.RenewalTimeValue_T1, LeaseTime, ref ddHcps.dStruct.D_options);
                CreateOptionElement(DHCPOptionEnum.RebindingTimeValue_T2, LeaseTime, ref ddHcps.dStruct.D_options);
                //create the end option
                Array.Resize(ref ddHcps.dStruct.D_options, ddHcps.dStruct.D_options.Length + 1);
                Array.Copy(new byte[] { 255 }, 0, ddHcps.dStruct.D_options, ddHcps.dStruct.D_options.Length - 1, 1);
                //send the data to the unit
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                LeaseTime = null;
                PReqList = null;
                t1 = null;
                
            }
        }

        //mac announced itself, established IP etc....
        //send the offer to the mac
        public void SendDHCPMessage(DHCPMsgType msgType, cDHCPStruct ddHcpS)
        {
            byte[] Subn, HostID,  DataToSend;
           

            //we shall leave everything as is structure wise
            //shall CHANGE the type to OFFER
            //shall set the client's IP-Address
            try
            {
                //change message type to reply
                ddHcpS.dStruct.D_op = 2; //reply
                //subnet
                Subn = IPAddress.Parse(ddHcpS.dData.SubMask).GetAddressBytes();
                //create your ip address
                ddHcpS.dStruct.D_yiaddr = IPAddress.Parse(ddHcpS.dData.IPAddr).GetAddressBytes();
                
                //Host ID
                HostID = System.Text.Encoding.ASCII.GetBytes(ddHcpS.dData.ServerName);

                CreateOptionStruct(ref ddHcpS, msgType);
                //send the data to the unit
                DataToSend = BuildDataStructure(ddHcpS.dStruct);
                cUdp.SendData(DataToSend);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Subn = null;
                //LeaseTime= null;
                HostID= null;
                
                DataToSend = null;
            }
        }

        //function to build the data structure to a byte array
        private byte[] BuildDataStructure(cDHCPStruct.DHCPstruct ddHcpS)
        {
            byte[] mArray;

            try
            {
                mArray = new byte[0];
                AddOptionElement(new byte[] { ddHcpS.D_op }, ref mArray);
                AddOptionElement(new byte[] { ddHcpS.D_htype }, ref mArray);
                AddOptionElement(new byte[] { ddHcpS.D_hlen }, ref mArray);
                AddOptionElement(new byte[] { ddHcpS.D_hops }, ref mArray);
                AddOptionElement(ddHcpS.D_xid, ref mArray);
                AddOptionElement(ddHcpS.D_secs, ref mArray);
                AddOptionElement(ddHcpS.D_flags, ref mArray);
                AddOptionElement(ddHcpS.D_ciaddr, ref mArray);
                AddOptionElement(ddHcpS.D_yiaddr, ref mArray);
                AddOptionElement(ddHcpS.D_siaddr, ref mArray);
                AddOptionElement(ddHcpS.D_giaddr, ref mArray);
                AddOptionElement(ddHcpS.D_chaddr, ref mArray);
                AddOptionElement(ddHcpS.D_sname, ref mArray);
                AddOptionElement(ddHcpS.D_file, ref mArray);
                AddOptionElement(ddHcpS.M_Cookie, ref mArray);
                AddOptionElement(ddHcpS.D_options, ref mArray);
                return mArray;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            finally
            {
                mArray = null;
            }
            
        }

        private void AddOptionElement(byte[] FromValue, ref byte[] TargetArray)
        {
            try
            {
                if (TargetArray != null)
                    Array.Resize(ref TargetArray, TargetArray.Length + FromValue.Length );
                else
                    Array.Resize(ref TargetArray, FromValue.Length );
                Array.Copy(FromValue, 0, TargetArray, TargetArray.Length - FromValue.Length, FromValue.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
 

        //create an option message 
        //shall always append at the end of the message
        private void CreateOptionElement(DHCPOptionEnum Code, byte[] DataToAdd, ref byte[] AddtoMe)
        {
            byte[] tOption;

            try
            {
                tOption = new byte[DataToAdd.Length +2];
                //add the code, and data length
                tOption[0] = (byte)Code;
                tOption[1] = (byte)DataToAdd.Length;
                //add the code to put in
                Array.Copy(DataToAdd,0,tOption,2,DataToAdd.Length);
                //copy the data to the out array
                if (AddtoMe == null)
                    Array.Resize(ref AddtoMe, (int)tOption.Length);
                else
                    Array.Resize(ref AddtoMe, AddtoMe.Length + tOption.Length);
                Array.Copy(tOption, 0, AddtoMe, AddtoMe.Length - tOption.Length , tOption.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        

    } // class clsDHCP
}
  