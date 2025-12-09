using AutoMapper;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Services
{
    public class ListingCaseService : IListingCaseService
    {
        private readonly IListingCaseRepository _listingCaseRepository;
        private readonly ICaseHistoryRepository _caseHistoryRepository;
        private IMapper _mapper;

        public ListingCaseService(IListingCaseRepository listingCaseRepository, ICaseHistoryRepository caseHistoryRepository,
            IMapper mapper)
        {
            _listingCaseRepository = listingCaseRepository;
            _caseHistoryRepository = caseHistoryRepository;
            _mapper = mapper;
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
            await LogListingCaseHistory(listingCase.Id, request.Title, userId);

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

            // If user is not authenticated
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new GetListingCasesResponse
                {
                    Status = GetListingCasesStatus.Unauthorized,
                    ErrorMessage = "User is not authenticated."
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
                .Take(pageSize);

            var mappedCases = _mapper.Map<List<ListingCaseDto>>(paginatedCases); 

            return new GetListingCasesResponse
            {
                Status = GetListingCasesStatus.Success,
                ListingCases = mappedCases,
                TotalCount = totalCount
            };

        }








        private async Task LogListingCaseHistory(int listingCaseId, string caseTitle, string userId)
        {
            var log = new CaseHistory
            {
                ListingCaseId = listingCaseId,
                CaseTitle = caseTitle,
                Change = "Creation",
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
