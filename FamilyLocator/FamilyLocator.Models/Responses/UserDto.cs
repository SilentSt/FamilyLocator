using SBEU.Extensions;

namespace FamilyLocator.Models.Responses
{
    public class UserDto : IBaseDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FireBaseId { get; set; }
        public byte[] Image { get; set; }
    }

    public class MoreUserDto : UserDto
    {
        public int Battery { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
