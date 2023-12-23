using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using stock_quote_alert;
using stock_quote_alert.Interfaces;
using stock_quote_alert.Services;

namespace StockQuoteAlert
{
    class Program
    {

        private static async Task Main()
        {
            StockArgs stockArgs = GetParams();
            await CreateHostBuilder(stockArgs).RunConsoleAsync();
        }

        private static StockArgs GetParams()
        {
            try
            {
                Console.WriteLine("Olá, bem vindo ao programa de monitoramento de cotação de ativos");

                //Pegar ativo
                Console.WriteLine("Digite o nome do ativo a ser monitorado: ");
                string ativo = Console.ReadLine().ToUpper().TrimEnd();

                //Preço venda
                Console.WriteLine("Digite o preço de referência para venda: ");
                decimal precoVenda = decimal.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

                //Preço compra
                Console.WriteLine("Digite o preço de referência para compra: ");
                decimal precoCompra = decimal.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

                return new StockArgs() { Name = ativo, SellingPrice = precoVenda, BuyingPrice = precoCompra };
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message);
                return GetParams();
            }
            
        }

        private static IHostBuilder CreateHostBuilder(StockArgs args) {
            return Host.CreateDefaultBuilder()
                   .ConfigureServices((context, services) =>
                   {
                       services.AddSingleton(args);
                       services.AddSingleton<IEmailService, EmailService>();
                       services.AddHostedService<StockQuoteAlertService>();
                   });
                   
        }

        
    }
}