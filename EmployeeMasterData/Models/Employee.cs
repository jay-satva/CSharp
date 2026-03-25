using System;
using System.Collections.Generic;
using System.Text;

namespace EmployeeMasterData.Models
{
    public class Employee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateOnly DOB { get; set; }
        public char Gender { get; set; }
        public string Designation { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int Pincode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public DateOnly DatOfJoining { get; set; }  
        public int TotExperience { get; set; }
        public string Remarks { get; set; }
        public Department Department { get; set; }
        public int Salary { get; set; }
    }
}
