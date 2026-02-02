using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace StackOverFlow;

public class TimerFunction
{
    private readonly ILogger _logger;

    public TimerFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TimerFunction>();
    }

    [Function("TimerFunction")]
    public async Task Run(
        [TimerTrigger("0 30 9 * * *")] TimerInfo timer)
    {
        var jsonString = await MakeStackOverflowRequest();
        
        var jsonOb = JsonConvert.DeserializeObject<dynamic>(jsonString);

        var newQuestionCount = jsonOb.items.Count;
        
        _logger.LogInformation($"Timer trigger executed at {DateTime.Now}");

        var slackResponse = await MakeSlackRequest($"You have {newQuestionCount} questions on stackoverflow");

        _logger.LogInformation($"Slack response: {slackResponse}");
    }

    private async Task<string> MakeSlackRequest(string message)
    {
        using var client = new HttpClient();

        var payload = new StringContent(
            $"{{\"text\":\"{message}\"}}",
            Encoding.UTF8,
            "application/json");

        var webhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl");

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogError("SlackWebhookUrl is not configured");
            return "Webhook missing";
        }

        var response = await client.PostAsync(webhookUrl, payload);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> MakeStackOverflowRequest()
    {
        var epochTime = (Int32)(DateTime.UtcNow.AddDays(-1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        
        HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        
        using var client = new HttpClient(handler);
        
        client.DefaultRequestHeaders.Add("User-Agent", "CSharpAzureFunction/1.0 (Contact: your@email.com)");
        
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        
        var payload = await  client.GetAsync($"https://api.stackexchange.com/2.3/search?fromdate={epochTime}&order=desc&sort=activity&intitle=rcs&site=stackoverflow");
        
        var result = await payload.Content.ReadAsStringAsync();
        
        return result;
    }
}
