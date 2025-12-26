using Fastfood.Models;
using FastFood.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fastfood.ViewModel
{
    public class RawMaterialConsumptionVM
    {
        // IMPORTANT: Initialize the list to avoid null reference issues
        public List<RawMaterial_Items_Consumption> Consumptions { get; set; }
            = new List<RawMaterial_Items_Consumption>();

        [ValidateNever]
        public IEnumerable<Consumeable> Consumeables { get; set; }

        [ValidateNever]
        public IEnumerable<Item> BaseItems { get; set; }

        [NotMapped]
        [ValidateNever]

        public string BaseItemName { get; set; }
    }
}
