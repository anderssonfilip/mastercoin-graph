open FSharp.Data
open System 
open System.IO
open System.Net
open System.Text.RegularExpressions

let ePoch = new DateTime(1970, 1, 1, 0, 0, 0)

type tx = {
            sender : String
            receiver : String
            amount: Decimal
            block : int
            currencyId : int
            details : String
            icon : String
            index : int
            invalid : bool
            method_ : String
            transactionType : int
            transactionVersion : int
            txHash : String
            txTime : DateTime
            txType : String
         }


// msc  balances: https://masterchain.info/mastercoin_verify/addresses/0
// tmsc balances: https://masterchain.info/mastercoin_verify/addresses/1
 
// samples:
// address tx:     https://masterchain.info/addr/1ACqY4wjo1hMehDpD8jipWfmX7yvuZpUn1.json
// tx:             https://masterchain.info/tx/12db8ac3c80cc09083a84d66bfdf2e0f73c54f44a11ffec164dd6dd05e488629.json
// tx per address: https://masterchain.info/mastercoin_verify/transactions/1zobh1SwsVdk3uMjEHeGV86hjNZeJ5CwC
 

type Addr =     JsonProvider<"addr.json">
type Balance =  JsonProvider<"balances.json">

let lineSequence(file) = 
        let reader = File.OpenText(file) 
        Seq.unfold(fun line -> 
            if line = null then 
                reader.Close() 
                None 
            else 
                Some(line,reader.ReadLine())) (reader.ReadLine())


[<EntryPoint>]
let Main args =

    let data = Seq.toList (lineSequence("balances.json"))

    let matches = Regex.Matches(data.Head, "(?<=\"address\":\s\").+?(?=\")") |> Seq.cast<Match> |> Seq.map (fun m -> m.Value)

    let addresses = Seq.toList matches
    
    //let address = lineSequence("addr.json") |> Seq.head

    //let req = WebRequest.Create(String.Format("https://masterchain.info/addr/{0}.json", addresses.Head))
    let req = WebRequest.Create("https://masterchain.info/addr/1KHE3kL1tkiswq27hPH7R8AT1X3u9UaCzy.json")
    let resp = req.GetResponse()
    let stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)

    let addr = Addr.Parse(reader.ReadToEnd())

    let sentMSC = seq {for tx in addr.``0``.SentTransactions do
                         yield {sender = tx.FromAddress
                                receiver = tx.ToAddress
                                amount = tx.FormattedAmount
                                block = tx.Block
                                currencyId = tx.CurrencyId
                                details = tx.Details
                                icon = tx.Icon
                                index = tx.Index
                                invalid = tx.Invalid
                                method_ = tx.Method
                                transactionType = tx.TransactionType
                                transactionVersion = tx.TransactionVersion
                                txHash = tx.TxHash
                                txTime = ePoch.AddTicks(tx.TxTime)
                                txType = tx.TxTypeStr}} |> Seq.toList

    let sentTMSC = seq {for tx in addr.``1``.SentTransactions do
                         yield {sender = tx.FromAddress
                                receiver = tx.ToAddress
                                amount = tx.FormattedAmount
                                block = tx.Block
                                currencyId = tx.CurrencyId
                                details = tx.Details
                                icon = tx.Icon
                                index = tx.Index
                                invalid = tx.Invalid
                                method_ = tx.Method
                                transactionType = tx.TransactionType
                                transactionVersion = tx.TransactionVersion
                                txHash = tx.TxHash
                                txTime = ePoch.AddTicks(tx.TxTime)
                                txType = tx.TxTypeStr}} |> Seq.toList

    use writer = new StreamWriter("txMSC.csv", false, System.Text.Encoding.UTF8)
    Seq.iter (fun (t:tx) -> writer.WriteLine(String.Join(",", t.sender, t.receiver, t.amount))) sentMSC

    use writer = new StreamWriter("txTMSC.csv", false, System.Text.Encoding.UTF8)
    Seq.iter (fun (t:tx) -> writer.WriteLine(String.Join(",", t.sender, t.receiver, t.amount))) sentTMSC



    0 // return OK


