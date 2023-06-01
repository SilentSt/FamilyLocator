using SBEU.Extensions;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyLocator.Models.Requests
{
    public class ZoneDto : IBaseDto
    {
        public string Name { get; set; }
        public List<PointF> Points { get; set; }
        public ZoneType ZoneType { get; set; }
    }

    public class IdZoneDto : ZoneDto
    {
        public int Id { get; set; }
    }
}
