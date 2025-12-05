using Recam.Models.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Repositories.Interfaces
{
    public interface IUserActivityLogRepository
    {
        Task Insert(UserActivityLog log);
    }
}
