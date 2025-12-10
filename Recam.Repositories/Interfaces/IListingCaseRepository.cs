using Recam.Models.Entities;
using Recam.Models.Enums;
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
        Task<List<ListingCase>> GetListingCasesForPhotographyCompany(string userId);
        Task<List<ListingCase>> GetListingCasesForAgent(string userId);
        Task<ListingCase?> GetListingCaseById(int id);
        Task<int> ChangeListingCaseStatus(int id, ListingCaseStatus status);
        Task<int> DeleteListingCase(int id);
        Task SaveChangesAsync();
    }
}
