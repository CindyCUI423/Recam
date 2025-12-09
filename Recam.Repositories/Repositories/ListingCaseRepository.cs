using Microsoft.EntityFrameworkCore;
using Recam.DataAccess.Data;
using Recam.Models.Entities;
using Recam.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Repositories
{
    public class ListingCaseRepository : IListingCaseRepository
    {
        private RecamDbContext _dbContext;

        public ListingCaseRepository(RecamDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddListingCase(ListingCase listingCase)
        {
            await _dbContext.ListingCases.AddAsync(listingCase);
        }

        public async Task<List<ListingCase>> GetListingCasesForPhotographyCompany(string userId)
        {
            return await _dbContext.ListingCases
                .AsNoTracking()
                .Where(l => l.UserId == userId && !l.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<ListingCase>> GetListingCasesForAgent(string userId)
        {
            return await _dbContext.ListingCases
                .AsNoTracking()
                .Where(l => l.AgentListingCases.Any(a => a.AgentId == userId) && !l.IsDeleted)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        
    }
}
