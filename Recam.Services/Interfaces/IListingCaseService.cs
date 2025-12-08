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
    }
}
