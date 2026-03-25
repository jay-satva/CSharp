using System;
using System.Collections.Generic;
using System.Text;

namespace Person.Models
{
    public class PersonClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public City City { get; set; }
        public override string ToString()
        {
            return $"{Name} - {Age} - {City}";
        }
    }
}
