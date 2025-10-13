using Fastfood.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fastfood.ViewModels
{
    public class PurchasedItemVM
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public bool IsConsumeable { get; set; }
    }

    public class PurchaseVM
    {
        [Required]
        public int SupplierId { get; set; }

       
        public DateTime? PurchaseDate { get; set; }

        
        public decimal? InvoiceNo { get; set; }

        public decimal? Payment { get; set; }
        public string? DealingPerson { get; set; }
        public string? OrderOrpurchase { get; set; }

        public decimal? FlatDisc { get; set; }

        public decimal? Misc { get; set; }

        public decimal? Commesion { get; set; }

        public List<PurchasedItemVM>? PurchasedItems { get; set; } = new List<PurchasedItemVM>();
        public List<SelectListItem> ?Suppliers { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ?Items { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem>? Consumeables { get; set; }
    }
}
