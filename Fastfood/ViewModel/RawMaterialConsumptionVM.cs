using Fastfood.Models;
using FastFood.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;

namespace Fastfood.ViewModel
{
    public class RawMaterialConsumptionVM
    {
        public RawMaterial_Items_Consumption Consumption { get; set; }

        [ValidateNever]
        public IEnumerable<Consumeable> Consumeables { get; set; }

        [ValidateNever]
        public IEnumerable<Item> BaseItems { get; set; }
    }
}
