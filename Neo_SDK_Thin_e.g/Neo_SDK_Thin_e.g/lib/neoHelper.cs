using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinNeo;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace NEL
{
    static public class neoHelper
    {
        public const string GAShash = "602c79718b16e442de58778e148d0b1084e3b2dffd5de6b7b16cee7969282de7";

        public class neoAddress
        {
            public string addrStr { set; get; }
            public string addrPubKey { set; get; }
            public byte[] addrPubKeyBytes { set; get; }
            public string addrPriKey { set; get; }
            public byte[] addrPriKeyBytes { set; get; }

            public neoAddress(string WIF) {
                addrPriKeyBytes = ThinNeo.Helper_NEO.GetPrivateKeyFromWIF(WIF);
                addrPubKeyBytes = ThinNeo.Helper_NEO.GetPublicKey_FromPrivateKey(addrPriKeyBytes);
                addrPriKey = ThinNeo.Helper.Bytes2HexString(addrPriKeyBytes);
                addrPubKey = ThinNeo.Helper.Bytes2HexString(addrPubKeyBytes);
                addrStr = ThinNeo.Helper_NEO.GetAddress_FromPublicKey(addrPubKeyBytes);
            }
        }

        public static decimal hexstring2Decimal(string hexStr,int decimals) {
            if (hexStr == "False") return 0;

            var value = decimal.Parse(new BigInteger(Helper.HexString2Bytes(hexStr)).ToString());
            value = value / (decimal)Math.Pow(10, decimals);

            return value;
        }

        public static string Hexstring2String(this string hexstr)
        {
            List<byte> byteArray = new List<byte>();

            for (int i = 0; i < hexstr.Length; i = i + 2)
            {
                string s = hexstr.Substring(i, 2);
                byteArray.Add(Convert.ToByte(s, 16));
            }

            string str = Encoding.UTF8.GetString(byteArray.ToArray());

            return str;
        }

        public static string jarray2script(string contractHash, JArray JA)
        {
            ThinNeo.ScriptBuilder tmpSb = new ThinNeo.ScriptBuilder();
            httpHelper hh = new httpHelper();
            //var json = MyJson.Parse(JsonConvert.SerializeObject(paramsJA[n])).AsList();
            //var list = JA.AsList();
            for (int i = JA.Count - 1; i >= 0; i--)
            {
                tmpSb.EmitParamJson(JA[i]);
            }

            var scripthashReverse = ThinNeo.Helper.HexString2Bytes(contractHash).Reverse().ToArray();
            tmpSb.EmitAppCall(scripthashReverse);
            string invokeSc = ThinNeo.Helper.Bytes2HexString(tmpSb.ToArray());

            return invokeSc;
        }

        public class neoTranstion
        {
            nelApiHelper nelApi;

            public neoTranstion(string url)
            {
                nelApi = new nelApiHelper(url);
            }

            public class Utxo
            {
                //txid[n] 是utxo的属性
                public ThinNeo.Hash256 txid;
                public int n;

                //asset资产、addr 属于谁，value数额，这都是查出来的
                public string addr;
                public string asset;
                public decimal value;
                public Utxo(string _addr, ThinNeo.Hash256 _txid, string _asset, decimal _value, int _n)
                {
                    this.addr = _addr;
                    this.txid = _txid;
                    this.asset = _asset;
                    this.value = _value;
                    this.n = _n;
                }
            }

            public string signAndGetTxHex(neoAddress neoAddress, Transaction TX)
            {
                //添加鉴证（签名）
                var signdata = ThinNeo.Helper_NEO.Sign(TX.GetMessage(), neoAddress.addrPriKeyBytes);
                var b = ThinNeo.Helper.Bytes2HexString(neoAddress.addrPubKeyBytes);
                TX.AddWitness(signdata, neoAddress.addrPubKeyBytes, neoAddress.addrStr);

                //交易序列化
                var trandata = TX.GetRawData();
                var strtrandata = ThinNeo.Helper.Bytes2HexString(trandata);

                return strtrandata;
            }

            public string sendSimpleInvokeTx(neoAddress neoAddress,string addrTransfer, string contractHash, JArray inputJA)
            {
                //获取可用NEO UTXO
                Dictionary<string, List<neoHelper.neoTranstion.Utxo>> dir = GetUTXOByAddress(neoAddress.addrStr);
                if (!dir.ContainsKey("0x" + neoHelper.GAShash)) return "没有GAS";

                ThinNeo.Transaction TX = null;
                {
                    //构造转账部分交易(自己给自己)与手续费
                    TX = makeTransferTx(dir["0x" + neoHelper.GAShash], addrTransfer, neoHelper.GAShash, 0, (decimal)0.00000000);

                    //构造合约调用部分
                    TX = makeInvokeTxFromTransferTx(TX, contractHash, inputJA, (decimal)0.00000000);


                    //发送交易
                    string txHexStr = signAndGetTxHex(neoAddress, TX);
                    var res = nelApi.sendRawTransaction(txHexStr);
                    return (string)res["result"][0]["txid"];
                }
            }

            public Transaction makeInvokeTxFromTransferTx(Transaction transferTx, string contractHash, JArray inputJA, decimal contractGas)
            {
                var TX = transferTx;
                TX.type = TransactionType.InvocationTransaction;
                var itd = new InvokeTransData();
                TX.extdata = itd;
                itd.script = ThinNeo.Helper.HexString2Bytes(neoHelper.jarray2script(contractHash, inputJA));
                itd.gas = contractGas;

                return TX;
            }

            public Transaction makeTransferTx(List<Utxo> utxos, string targetaddr, string assetid, decimal sendcount, decimal extgas = 0, List<Utxo> utxos_ext = null, string extaddr = null)
            {
                byte[] assetidBytes = new Hash256(assetid);

                var tran = new Transaction();
                tran.type = TransactionType.ContractTransaction;
                if (extgas >= 1)
                {
                    tran.version = 1;//0 or 1
                }
                else
                {
                    tran.version = 0;//0 or 1
                }
                tran.extdata = null;

                tran.attributes = new ThinNeo.Attribute[0];
                var scraddr = "";
                utxos.Sort((a, b) =>
                {
                    if (a.value > b.value)
                        return 1;
                    else if (a.value < b.value)
                        return -1;
                    else
                        return 0;
                });
                decimal count = decimal.Zero;
                List<TransactionInput> list_inputs = new List<TransactionInput>();
                for (var i = 0; i < utxos.Count; i++)
                {
                    TransactionInput input = new TransactionInput();
                    input.hash = utxos[i].txid;
                    input.index = (ushort)utxos[i].n;
                    list_inputs.Add(input);
                    count += utxos[i].value;
                    scraddr = utxos[i].addr;
                    if (count >= sendcount)
                    {
                        break;
                    }
                }
                decimal count_ext = decimal.Zero;
                if (utxos_ext != null)
                {
                    //手续费
                    TransactionInput input = new TransactionInput();
                    input.hash = utxos_ext[0].txid;
                    input.index = (ushort)utxos_ext[0].n;
                    count_ext = utxos_ext[0].value;
                    list_inputs.Add(input);
                }

                tran.inputs = list_inputs.ToArray();
                if (count >= sendcount)//输入大于等于输出
                {
                    List<TransactionOutput> list_outputs = new List<TransactionOutput>();
                    //输出
                    if (sendcount > decimal.Zero && targetaddr != null)
                    {
                        TransactionOutput output = new TransactionOutput();
                        output.assetId = assetidBytes;
                        output.value = sendcount;
                        output.toAddress = ThinNeo.Helper_NEO.GetScriptHash_FromAddress(targetaddr);
                        list_outputs.Add(output);
                    }
                    var change = count - sendcount - extgas;
                    decimal extchange = decimal.Zero;
                    //找零
                    if (utxos_ext != null)
                    {
                        change = count - sendcount;
                        extchange = count_ext - extgas;
                    }
                    else
                    {
                        change = count - sendcount - extgas;
                    }
                    if (change > decimal.Zero)
                    {
                        TransactionOutput outputchange = new TransactionOutput();
                        outputchange.toAddress = ThinNeo.Helper_NEO.GetScriptHash_FromAddress(scraddr);
                        outputchange.value = change;
                        outputchange.assetId = assetidBytes;
                        list_outputs.Add(outputchange);
                    }
                    if (extchange > decimal.Zero)
                    {
                        TransactionOutput outputchange = new TransactionOutput();
                        outputchange.toAddress = ThinNeo.Helper_NEO.GetScriptHash_FromAddress(extaddr);
                        outputchange.value = extchange;
                        outputchange.assetId = assetidBytes;
                        list_outputs.Add(outputchange);
                    }
                    tran.outputs = list_outputs.ToArray();
                }
                else
                {
                    throw new Exception("no enough money.");
                }
                return tran;
            }

            //获取地址的utxo来得出地址的资产  
            public Dictionary<string, List<Utxo>> GetUTXOByAddress(string addr)
            {
                JObject response = nelApi.getUTXO(addr);
                JArray resJA = (JArray)response["result"];
                Dictionary<string, List<Utxo>> _dir = new Dictionary<string, List<Utxo>>();
                foreach (JObject j in resJA)
                {
                    Utxo utxo = new Utxo(j["addr"].ToString(), new ThinNeo.Hash256(j["txid"].ToString()), j["asset"].ToString(), decimal.Parse(j["value"].ToString()), int.Parse(j["n"].ToString()));
                    if (_dir.ContainsKey(j["asset"].ToString()))
                    {
                        _dir[j["asset"].ToString()].Add(utxo);
                    }
                    else
                    {
                        List<Utxo> l = new List<Utxo>();
                        l.Add(utxo);
                        _dir[j["asset"].ToString()] = l;
                    }

                }
                return _dir;
            }
        }
    }
}
