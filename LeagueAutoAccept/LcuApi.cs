using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeagueAutoAccept.Utils;

namespace LeagueAutoAccept;

public class LcuApi
{
    private static readonly HttpClient HttpClient;

    static LcuApi()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12
        };
        HttpClient = new HttpClient(handler);
    }

    public static (string, string)? GetCredentials()
    {
        Debug.WriteLine("Attempting to get LCU credentials...");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "PROCESS WHERE name='LeagueClientUx.exe' GET commandline",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (string.IsNullOrWhiteSpace(output))
        {
            Debug.WriteLine("WMIC command returned no output. League client likely not running.");
            return null;
        }

        var appPortMatch = Regex.Match(output, "--app-port=([0-9]*)");
        var authTokenMatch = Regex.Match(output, "--remoting-auth-token=([\\w-]*)");

        if (!appPortMatch.Success || !authTokenMatch.Success)
        {
            Debug.WriteLine("Could not find port or auth token in command line arguments.");
            return null;
        }

        var port = appPortMatch.Groups[1].Value;
        var token = authTokenMatch.Groups[1].Value;
        
        Debug.WriteLine($"LCU credentials found. Port: {port}");

        return (port, token);
    }

    public static async Task<string> Request(string method, string endpoint, string port, string token, string? body = null)
    {
        Debug.WriteLine($"Making LCU request: {method} {endpoint}");
        var request = new HttpRequestMessage(new HttpMethod(method), $"https://127.0.0.1:{port}{endpoint}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{token}")));

        if (body != null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        var response = await HttpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            Debug.WriteLine($"LCU Response for {method} {endpoint}: {responseString}");
        }
        else
        {
            Debug.WriteLine($"LCU Request for {method} {endpoint} failed with status {response.StatusCode}: {responseString}");
        }

        return responseString;
    }

    public static async Task<bool> IsInMatch()
    {
        try
        {
            var credentials = await LcuUtils.GetLcuCredentials();
            if (credentials == null) return false;
            
            var response = await HttpClient.GetAsync($"{credentials.Protocol}://127.0.0.1:{credentials.Port}/lol-gameflow/v1/session");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking match: {ex.Message}");
            return false;
        }
    }

    public static async Task AcceptMatch()
    {
        try
        {
            var credentials = await LcuUtils.GetLcuCredentials();
            if (credentials == null) throw new Exception("Failed to get LCU credentials");
            
            await HttpClient.PostAsync($"{credentials.Protocol}://127.0.0.1:{credentials.Port}/lol-matchmaking/v1/ready-check/accept", null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error accepting match: {ex.Message}");
            throw;
        }
    }
} 