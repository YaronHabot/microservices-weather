using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudweather.DataLoader.Models
{
    internal class PrecipitationModel
    {
        public decimal AmountInches { get; set; }
        public string? WeatherType { get; set; }
        public string? ZipCode { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
