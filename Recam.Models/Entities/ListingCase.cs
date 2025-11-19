using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class ListingCase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        public bool IsDeleted { get; set; }
        public PropertyType PropertyType { get; set; }
        public SaleCategory SaleCategory { get; set; }
        public ListingCaseStatus ListingCaseStatus { get; set; }

        [ForeignKey(nameof(User))]
        public string UserId { get; set; }
        public ICollection<AgentListingCase> AgentListingCases { get; set; } = new List<AgentListingCase>();
        public ICollection<CaseContact> CaseContacts { get; set; } = new List<CaseContact>();
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();





    }
}
