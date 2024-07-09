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
            try
            {
                var request = new
                {
                    MerchantOrderId = transaction.MerchantOrderId,
                    Customer = new
                    {
                        Name = transaction.CardHolder
                    },
                    Payment = new
                    {
                        Type = "CreditCard",
                        Amount = transaction.Amount,
                        Installments = 1,
                        Capture = true, //transição capturada manualemnte aqui
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

                // Capturar o PaymentId da resposta da Cielo e atribuir ao seu modelo de transação
                transaction.PaymentId = responseJson.Payment.PaymentId;
                Console.WriteLine(transaction.PaymentId);
                
                

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar transação na Cielo: {ex.Message}");
                throw;
            }     
            Console.WriteLine("Chegou nessa parte, depois de criar");  
            Console.WriteLine(_merchantKey);
            Console.WriteLine(_merchantId);
        }

        public class TransactionResponse
        {
            public string? MerchantOrderId { get; set; }
            public string? Status { get; set; }
            public PaymentDetails? Payment { get; set; }

            public class PaymentDetails
            {
                public string? PaymentId { get; set; }
                // Outros detalhes do pagamento podem ser adicionados aqui conforme necessário
            }
        }
        public async Task<string> teste(string paymentId)
        {
            Console.WriteLine("Teste 123:");
            Console.WriteLine(paymentId);
            
            return paymentId;
        }

        public async Task<string> CaptureTransactionAsync(string paymentId)
        {
            Console.WriteLine("Chegou aqui 32");
            try
            {
                var requestUrl = $"/1/sales/{paymentId}/capture";

                var response = await _httpClient.PutAsync(requestUrl, null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erro ao capturar transação. StatusCode: {response.StatusCode}. Content: {errorMessage}");
                    return "Erro ao capturar transação";
                }

                var content = await response.Content.ReadAsStringAsync();
                var captureResponse = JsonSerializer.Deserialize<TransactionResponse>(content);

                return captureResponse.Status;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao capturar transação: {ex.Message}");
                return "Erro ao capturar transação";
            }
        }

        public async Task CancelTransactionAsync(string paymentId)
        {
            Console.WriteLine("Iniciando cancelamento da transação..."); 

            var requestUrl = $"http://apisandbox.cieloecommerce.cielo.com.br/1/sales/{paymentId}/void";

            var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            requestMessage.Headers.Add("MerchantId", _merchantId);
            requestMessage.Headers.Add("MerchantKey", _merchantKey);

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erro ao cancelar transação na Cielo. StatusCode: {response.StatusCode}. Content: {errorMessage}");
            }

            Console.WriteLine($"Transação com paymentId {paymentId} cancelada com sucesso.");
            
        
        }

       
    }
}
