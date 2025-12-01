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
    public class AuthRepository: IAuthRepository
    {
        private RecamDbContext _dbContext;

        public AuthRepository(RecamDbContext dbContext)
        { 
            _dbContext = dbContext;
        }

        public async Task AddAgent(Agent agent)
        {
            await _dbContext.Agents.AddAsync(agent);
        }

        public async Task AddPhotographyCompany(PhotographyCompany photographyCompany)
        {
            await _dbContext.PhotographyCompanies.AddAsync(photographyCompany);
        }

        public async Task<Agent?> GetAgentByUserId(string userId)
        {
            return await _dbContext.Agents.FindAsync(userId);
        }

        public async Task<PhotographyCompany?> GetPhotographyCompanyByUserId(string userId)
        {
            return await _dbContext.PhotographyCompanies.FindAsync(userId);
        }
    }
}
