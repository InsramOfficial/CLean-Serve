﻿using Fastfood.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.ViewModel
{
    public class ItemsVM
    {
        public int? ItemId { get; set; }
        public string? ItemName { get; set; }
        public int? CategoryId { get; set; }
        public int? RecentUnitPrice { get; set; }
        //public int Quanity { get; set; }
        public int? Discount { get; set; }
        public string? Remarks { get; set; }
        //public double NetTotal { get; set; }
        public string? Picture { get; set; }
        [NotMapped]
        public IFormFile ItemImage { get; set; }
        public List<Category>? category { get; set; }
    }
}
