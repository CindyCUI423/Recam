using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IListingCaseService
    {
        Task<int> CreateListingCase(CreateListingCaseRequest request, string userId);
        Task<GetListingCasesResponse> GetListingCasesByUser(int pageNumber, int pageSize, string userId, string role);
        Task<ListingCaseDetailResponse> GetListingCaseById(string userId, string role, int id);
    }
}
