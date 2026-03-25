using System;
using System.Collections.Generic;
using System.Text;

namespace Student.Models
{
    public class StudentClass
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string RollNo { get; set; }
        public int Semester { get; set; }
        //public DateOnly DOB { get; set; }
        public string DOB { get; set; }
        public int Age { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }

    }
}
