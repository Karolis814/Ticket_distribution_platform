using System.Net.Http.Json;
using Radzen;
using TicketPlatform.Shared;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Services;

public class ForgotPasswordClient(HttpClient http) : IForgotPasswordClient
{
    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        try
        {
            ResetPasswordRequest request = new() { Email = email, Token = "", NewPassword = "" };
            var response = await http.PostAsJsonAsync("api/auth/request-password-reset", request, ct);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
               
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting password reset: {ex.Message}");
            return false;
        }
    }
}