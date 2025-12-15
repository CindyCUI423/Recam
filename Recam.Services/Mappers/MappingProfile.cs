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

            // Agent -> AgentInfo
            CreateMap<Agent, AgentInfo>();

            // PhotographyCompany -> PhotographyCompanyInfo
            CreateMap<PhotographyCompany, PhotographyCompanyInfo>();

            // User -> UserDto (when getting user list)
            CreateMap<User, UserDto>()
                .ForMember(
                    dest => dest.Role,
                    opt => opt.MapFrom(src => 
                        src.UserRoles
                           .Select(ur => ur.Role.Name)
                           .FirstOrDefault())
                );

            // CreateListingCaseRequest -> ListingCase
            CreateMap<CreateListingCaseRequest, ListingCase>();

            // ListingCase -> ListingCaseDto (when getting listingCase list)
            CreateMap<ListingCase, ListingCaseDto>();

            // UpdateListingCaseRequest <-> ListingCase
            CreateMap<UpdateListingCaseRequest, ListingCase>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
            CreateMap<ListingCase, UpdateListingCaseRequest>();

            // CreateMediaAssetRequest -> MediaAsset
            CreateMap<CreateMediaAssetRequest, MediaAsset>();
        }
    }
}
