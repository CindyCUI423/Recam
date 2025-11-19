using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recam.Models.Enums;

namespace Recam.Models.Entities
{
    public class MediaAsset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public MediaType MediaType { get; set; }
        public string MediaUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsSelect { get; set; }
        public bool IsHero { get; set; }

        [ForeignKey(nameof(ListingCase))]
        public int ListingCaseId { get; set; }
        public ListingCase ListingCase { get; set; }

        [ForeignKey(nameof(User))]
        public string UserId { get; set; }
        public User User { get; set; }
        public bool IsDeleted { get; set; }
    }
}
