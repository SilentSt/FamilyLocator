using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    public class FirePushTokens : IBaseEF
    {
        [Key]
        public string Token { get; set; }
        public XIdentityUser User { get; set; }
    }
}
