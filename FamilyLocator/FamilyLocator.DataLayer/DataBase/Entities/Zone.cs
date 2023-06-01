using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FamilyLocator.Models;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    public class Zone : IBaseEF
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string Name { get; set; }
        public List<PointF> Points { get; set; }
        public virtual Family Family { get; set; }
        public ZoneType ZoneType { get; set; }
    }

    
}
