using System.Text.Json;
using Microsoft.Extensions.Options;
using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Services;

public class RecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaSettings _settings;

    public RecaptchaService(HttpClient httpClient, IOptions<RecaptchaSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<bool> ValidateRecaptchaAsync(string recaptchaResponse)
    {
        if (!_settings.IsEnabled) return true;

        if (string.IsNullOrEmpty(recaptchaResponse))
            return false;

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("secret", _settings.SecretKey),
            new KeyValuePair<string, string>("response", recaptchaResponse)
        });

        var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
        if (!response.IsSuccessStatusCode)
            return false;

        var jsonString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(jsonString);
        
        return jsonDoc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean();
    }
}
