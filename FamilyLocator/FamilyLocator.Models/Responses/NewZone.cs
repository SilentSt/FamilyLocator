using SBEU.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyLocator.Models.Responses
{
    public class NewZone : IBaseDto
    {
        public string UserId { get; set; }
        public ZoneType ZoneType { get; set; }
    }
}
