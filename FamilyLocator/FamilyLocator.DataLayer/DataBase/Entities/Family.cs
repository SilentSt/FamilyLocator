using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    public class Family : IBaseEF
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [StringLength(128,MinimumLength = 128)]
        public string SignInCode { get; set; }
        public virtual ICollection<XIdentityUser> Users { get; set; }
        public virtual ICollection<Zone> Zones { get; set; }
    }
}
