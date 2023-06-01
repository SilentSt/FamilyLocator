using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SBEU.Extensions;

namespace FamilyLocator.DataLayer.DataBase.Entities
{
    public class XUCoordinates : IBaseEF
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public DateTime Time { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Speed { get; set; }
        [Range(0,100)]
        public int Battery { get; set; }
        public virtual XIdentityUser User { get; set; }
    }
}
