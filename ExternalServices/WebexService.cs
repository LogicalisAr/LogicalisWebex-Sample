using SallyBot.DTOs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace SallyBot.Services
{
    public class WebexService
    {
        private readonly string _webexAccessToken;
        private readonly string _webexPeopleEndPoint;
        public WebexService(IConfiguration configuration)
        {
            _webexAccessToken = configuration["WebexAccessToken"];
            _webexPeopleEndPoint = configuration["WebexPeopleEndPoint"];
        }

        public async Task<WebexMeDTO> getUserDataAsync(string userId)
        {
            string webexPeopleData = _webexPeopleEndPoint + userId;

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _webexAccessToken);

            var response = await httpClient.GetAsync(webexPeopleData);
            var contents = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<WebexMeDTO>(contents);
        }
    }
}
