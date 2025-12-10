using Recam.Models.Enums;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IListingCaseService
    {
        Task<int> CreateListingCase(CreateListingCaseRequest request, string userId);
        Task<GetListingCasesResponse> GetListingCasesByUser(int pageNumber, int pageSize, string userId, string role);
        Task<ListingCaseDetailResponse> GetListingCaseById(int id, ClaimsPrincipal User);
        Task<UpdateListingCaseResponse> UpdateListingCase(int id, UpdateListingCaseRequest request, ClaimsPrincipal user);
        Task<ChangeListingCaseStatusResponse> ChangeListingCaseStatus(int id, ChangeListingCaseStatusRequest request, ClaimsPrincipal user);
        Task<DeleteListingCaseResponse> DeleteListingCase(int id, ClaimsPrincipal user);
    }
}
