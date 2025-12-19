using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class SelectMediaRequest
    {
        public List<int> SelectedId { set; get; }
        public List<int> UnselectedId { set; get; }
    }
}
