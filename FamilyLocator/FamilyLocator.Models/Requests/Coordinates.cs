using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SBEU.Extensions;

namespace FamilyLocator.Models.Requests
{
    public class Coordinates : IBaseDto
    {
        public string UserId { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Speed { get; set; }
        public int Battery { get; set; }
    }
}
