using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var configPath = "keyzy-config.json";
var configJson = File.ReadAllText(configPath);
var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);

string appId = config["app_id"];
string apiKey = config["api_key"];
string productCode = config["product_code"];
string licenseKey = args.Length > 0 ? args[0] : "DZAF-BYMG-W533-FIE2-1NRR";

Console.WriteLine("=== Keyzy Validate via GET ===");
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
client.DefaultRequestHeaders.UserAgent.ParseAdd("SystemSweep/2.0");

// Build query string
var parms = new Dictionary<string, string> {
    ["app_id"] = appId, ["api_key"] = apiKey, ["code"] = productCode,
    ["serial"] = licenseKey, ["version"] = "1.0",
    ["host_id"] = Environment.MachineName, ["device_tag"] = "Windows_10__64bits"
};
var query = string.Join("&", parms.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

string url = $"https://api.keyzy.io/v2/licenses/validate?{query}";
Console.WriteLine($"GET {url[..120]}...");
Console.WriteLine();

var response = await client.GetAsync(url);
var body = await response.Content.ReadAsStringAsync();
Console.WriteLine($"HTTP {(int)response.StatusCode}");
Console.WriteLine($"Body: '{body}'");
Console.WriteLine();

if (response.IsSuccessStatusCode)
{
    Console.WriteLine("✅ VALIDATE endpoint WORKS!");
    if (!string.IsNullOrEmpty(body))
    {
        var result = JObject.Parse(body);
        Console.WriteLine($"Response: {result}");
    }
    else
        Console.WriteLine("Empty body - license may be valid but no data returned");
}
else
{
    Console.WriteLine($"❌ Error: {body[..Math.Min(200, body.Length)]}");
}

// Also try empty body means success
if (response.IsSuccessStatusCode && string.IsNullOrEmpty(body))
{
    Console.WriteLine("\nInterpretation: Empty 200 = License IS valid!");
}
