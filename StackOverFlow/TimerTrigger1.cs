using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation($"Timer trigger executed at {DateTime.Now}");

        var slackResponse = await MakeSlackRequest("Hello from Azure!");

        _logger.LogInformation($"Slack response: {slackResponse}");
    }

    private static async Task<string> MakeSlackRequest(string message)
    {
        using var client = new HttpClient();

        var payload = new StringContent(
            $"{{\"text\":\"{message}\"}}",
            Encoding.UTF8,
            "application/json");

        var webhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl");

        var response = await client.PostAsync(webhookUrl, payload);
        return await response.Content.ReadAsStringAsync();
    }
}
