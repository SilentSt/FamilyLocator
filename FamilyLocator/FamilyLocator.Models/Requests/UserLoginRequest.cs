using SBEU.Extensions;

using System.ComponentModel.DataAnnotations;

namespace FamilyLocator.Models.Requests
{
    public class UserLoginRequest : IBaseDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
