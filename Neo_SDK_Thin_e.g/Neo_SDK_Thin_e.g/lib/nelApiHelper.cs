using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEL
{
    public class nelApiHelper
    {
        string neoCliJsonRPCUrl = string.Empty;

        public nelApiHelper(string url) {
            neoCliJsonRPCUrl = url;
        }

        httpHelper hh = new httpHelper();

        public Int64 getBlockCount()
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'getblockcount',
	            'params': [],
	            'id': '1'
            }";
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = (JObject)JObject.Parse(result)["result"][0];

            var blockCount = (Int64)resultJ["blockcount"];

            return blockCount;
        }

        public JObject getBalance(string addr)
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'getbalance',
	            'params': ['#'],
	            'id': '1'
            }";
            input = input.Replace("#", addr);
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = JObject.Parse(result);

            return resultJ;
        }

        public JObject getUTXO(string addr)
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'getutxo',
	            'params': ['#'],
	            'id': '1'
            }";
            input = input.Replace("#", addr);
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = JObject.Parse(result);

            return resultJ;
        }

        public JObject sendRawTransaction(string txHex)
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'sendrawtransaction',
	            'params': ['#'],
	            'id': '1'
            }";
            input = input.Replace("#", txHex);
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = JObject.Parse(result);

            return resultJ;
        }

        public JObject getNotify(string txid)
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'getnotify',
	            'params': ['#'],
	            'id': '1'
            }";
            input = input.Replace("#", txid);
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = JObject.Parse(result);

            return resultJ;
        }

        public JObject invokeScript(string script)
        {
            string input = @"{
	            'jsonrpc': '2.0',
                'method': 'invokescript',
	            'params': ['#'],
	            'id': '1'
            }";
            input = input.Replace("#", script);
            string result = hh.Post(neoCliJsonRPCUrl, input, System.Text.Encoding.UTF8, 1);
            JObject resultJ = JObject.Parse(result);

            return resultJ;
        }
    }
}
