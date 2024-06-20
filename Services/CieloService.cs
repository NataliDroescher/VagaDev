using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CartaoCard.Components.Models;

namespace CartaoCard.Services
{
    public class CieloService
    {
        private readonly HttpClient _httpClient;
        private readonly string _merchantId;
        private readonly string _merchantKey;
        private readonly string _apiUrl;

        public CieloService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _merchantId = configuration["Cielo:MerchantId"] ?? throw new ArgumentNullException("Cielo:MerchantId configuration missing");
            _merchantKey = configuration["Cielo:MerchantKey"] ?? throw new ArgumentNullException("Cielo:MerchantKey configuration missing");
            _apiUrl = configuration["Cielo:ApiUrl"] ?? throw new ArgumentNullException("Cielo:ApiUrl configuration missing");

            // Configuração inicial do HttpClient para a API da Cielo
            _httpClient.BaseAddress = new Uri(_apiUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("MerchantId", _merchantId);
            _httpClient.DefaultRequestHeaders.Add("MerchantKey", _merchantKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task CreateTransactionAsync(TransactionModel transaction)
        {
                var request = new
                {
                    MerchantOrderId = transaction.MerchantOrderId,
                    Customer = new
                    {
                        Name = "John Doe"
                    },
                    Payment = new
                    {
                        Type = "CreditCard",
                        Amount = transaction.Amount,
                        Installments = 1,
                        Capture = true,
                        CreditCard = new
                        {
                            CardNumber = transaction.CardNumber,
                            Holder = transaction.CardHolder,
                            ExpirationDate = transaction.ExpirationDate,
                            SecurityCode = transaction.SecurityCode
                        }
                    }
                };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Making POST request to: {_apiUrl}/1/sales");
            Console.WriteLine($"Request body: {json}");

            var response = await _httpClient.PostAsync("/1/sales", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro ao criar transação na Cielo. StatusCode: {response.StatusCode}. Content: {errorMessage}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JsonSerializer.Deserialize<TransactionResponse>(responseBody);
           // transaction.PaymentId = responseJson.Payment.PaymentId; // Adicione essa linha para capturar o paymentId
    
        
        }

        public class TransactionResponse
        {
            public string MerchantOrderId { get; set; }
        }

        public async Task CancelTransactionAsync(string paymentId)
        {
            try
            {
                var request = new
                {
                     
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/1/sales/{paymentId}/void", content);
                

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erro ao cancelar transação na Cielo. StatusCode: {response.StatusCode}. Content: {errorMessage}");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Erro ao cancelar transação na Cielo", ex);
            }
        }


    }
}