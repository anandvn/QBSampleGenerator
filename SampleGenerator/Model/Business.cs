using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator.Model
{
    public class Business
    {
        public string QBTermsID { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AccountNumber { get; set; }
        public string TaxID { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }   
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public decimal CreditLimit { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
    }
}
