using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Recam.Services.DTOs.DeleteListingCaseResponse;

namespace Recam.Services.Services
{
    public class ListingCaseService : IListingCaseService
    {
        private readonly IListingCaseRepository _listingCaseRepository;
        private readonly ICaseHistoryRepository _caseHistoryRepository;
        private IMapper _mapper;
        private readonly IAuthorizationService _authorizationService;

        public ListingCaseService(IListingCaseRepository listingCaseRepository, ICaseHistoryRepository caseHistoryRepository,
            IMapper mapper, IAuthorizationService authorizationService)
        {
            _listingCaseRepository = listingCaseRepository;
            _caseHistoryRepository = caseHistoryRepository;
            _mapper = mapper;
            _authorizationService = authorizationService;
        }

        public async Task<int> CreateListingCase(CreateListingCaseRequest request, string userId)
        {
            var listingCase = _mapper.Map<ListingCase>(request);

            listingCase.CreatedAt = DateTime.UtcNow;
            listingCase.IsDeleted = false;
            listingCase.SaleCategory = SaleCategory.ForSale;
            listingCase.ListingCaseStatus = ListingCaseStatus.Created;
            listingCase.UserId = userId;

            await _listingCaseRepository.AddListingCase(listingCase);
            await _listingCaseRepository.SaveChangesAsync();

            // Log listing case creation
            await LogListingCaseHistory(listingCase.Id, request.Title, "Creation", null, userId);

            return listingCase.Id;
        }

        public async Task<GetListingCasesResponse> GetListingCasesByUser(int pageNumber, int pageSize, string userId, string role)
        {
            // If pageNumber or pageSize is invalid
            if (pageNumber < 1 || pageSize < 1)
            {
                return new GetListingCasesResponse
                {
                    Status = GetListingCasesStatus.BadRequest,
                    ErrorMessage = "pageNumber and pageSize must be greater than 0."
                };
            }

            List<ListingCase> cases;

            if (role == "PhotographyCompany")
            {
                cases = await _listingCaseRepository.GetListingCasesForPhotographyCompany(userId);
            }
            else if (role == "Agent")
            {
                cases = await _listingCaseRepository.GetListingCasesForAgent(userId);
            }
            // If role is invalid
            else
            {
                return new GetListingCasesResponse
                {
                    Status = GetListingCasesStatus.Unauthorized,
                    ErrorMessage = "Invalid user role."
                };
            }

            // Paginate results
            var totalCount = cases.Count();

            var paginatedCases = cases
                .OrderByDescending(c => c.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var mappedCases = _mapper.Map<List<ListingCaseDto>>(paginatedCases); 

            return new GetListingCasesResponse
            {
                Status = GetListingCasesStatus.Success,
                ListingCases = mappedCases,
                TotalCount = totalCount
            };

        }

        public async Task<ListingCaseDetailResponse> GetListingCaseById(int id, ClaimsPrincipal user)
        {
            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                return new ListingCaseDetailResponse
                {
                    Status = ListingCaseDetailStatus.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new ListingCaseDetailResponse
                {
                    Status = ListingCaseDetailStatus.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            var mappedListingCase = _mapper.Map<ListingCaseDto>(listingCase);

            var agents = listingCase?.AgentListingCases?
                .Select(al => al.Agent)
                .Where(a => a != null)
                .ToList();
            var mappedAgents = _mapper.Map<List<AgentInfo>>(agents);

            return new ListingCaseDetailResponse
            {
                Status = ListingCaseDetailStatus.Success,
                ListingCaseInfo = mappedListingCase,
                Agents = mappedAgents
            };
        }

        public async Task<UpdateListingCaseResponse> UpdateListingCase(int id, UpdateListingCaseRequest request, ClaimsPrincipal user)
        {
            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                return new UpdateListingCaseResponse
                {
                    Result = UpdateListingCaseResult.BadRequest,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Store the old status before update
            var oldSnapshot = _mapper.Map<UpdateListingCaseRequest>(listingCase);

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new UpdateListingCaseResponse
                {
                    Result = UpdateListingCaseResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            // Update the listingCase through AutoMapper
            _mapper.Map(request, listingCase);
            var newSnapshot = _mapper.Map<UpdateListingCaseRequest>(listingCase);

            var result = await _listingCaseRepository.UpdateListingCase(listingCase);

            // If failed to update
            if (result == 0)
            {
                throw new Exception("Failed to update listing case.");
            }
            else
            {
                var change = new
                {
                    Old = oldSnapshot,
                    New = newSnapshot
                };

                var description = JsonSerializer.Serialize(change, new JsonSerializerOptions { WriteIndented = true });

                // Log listing case status change on success
                await LogListingCaseHistory(id, listingCase.Title, "Update", description, user.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            return new UpdateListingCaseResponse
            {
                Result = UpdateListingCaseResult.Success
            };
        }

        public async Task<ChangeListingCaseStatusResponse> ChangeListingCaseStatus(int id, ChangeListingCaseStatusRequest request, ClaimsPrincipal user)
        {
            var newStatus = request.Status;

            // Get the current listing case before changing status
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                return new ChangeListingCaseStatusResponse
                {
                    Result = ChangeListingCaseStatusResult.InvalidId,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new ChangeListingCaseStatusResponse
                {
                    Result = ChangeListingCaseStatusResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            var currentStatus = listingCase.ListingCaseStatus;
            var title = listingCase.Title;

            var result = await _listingCaseRepository.ChangeListingCaseStatus(id, newStatus);
            await _listingCaseRepository.SaveChangesAsync();

            // If failed to change status
            if (result == 0)
            {
                throw new Exception("Failed to change listing case status.");
            }
            else
            {
                // Log listing case status change on success
                await LogListingCaseHistory(id, title, "StatusUpdate", $"{currentStatus} -> {newStatus}", user.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            return new ChangeListingCaseStatusResponse
            {
                Result = ChangeListingCaseStatusResult.Success,
                oldStatus = currentStatus,
                newStatus = newStatus
            };
        }

        public async Task<DeleteListingCaseResponse> DeleteListingCase(int id, ClaimsPrincipal user)
        {
            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                return new DeleteListingCaseResponse
                {
                    Result = DeleteListingCaseResult.InvalidId,
                    ErrorMessage = "Unable to find the resource. Please provide a valid listing case id."
                };
            }

            // Check resource-based Authorization
            var authResult = await _authorizationService.AuthorizeAsync(user, listingCase, "ListingCaseAccess");

            if (!authResult.Succeeded)
            {
                return new DeleteListingCaseResponse
                {
                    Result = DeleteListingCaseResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            var result = await _listingCaseRepository.DeleteListingCase(id);
            await _listingCaseRepository.SaveChangesAsync();

            // If failed to delete listing case
            if (result == 0)
            {
                throw new Exception("Failed to delete listing case.");
            }
            else
            {
                // Log listing case deletion on success
                await LogListingCaseHistory(id, listingCase.Title, "Deletion", null, user.FindFirstValue(ClaimTypes.NameIdentifier));
            }

            return new DeleteListingCaseResponse
            {
                Result = DeleteListingCaseResult.Success
            };

        }

        private async Task LogListingCaseHistory(int listingCaseId, string caseTitle, string change, string? description, string userId)
        {
            var log = new CaseHistory
            {
                ListingCaseId = listingCaseId,
                CaseTitle = caseTitle,
                Change = change,
                Description = description,
                UserId = userId,
                OccurredAt = DateTime.Now,
            };

            try
            {
                await _caseHistoryRepository.Insert(log);
            }
            catch (Exception exception)
            {
                // TODO: add failure into logger...?
            }
        }

        
    }

    
}
