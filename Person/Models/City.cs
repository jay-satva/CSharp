using System;
using System.Collections.Generic;
using System.Text;

namespace Person.Models
{
    public class City
    {
        public string Name { get; set; }
        public int Population { get; set; }
        public override string ToString()
        {
            return $"{Name}: {Population}";
        }
    }
}
