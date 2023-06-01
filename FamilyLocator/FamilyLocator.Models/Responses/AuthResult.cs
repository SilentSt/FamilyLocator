using SBEU.Extensions;

namespace FamilyLocator.Models.Responses
{
    public class AuthResult : IBaseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
    }
}
