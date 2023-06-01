using SBEU.Extensions;

using System.ComponentModel.DataAnnotations;

namespace FamilyLocator.Models.Requests
{
    public class TokenRequest : IBaseDto
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}
