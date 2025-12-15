using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class GetListingCasesResponse
    {
        public GetListingCasesStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ListingCaseDto>? ListingCases { get; set; } = new();
        public int? TotalCount { get; set; }
    }

    public enum GetListingCasesStatus
    {
        Success,
        BadRequest,
        Unauthorized
    }

    public class ListingCaseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Postcode { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public decimal Price { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int Garages { get; set; }
        public double FloorArea { get; set; }
        public DateTime CreatedAt { get; set; }
        public PropertyType PropertyType { get; set; }
        public SaleCategory SaleCategory { get; set; }
        public ListingCaseStatus ListingCaseStatus { get; set; }
        public string UserId { get; set; }

    }
}
