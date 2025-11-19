using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class CaseContact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CompanyName { get; set; }
        public string ProfileUrl { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        [ForeignKey(nameof(ListingCase))]
        public int ListingCaseId { get; set; }
        public ListingCase ListingCase { get; set; }
    }
}
