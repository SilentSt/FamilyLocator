using SBEU.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace FamilyLocator.Models.Requests
{
    public class CoordinatesDto : IBaseDto
    {
        public string UserId { get; set; }
        public DateTime Time { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Speed { get; set; }
        public int Battery { get; set; }
    }

    public class ListCoordinatesDto : IBaseDto
    {
        public IEnumerable<IGrouping<DateTime,CoordinatesDto>> Coordinates { get; set; }
        public double AvgSpeed
        {
            get {
                try
                {
                    return Coordinates.SelectMany(x => x).Where(x => Math.Round(x.Speed, 0) != 0).Average(x => x.Speed);
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}
