using System.Globalization;
using System.IO;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using stock_quote_alert.Enums;
using stock_quote_alert.Interfaces;

namespace stock_quote_alert.Services
{
    public class StockQuoteAlertService : BackgroundService
    {
        private readonly StockArgs _stockArgs;
        private readonly IEmailService _emailService;
        private string apiToken = "";

        private List<string> keys = new List<string>();

        private State state = State.Neutral;

        public StockQuoteAlertService(StockArgs stockArgs, IEmailService emailService)
        {
            _stockArgs = stockArgs;
            _emailService = emailService;
            GetApiKeys();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await CheckQuote();
                await Task.Delay(5000, cancellationToken);
            }
        }

        private async Task CheckQuote()
        {
            try
            {
                var price = await StockQuoteAPI();

                // state != State.Positive para nao ficar mandando varios emails, caso já esteja no estado de positivo (venda), não precisa enviar outro email
                if (price > _stockArgs.SellingPrice && state != State.Positive)
                {
                     _emailService.SendEmail(
                        "Atualização do valor do ativo",
                        $"Olá, é aconselhavél a venda do ativo {_stockArgs.Name} já que seu preço subiu para {price.ToString(CultureInfo.InvariantCulture)}"
                                                );
                    // caso o valor esteja acima da referencia, estado positivo
                    state = State.Positive;
                }
                else if (price < _stockArgs.BuyingPrice && state != State.Negative)
                {
                     _emailService.SendEmail(
                        "Atualização do valor do ativo",
                        $"Olá, é aconselhavél a compra do ativo {_stockArgs.Name} já que seu preço desceu para {price.ToString(CultureInfo.InvariantCulture)}"
                                                );
                    // caso o valor esteja abaixo da referencia, estado negativo
                    state = State.Negative;
                }
                else if (price <= _stockArgs.SellingPrice && price >= _stockArgs.BuyingPrice && state != State.Neutral)
                {
                    // caso o valor esteja entre as referencias, estado neutro
                    state = State.Neutral;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private async Task<decimal> StockQuoteAPI()
        {
            try
            {
                var client = new HttpClient();

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://real-time-finance-data.p.rapidapi.com/stock-quote?symbol={_stockArgs.Name}&language=en"),
                    Headers =
                    {
                        { "X-RapidAPI-Key", apiToken },
                        { "X-RapidAPI-Host", "real-time-finance-data.p.rapidapi.com" },
                    },
                };

                var response = await client.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(responseContent);
                
                // caso o numero de requisições chegar no limite, a response vai retornar com uma mensagem, dai eu troco a chave da api
                if (jsonObject["message"] != null)
                {
                    ChangeApiKey();
                    Console.WriteLine("Chave de api trocada");
                    return await StockQuoteAPI();
                }

                // caso o nome do ativo não existir ou for invalido para essa api, o campo "data" vai ser vazio, portanto precisamos reiniciar o programa
                // com outro ativo
                if (jsonObject["data"].Count() == 0)
                {
                    Console.WriteLine("Nome de ativo inválido, por favor reinicie o programa!");
                    Environment.Exit(0);
                }

                var price = (string)jsonObject["data"]["price"];
                
                return Convert.ToDecimal(price, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
       
        private void GetApiKeys()
        {
            try
            {
                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string path = executableDirectory + "\\Config\\APIKeys.txt";
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        keys.Add(line.TrimEnd());
                    }
                    apiToken = keys[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
          
        }

        private void ChangeApiKey()
        {
            keys.RemoveAt(0);
            if (keys.Count == 0) Environment.Exit(0);
            apiToken = keys[0];
        }
    }
}
