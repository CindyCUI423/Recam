using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        Task AddAgent(Agent agent);
        Task AddPhotographyCompany(PhotographyCompany photographyCompany);
        Task<Agent?> GetAgentByUserId(string userId);
        Task<PhotographyCompany?> GetPhotographyCompanyByUserId(string userId);
        Task<List<User>> GetUsersPaginated(int pageNumber, int pageSize);
        Task<int> GetUsersTotal();
        Task<List<int>?> GetAssociatedListingCaseIds(string userId);
        Task<List<int>?> GetAssignedListingCaseIds(string userId);
    }
}
