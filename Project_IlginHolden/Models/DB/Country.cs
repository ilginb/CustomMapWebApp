using System;
using System.Collections.Generic;

#nullable disable

namespace Project_IlginHolden.Models.DB
{
    public partial class Country
    {
        public string Id { get; set; }
        public string Admin { get; set; }
        public string IsoA3 { get; set; }
        public string Geometry { get; set; }
    }
}
