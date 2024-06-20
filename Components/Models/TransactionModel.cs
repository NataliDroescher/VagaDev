
namespace CartaoCard.Components.Models
{
    public class TransactionModel
    {
        public string? MerchantOrderId { get; set; }
        public string? PaymentId { get; set; } 
        public string? CardNumber { get; set; }
        public string? CardHolder { get; set; }
        public string? ExpirationDate { get; set; }
        public string? SecurityCode { get; set; }
        public decimal? Amount { get; set; }
    
    }
}

