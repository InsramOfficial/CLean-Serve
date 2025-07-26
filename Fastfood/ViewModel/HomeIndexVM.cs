using Fastfood.Models;

namespace Fastfood.ViewModel
{
    public class HomeIndexVM
    {
        public List<Category> Categories { get; set; }
        public List<Item> RandomItems { get; set; }
        public List<Item> LatestItems { get; set; }
    }

}
