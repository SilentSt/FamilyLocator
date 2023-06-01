
using Microsoft.AspNetCore.Identity;

using System.ComponentModel.DataAnnotations.Schema;
using FamilyLocator.Models;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    public class XIdentityUser : IdentityUser, IBaseEF
    {
        public virtual Family? Family { get; set; }
        public InFamilyRole InFamilyRole { get; set; }
        public virtual ICollection<XUCoordinates> Coordinates { get; set; }
        public virtual UserImage Image { get; set; }
        public ZoneType CurrentZoneType { get; set; }
        public string? FireBaseId { get; set; }
        public virtual ICollection<FirePushTokens> FirePushTokens { get; set; }
    }

    
}
