namespace WebApplication1.Models
{
    public class Country
    {
        public string Iso2 { get; set; }
        public string Iso3 { get; set; }
        public string CountryName { get; set; }
        public List<string> Cities { get; set; }
    }
}