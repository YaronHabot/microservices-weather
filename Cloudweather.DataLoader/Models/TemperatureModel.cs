using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudweather.DataLoader.Models
{
    internal class TemperatureModel
    {
        public int TempLowF { get; set; }
        public int TempHighF { get; set; }
        public string? ZipCode { get; set; }
        public DateTime CreatedOn { get; set; }

    }
}
