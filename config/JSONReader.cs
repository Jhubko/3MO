using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot.config
{
    internal class JSONReader
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string apikey { get; set; }
        public string cseId { get; set; }
        public string apiGPT { get; set; }
        public string llHostname { get; set; }
        public int llPort { get; set; }
        public string llPass { get; set; }
        public bool secured { get; set; }

        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);
                this.token = data.token;
                this.prefix = data.prefix;
                this.apikey = data.apikey;
                this.cseId = data.cseId;
                this.apiGPT = data.apiGPT;
                this.llHostname = data.llHostname;
                this.llPort = data.llPort;
                this.llPass = data.llPass;
                this.secured = data.secured;
            }
        }

    }

    internal sealed class JSONStructure
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string apikey { get; set; }
        public string cseId { get; set; }
        public string apiGPT { get; set; }
        public string llHostname { get; set; }
        public int llPort { get; set; }
        public string llPass { get; set; }

        public bool secured { get; set; }
    }
}
