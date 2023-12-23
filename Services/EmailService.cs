using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using stock_quote_alert.Entities;
using stock_quote_alert.Interfaces;

namespace stock_quote_alert
{
    public class EmailService : IEmailService
    {

        private SMTPConfig config;

        public EmailService() 
        { 
            ReadConfig();
        }
        
        public void ReadConfig()
        {
            try
            {
                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Config\\SMTPConfig.json";

                using (StreamReader sr = new StreamReader(path))
                {
                    string json = sr.ReadToEnd();
                    config = JsonConvert.DeserializeObject<SMTPConfig>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void SendEmail(string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(config.Host)
                {
                    Port = config.Port,
                    Credentials = new NetworkCredential(config.Username.Trim(), config.Password.Trim()),
                    EnableSsl = config.Ssl
                };

                smtpClient.Send(config.Username, config.Recipient, subject, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
