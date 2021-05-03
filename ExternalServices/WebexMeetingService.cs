using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SallyBot.DTOs;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SallyBot.Services
{
    public class WebexMeetingService
    {
        private readonly string _webexURL;
        private readonly string _clientID;
        private readonly string _secretID;
        private readonly string _redirectURI;
        private readonly string _webexMeetingAccessToken;
        private readonly string _webexMeetingsEndPoint;
        private readonly string _webexPeopleMeEndPoint;
        private readonly string _webexAuthorizeEndPoint;
        private readonly string _webexAuthorizeScope;

        private readonly ILogger<WebexMeetingService> _logger;

        public WebexMeetingService(IConfiguration configuration, ILogger<WebexMeetingService> logger)
        {
            _webexURL = configuration["WebexURL"];
            _clientID = configuration["WebexAPIClientID"];
            _secretID = configuration["WebexAPISecretID"];
            _redirectURI = configuration["WebexAccessTokenRedirectURL"];
            _webexMeetingAccessToken = configuration["WebexAccessTokenEndPoint"];
            _webexPeopleMeEndPoint = configuration["WebexPeopleMeEndPoint"];
            _webexMeetingsEndPoint = configuration["WebexMeetingsEndPoint"];
            _webexAuthorizeEndPoint = configuration["WebexAuthorizeEndPoint"];
            _webexAuthorizeScope = configuration["WebexAuthorizeScope"];

            _logger = logger;
        }

        public async Task<WebexMeetingResponseDTO> createMeeting(string tokenWebex, JObject requestData)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + tokenWebex);

            var response = await httpClient.PostAsync(_webexURL + _webexMeetingsEndPoint, new StringContent(requestData.ToString(), System.Text.Encoding.UTF8, "application/json"));
            _logger.LogInformation(response.StatusCode.ToString());
            var contents = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<WebexMeetingResponseDTO>(contents);
        }

        public async Task<string> getOauthTOken(string code)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            string payload = String.Format("grant_type=authorization_code&client_id={0}&client_secret={1}&code={2}&redirect_uri={3}", _clientID, _secretID, code, _redirectURI);

            var response = await httpClient.PostAsync(_webexURL + _webexMeetingAccessToken, new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

            _logger.LogDebug("getOauthTOken status code: " + response.StatusCode);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                _logger.LogError("getOauthTOken error: " + await response.Content?.ReadAsStringAsync());
                return null;
            }

        }

        public async Task<string> refreshOauthTOken(string refresh_token)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            string payload = String.Format("grant_type=refresh_token&client_id={0}&client_secret={1}&refresh_token={2}", _clientID, _secretID, refresh_token);

            var response = await httpClient.PostAsync(_webexURL + _webexMeetingAccessToken, new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

            _logger.LogDebug("getOauthTOken status code: " + response.StatusCode);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                _logger.LogError("refreshOauthTOken error: " + await response.Content?.ReadAsStringAsync());
                return null;
            }

        }

        public string getAuthorizeURL()
        {
            return $"{_webexURL}{_webexAuthorizeEndPoint}" +
                $"?client_id={_clientID}" +
                $"&response_type=code" +
                $"&redirect_uri={HttpUtility.UrlEncode(_redirectURI)}" +
                $"&scope={HttpUtility.UrlEncode(_webexAuthorizeScope)}" +
                $"&state=set_state_here";
        }

        public async Task<string> getPeopleMe(string accessToken)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            var response = await httpClient.GetAsync(_webexURL + _webexPeopleMeEndPoint);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
