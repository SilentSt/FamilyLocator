using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    [Table("AspNetUsers")]
    public class UserImage : IBaseEF
    {
        public string Id { get; set; }
        public byte[] Image { get; set; }
    }
}
