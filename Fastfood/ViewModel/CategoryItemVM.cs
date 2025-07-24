using Fastfood.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Fastfood.ViewModel
{
    public class CategoryItemVM
    {
        public List<Category>? category { get; set; }
        public List<SaledItems>? DynamicData { get; set; } = new List<SaledItems>();
        public string? CustomerName { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DeliveryMethod { get; set; }
        public double? GrossTotal { get; set; }
        public double? BilDiscount { get; set; }
        public double? FinalBillTotal { get; set; }
        public double? CashReceived { get; set; }
        public double? CashPayBack { get; set; }
        public string? CardNumber { get; set; }
        public string? AuthID { get; set; }
        public int? IDforUpdateRecord { get; set; }
        public BankSattlement? BankSattlement { get; set; } = new BankSattlement();
        public List<Client>? clients { get; set; } = new List<Client>();
        public Client? Clientdetails { get; set; }

    }
}
