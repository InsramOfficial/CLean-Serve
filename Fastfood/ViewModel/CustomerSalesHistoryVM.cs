using Fastfood.Models;

namespace Fastfood.ViewModel
{
    public class CustomerSalesHistoryVM
    {
        public Client Customer { get; set; }
        public List<Sales> Sales { get; set; }
    }
}
