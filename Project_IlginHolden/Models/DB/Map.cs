using System;
using System.Collections.Generic;

#nullable disable

namespace Project_IlginHolden.Models.DB
{
    public partial class Map
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string GeoJson { get; set; }
    }
}
