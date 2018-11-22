using System;
using System.Collections.Generic;
using NEL;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace Neo_SDK_Thin_e.g
{
    class Program
    {
        static neoHelper.neoAddress neoAddress;
        static string nelAPIurl = "https://api.nel.group/api/testnet";
        static nelApiHelper nelApi = new nelApiHelper(nelAPIurl);
        static neoHelper.neoTranstion nTX = new neoHelper.neoTranstion(nelAPIurl);
        static string CGAShash = "74f2dc36a68fdc4682034178eb2220729231db76";

        static void Main(string[] args)
        {
            while(true)
            {
                if (initAddr()) break;
            }



            while (true)
            {
                showMenu();
            }

            //Console.ReadKey();
        }

        static private bool initAddr()
        {
            Console.WriteLine("Input NEO WIF:");
            string WIF = Console.ReadLine();
            try
            {
                neoAddress = new neoHelper.neoAddress(WIF);
                //PriKey = ThinNeo.Helper_NEO.GetPrivateKeyFromWIF(WIF);
                //PubKey = ThinNeo.Helper_NEO.GetPublicKey_FromPrivateKey(PriKey);
                //addrStr = ThinNeo.Helper_NEO.GetAddress_FromPublicKey(PubKey);
                Console.WriteLine("NEO address: " + neoAddress.addrStr);
                return true;
            }
            catch
            {
                Console.WriteLine("Error WIF,retry!");
                return false;
            }
        }

        static private void showMenu()
        {
            Console.WriteLine("**************************************");
            Console.WriteLine("select function to run(input Num):");
            Console.WriteLine("1:transfer GAS(UTXO)");
            Console.WriteLine("2:get CGAS balanceOf");
            Console.WriteLine("3:transfer CGAS(NEP5)");
            Console.WriteLine("**************************************");
            Console.WriteLine("");

            var selectNum = Console.ReadLine();

            switch (selectNum)
            {
                case "1":
                    Console.WriteLine("1:transfer GAS(UTXO)");
                    Console.WriteLine("");
                    Console.WriteLine("input addr to (Payee)");
                    var addrTo = Console.ReadLine();
                    try
                    {
                        var addrSH = ThinNeo.Helper_NEO.GetScriptHash_FromAddress(addrTo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                    
                    Console.WriteLine("input amount");
                    var amount = Console.ReadLine();
                    Console.WriteLine("input assetid (no input is GAS)");
                    var assetid = Console.ReadLine();
                    if (assetid.Length == 0)
                    {
                        Console.WriteLine("txid: " + transfer_UTXO(addrTo, decimal.Parse(amount)));
                    }
                    else
                    {
                        Console.WriteLine("txid: " + transfer_UTXO(addrTo, decimal.Parse(amount),assetid));
                    }
                    break;
                case "2":
                    Console.WriteLine("2:get CGAS balanceOf");
                    Console.WriteLine("");
                    Console.WriteLine("you have: " + getContractInfo_NEP5_balanceOf(CGAShash, neoAddress.addrStr) + " CGAS");
                    break;
                case "3":
                    Console.WriteLine("3:transfer CGAS(NEP5)");
                    Console.WriteLine("");
                    Console.WriteLine("input addr to (Payee)");
                    var addrToNep5 = Console.ReadLine();
                    try
                    {
                        var addrSH = ThinNeo.Helper_NEO.GetScriptHash_FromAddress(addrToNep5);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }

                    Console.WriteLine("input amount");
                    var amountNep5 = new BigInteger(decimal.Parse(Console.ReadLine()) * (decimal)Math.Pow(10, 8));

                    Console.WriteLine("txid: " + invokeContract_NEP5_transfer(CGAShash, addrToNep5, amountNep5));

                    break;
                default:
                    Console.WriteLine("select Num wrong!");
                    Console.WriteLine("");
                    break;
            }
        }

        static private string transfer_UTXO(string addrTo,decimal amount, string assetid = neoHelper.GAShash)
        {          
            Dictionary<string, List<neoHelper.neoTranstion.Utxo>> dirUTXO = nTX.GetUTXOByAddress(neoAddress.addrStr);
            if (!dirUTXO.ContainsKey("0x" + assetid)) return "asset UTXO is null!";

            ThinNeo.Transaction TX = nTX.makeTransferTx(dirUTXO["0x" + assetid], addrTo, assetid, amount);

            JObject res = nelApi.sendRawTransaction(nTX.signAndGetTxHex(neoAddress, TX));

            return (string)res["result"][0]["txid"];
        }

        static private decimal getContractInfo_NEP5_balanceOf(string NEP5hash, string addr)
        {
            JArray inputJA = JArray.Parse(@"
                  [
                    '(str)balanceOf',
                        [
                            '(addr)#'
                        ]
                  ]
                ".Replace("#", addr));

            JObject balanceJ = nelApi.invokeScript(neoHelper.jarray2script(NEP5hash, inputJA));
            var valueHex = (string)balanceJ["result"][0]["stack"][0]["value"];
            var value = neoHelper.hexstring2Decimal(valueHex, 8);

            return value;
        }

        static private string invokeContract_NEP5_transfer(string NEP5hash,string addrTo, System.Numerics.BigInteger amount)
        {
            JArray inputJA = JArray.Parse(@"
                  [
                    '(str)transfer',
                        [
                            '(addr)[1]',
                            '(addr)[2]',
                            '(int)[3]'
                        ]
                  ]
                ".Replace("[1]", neoAddress.addrStr).Replace("[2]", addrTo).Replace("[3]", amount.ToString()));

            return nTX.sendSimpleInvokeTx(neoAddress, neoAddress.addrStr, NEP5hash, inputJA);
        }
    }
}
