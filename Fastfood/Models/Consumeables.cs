using Fastfood.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace FastFood.Models
    {
        [Table("Consumeables")]
        public class Consumeable
        {
            [Key]
            public int CMID { get; set; }
         
            public string? CMName { get; set; }
         
            public decimal? UnitPrice { get; set; }
            
            public decimal? StockAletQty { get; set; }
            public decimal? PackWeight { get; set; }

            public DateTime? Expiry { get; set; }
            public int? UnitId { get; set; }  // FK to UnitPrice table

            public decimal? ExpiryAlertDays { get; set; }
            [ValidateNever]
            public UnitPrice? Unit { get; set; }


        public bool? Status { get; set; }
        }
    }
