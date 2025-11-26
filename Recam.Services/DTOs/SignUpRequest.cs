namespace Recam.Services.DTOs
{
    public class SignUpRequest
    {
        // User basic information
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // Sign up as an Agent or a PhotographyCompany
        public string RoleType { get; set; }
        public PhotographyCompanySignUpInfo? PhotographyCompanyInfo { get; set; }
        public AgentSignUpInfo? AgentInfo { get; set; }

    }

    public class PhotographyCompanySignUpInfo
    { 
        public string PhotographyCompanyName { get; set; }
    }

    public class AgentSignUpInfo
    {
        public string AgentFirstName { get; set; }
        public string AgentLastName { get; set; }
        public string AgentCompanyName { get; set; }
    }
}
