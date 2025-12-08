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

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
