using Microsoft.AspNetCore.Mvc.Formatters;
using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class CreateMediaAssetRequest
    {
        public Models.Enums.MediaType MediaType { get; set; }
        public string MediaUrl { get; set; }
        public bool IsHero { get; set; }
    }
}
