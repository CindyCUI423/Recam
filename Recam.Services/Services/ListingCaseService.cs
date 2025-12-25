using AutoMapper;
using DnsClient.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly ILogger<ListingCaseService> _logger;

        public ListingCaseService(IListingCaseRepository listingCaseRepository, ICaseHistoryRepository caseHistoryRepository,
            IMapper mapper, IAuthorizationService authorizationService, ILogger<ListingCaseService> logger)
        {
            _listingCaseRepository = listingCaseRepository;
            _caseHistoryRepository = caseHistoryRepository;
            _mapper = mapper;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<int> CreateListingCase(CreateListingCaseRequest request, string userId)
        {
            _logger.LogInformation(
                  "Start creating the listing case. UserId={UserId}",
                  userId);

            var listingCase = _mapper.Map<ListingCase>(request);

            listingCase.CreatedAt = DateTime.UtcNow;
            listingCase.IsDeleted = false;
            listingCase.SaleCategory = SaleCategory.ForSale;
            listingCase.ListingCaseStatus = ListingCaseStatus.Created;
            listingCase.UserId = userId;

            _logger.LogInformation(
                "Strat creating listing case. UserId={UserId}",
                userId);

            await _listingCaseRepository.AddListingCase(listingCase);
            await _listingCaseRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Adding the listing case to db. ListingCaseId={ListingCaseId}",
                listingCase.Id);

            // Log listing case creation
            await LogListingCaseHistory(listingCase.Id, request.Title, "Creation", null, userId);

            _logger.LogInformation(
                "CreateListingCase completed. ListingCaseId={ListingCaseId}",
                listingCase.Id);

            return listingCase.Id;
        }

        public async Task<GetListingCasesResponse> GetListingCasesByUser(int pageNumber, int pageSize, string userId, string role)
        {
            _logger.LogInformation(
                  "Start retrieving the listing case. UserId={UserId}, UserRole={UserRole}",
                  userId,
                  role);

            // If pageNumber or pageSize is invalid
            if (pageNumber < 1 || pageSize < 1)
            {
                _logger.LogWarning(
                    "Invalid pageNumber or pageSize when retrieving listing cases. PageNumber={PageNumber}, PageSize={PageSize}",
                    pageNumber,
                    pageSize);
                
                return new GetListingCasesResponse
                {
                    Status = GetListingCasesStatus.BadRequest,
                    ErrorMessage = "pageNumber and pageSize must be greater than 0."
                };
            }

            List<ListingCase> cases;

            if (role == "PhotographyCompany")
            {
                _logger.LogInformation(
                    "Retrieving the listing cases for photography company. UserId={UserId}, UserRole={UserRole}",
                    userId,
                    role);

                cases = await _listingCaseRepository.GetListingCasesForPhotographyCompany(userId);
            }
            else if (role == "Agent")
            {
                _logger.LogInformation(
                    "Retrieving the listing cases for agent. UserId={UserId}, UserRole={UserRole}",
                    userId,
                    role);

                cases = await _listingCaseRepository.GetListingCasesForAgent(userId);
            }
            // If role is invalid
            else
            {
                _logger.LogWarning(
                    "Authorization failed when retrieving listing cases. UserId={UserId}, UserRole={UserRole}",
                    userId,
                    role);

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

            var mappedCasesIds = mappedCases.Select(c => c.Id).ToList();

            _logger.LogInformation(
                    "GetListingCasesByUser completed. TotalListingCaseCount={TotalListingCaseCount}, PageNumber={PageNumber}, PageSize={PageSize}, ListingCaseIds={ListingCaseIds}",
                    totalCount,
                    pageNumber,
                    pageSize,
                    mappedCasesIds);

            return new GetListingCasesResponse
            {
                Status = GetListingCasesStatus.Success,
                ListingCases = mappedCases,
                TotalCount = totalCount
            };

        }

        public async Task<ListingCaseDetailResponse> GetListingCaseById(int id, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Strat retrieving listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                id,
                userId);

            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when retrieving the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogWarning(
                  "Authorization failed when retrieving the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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

            _logger.LogInformation(
                "GetListingCaseById completed. ListingCaseId={ListingCaseId}",
                id);

            return new ListingCaseDetailResponse
            {
                Status = ListingCaseDetailStatus.Success,
                ListingCaseInfo = mappedListingCase,
                Agents = mappedAgents
            };
        }

        public async Task<UpdateListingCaseResponse> UpdateListingCase(int id, UpdateListingCaseRequest request, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                  "Start updating the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when updating the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogWarning(
                  "Authorization failed when updating the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

                return new UpdateListingCaseResponse
                {
                    Result = UpdateListingCaseResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            // Update the listingCase through AutoMapper
            _mapper.Map(request, listingCase);
            var newSnapshot = _mapper.Map<UpdateListingCaseRequest>(listingCase);

            _logger.LogInformation(
                "Updating the listing case. ListingCaseId={ListingCaseId}",
                id);

            var result = await _listingCaseRepository.UpdateListingCase(listingCase);

            // If failed to update
            if (result == 0)
            {
                _logger.LogError(
                    "Failed to update the listing case from db. ListingCaseId={ListingCaseId}",
                    id);

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
                await LogListingCaseHistory(id, listingCase.Title, "Update", description, userId);
            }

            _logger.LogInformation(
                "UpdateListingCase completed. ListingCaseId={ListingCaseId}",
                id);

            return new UpdateListingCaseResponse
            {
                Result = UpdateListingCaseResult.Success
            };
        }

        public async Task<ChangeListingCaseStatusResponse> ChangeListingCaseStatus(int id, ChangeListingCaseStatusRequest request, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                  "Start changing the listing case status. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

            var newStatus = request.Status;

            // Get the current listing case before changing status
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                _logger.LogWarning(
                  "Listing case not found when changing the listing case status. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogWarning(
                  "Authorization failed when changing the listing case status. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

                return new ChangeListingCaseStatusResponse
                {
                    Result = ChangeListingCaseStatusResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            var currentStatus = listingCase.ListingCaseStatus;
            var title = listingCase.Title;

            _logger.LogInformation(
                "Change the listing case status. ListingCaseId={ListingCaseId}",
                id);

            var result = await _listingCaseRepository.ChangeListingCaseStatus(id, newStatus);
            await _listingCaseRepository.SaveChangesAsync();

            // If failed to change status
            if (result == 0)
            {
                _logger.LogError(
                    "Failed to change the listing case status from db. ListingCaseId={ListingCaseId}",
                    id);

                throw new Exception("Failed to change listing case status.");
            }
            else
            {
                // Log listing case status change on success
                await LogListingCaseHistory(id, title, "StatusUpdate", $"{currentStatus} -> {newStatus}", userId);
            }

            _logger.LogInformation(
                "ChangeListingCaseStatus completed. ListingCaseId={ListingCaseId}, OldStatus={OldStatus}, NewStatus={NewStatus}",
                id,
                currentStatus,
                newStatus);

            return new ChangeListingCaseStatusResponse
            {
                Result = ChangeListingCaseStatusResult.Success,
                oldStatus = currentStatus,
                newStatus = newStatus
            };
        }

        public async Task<DeleteListingCaseResponse> DeleteListingCase(int id, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                  "Start deleting the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

            // Get the current listing case
            var listingCase = await _listingCaseRepository.GetListingCaseById(id);

            if (listingCase == null)
            {
                _logger.LogError(
                  "Failed to delete the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

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
                _logger.LogError(
                  "Authorization failed when deleting the listing case. ListingCaseId={ListingCaseId}, UserId={UserId}",
                  id,
                  userId);

                return new DeleteListingCaseResponse
                {
                    Result = DeleteListingCaseResult.Forbidden,
                    ErrorMessage = "You are not allowed to access this listing case."
                };
            }

            _logger.LogInformation(
                "Deleting the listing case. ListingCaseId={ListingCaseId}",
                id);

            var result = await _listingCaseRepository.DeleteListingCase(id);
            await _listingCaseRepository.SaveChangesAsync();

            // If failed to delete listing case
            if (result == 0)
            {
                _logger.LogError(
                    "Failed to deleting the listing case from db. ListingCaseId={ListingCaseId}",
                    id);

                throw new Exception("Failed to delete listing case.");
            }
            else
            {
                // Log listing case deletion on success
                await LogListingCaseHistory(id, listingCase.Title, "Deletion", null, userId);
            }

            _logger.LogInformation(
                "DeleteListingCase completed. ListingCaseId={ListingCaseId}",
                id);

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
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log listing case history. " +
                    "ListingCaseId={ListingCaseId}, Change={Change}, UserId={UserId}",
                    listingCaseId,
                    change,
                    userId
                );
            }
        }

        
    }

    
}
