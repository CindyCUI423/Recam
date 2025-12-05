using Microsoft.EntityFrameworkCore.Storage;
using Recam.DataAccess.Data;
using Recam.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly RecamDbContext _dbContext;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(RecamDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task BeginTransaction()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task Commit()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }

        public async  Task Rollback()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
    }
}
