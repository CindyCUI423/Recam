using AutoMapper;
using Recam.Models.Entities;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Mappers
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            // SignUpRequest -> User
            CreateMap<SignUpRequest, User>();

            // AgentSignUpInfo -> Agent
            CreateMap<AgentSignUpInfo, Agent>();

            // PhotographyCompanySignUpInfo -> PhotographyCompany
            CreateMap<PhotographyCompanySignUpInfo, PhotographyCompany>();

        }
    }
}
