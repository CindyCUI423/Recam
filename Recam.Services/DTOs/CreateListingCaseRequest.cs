using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class CreateListingCaseRequest
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Postcode { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Garages { get; set; }
        public double FloorArea { get; set; }
        public PropertyType PropertyType { get; set; }
    }
}
