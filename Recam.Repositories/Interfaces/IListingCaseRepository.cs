using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Interfaces
{
    public interface IListingCaseRepository
    {
        Task AddListingCase(ListingCase listingCase);
        Task SaveChangesAsync();
    }
}
