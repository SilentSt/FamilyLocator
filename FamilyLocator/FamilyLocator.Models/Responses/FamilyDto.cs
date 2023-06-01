using SBEU.Extensions;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyLocator.Models.Responses
{
    public class FamilyDto : IBaseDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SignInCode { get; set; }
        public List<MoreUserDto> Users { get; set; }
    }
}
