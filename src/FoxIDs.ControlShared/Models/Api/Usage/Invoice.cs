﻿using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class Invoice : IValidatableObject
    {
        [Required]
        public string InvoiceNumber { get; set; }

        [Required]
        public long CreateTime { get; set; }

        public bool IsCreditNote { get; set; }

        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        public List<InvoiceLine> Lines { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal Price { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal Vat { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Time specification items,
        /// </summary>
        [ListLength(Constants.Models.Used.ItemsMin, Constants.Models.Used.ItemsMin)]
        public List<UsedItem> TimeItems { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (TimeItems?.Count() > 0)
            {
                if (TimeItems.Where(i => i.Type != UsedItemTypes.Hours).Any())
                {
                    results.Add(new ValidationResult($"Only {nameof(UsedItem)} with the {nameof(UsedItem.Type)} of '{UsedItemTypes.Hours}' is allowed in the {nameof(TimeItems)} field.", [nameof(TimeItems)]));
                }
            }
            return results;
        }
    }
}