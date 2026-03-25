using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CRUDController : ControllerBase
    {
        private static List<Country> _countries = new List<Country>();
        private static bool _dataLoaded = false;
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!_dataLoaded)
            {
                var client = new HttpClient();

                var response = await client.GetAsync("https://countriesnow.space/api/v0.1/countries");

                if (!response.IsSuccessStatusCode)
                    return BadRequest("Failed to fetch countries");

                var json = await response.Content.ReadAsStringAsync();

                var doc = JsonDocument.Parse(json);

                var countries = doc.RootElement
                    .GetProperty("data")
                    .EnumerateArray()
                    .Take(10);

                foreach (var c in countries)
                {
                    var country = new Country
                    {
                        CountryName = c.GetProperty("country").GetString(),
                        Cities = c.GetProperty("cities").EnumerateArray()
                                  .Select(x => x.GetString())
                                  .ToList()
                    };

                    _countries.Add(country);
                }

                _dataLoaded = true;
            }

            return Ok(_countries);
        }

        [HttpPost]
        public IActionResult Post([FromBody] Country country)
        {
            _countries.Add(country);
            return Ok(country);
        }

        [HttpPut("{name}")]
        public IActionResult Put(string name, [FromBody] Country country)
        {
            var existing = _countries.FirstOrDefault(x => x.CountryName == name);

            if (existing == null)
                return NotFound();

            existing.Iso2 = country.Iso2;
            existing.Iso3 = country.Iso3;
            existing.Cities = country.Cities;

            return Ok(existing);
        }

        [HttpDelete("{name}")]
        public IActionResult Delete(string name)
        {
            var existing = _countries.FirstOrDefault(x => x.CountryName == name);

            if (existing == null)
                return NotFound();

            _countries.Remove(existing);

            return Ok($"{name} deleted");
        }
    }
}