using System;
using System.Collections.Generic;
using System.Text;

namespace Student.Models
{
    public class Awards
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public int Rank { get; set; }
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
    }
}
