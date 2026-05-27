using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Web.Services;

namespace TicketPlatform.Web.Pages;

public class SettingsBase : ComponentBase, IDisposable
{
    [Inject] private IUserSettingsClient SettingsClient { get; set; } = null!;
    [Inject] private IHostPaymentsClient HostPaymentsClient { get; set; } = null!;
    [Inject] private IAuthClient AuthClient { get; set; } = null!;
    [Inject] private HttpClient Http { get; set; } = null!;
    [Inject] private NotificationService Notify { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;

    private Guid LoadedUserId { get; set; }
    private CancellationTokenSource? _cts;
    protected bool IsLoading = true;
    protected bool Connecting;
    protected bool PollingStripe;
    protected int StripeCountdown;
    protected string? StripeError;
    protected bool ConfirmDelete;

    protected UserSettingsDto? UserSettings { get; set; }
    protected StripeConnectStatusDto? StripeStatus { get; set; }

    protected string FirstName { get; set; } = "";
    protected string LastName { get; set; } = "";
    protected string PhoneNumber { get; set; } = "";
    protected string Company { get; set; } = "";
    protected string Address { get; set; } = "";
    protected string TaxCode { get; set; } = "";
    protected string CurrentPassword { get; set; } = "";
    protected string NewPassword { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        AuthState.AuthenticationStateChanged += OnAuthStateChanged;

        var authState = await AuthState.GetAuthenticationStateAsync();
        var userIdStr = authState.User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            IsLoading = false;
            return;
        }

        LoadedUserId = userId;
        await LoadSettingsAsync();
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> _)
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadSettingsAsync()
    {
        IsLoading = true;
        try
        {
            UserSettings = await SettingsClient.GetAsync();
            if (UserSettings is not null)
            {
                FirstName = UserSettings.FirstName ?? "";
                LastName = UserSettings.LastName ?? "";
                PhoneNumber = UserSettings.PhoneNumber ?? "";
                Company = UserSettings.Company ?? "";
                Address = UserSettings.Address ?? "";
                TaxCode = UserSettings.TaxCode ?? "";

                StripeStatus = await HostPaymentsClient.GetStatusAsync(LoadedUserId);

                if (StripeStatus?.Ready == true)
                {
                    var authState = await AuthState.GetAuthenticationStateAsync();
                    if (!authState.User.IsInRole("Host"))
                        await AuthClient.RefreshAsync();

                    StateHasChanged();
                }
                else if (!string.IsNullOrWhiteSpace(StripeStatus?.StripeAccountId))
                {
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                    _ = PollStripeStatusAsync(_cts.Token);
                }
            }
        }
        catch (Exception ex)
        {
            Notify.Notify(NotificationSeverity.Error, "Failed to load settings", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async Task ConnectStripeAsync()
    {
        Connecting = true;
        StripeError = null;

        try
        {
            var response = await Http.PostAsync($"api/stripe-connect/onboard/{LoadedUserId}", null);

            if (!response.IsSuccessStatusCode)
            {
                StripeError = $"Could not start Stripe onboarding: {await response.Content.ReadAsStringAsync()}";
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<StripeConnectLinkResponse>();

            if (!string.IsNullOrWhiteSpace(result?.Url))
                Nav.NavigateTo(result.Url, forceLoad: true);
        }
        catch (Exception ex)
        {
            StripeError = $"Could not start Stripe onboarding: {ex.Message}";
        }
        finally
        {
            Connecting = false;
        }
    }

    protected async Task SaveProfileAsync()
    {
        try
        {
            await SettingsClient.UpdateProfileAsync(new UpdateProfileRequest(
                FirstName.Trim(),
                LastName.Trim(),
                PhoneNumber.Trim(),
                Company.Trim(),
                Address.Trim(),
                TaxCode.Trim()));

            Notify.Notify(NotificationSeverity.Success, "Profile saved", "Your profile has been updated.");
        }
        catch (Exception ex)
        {
            Notify.Notify(NotificationSeverity.Error, "Save failed", ex.Message);
        }
    }

    protected async Task ChangePasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            Notify.Notify(NotificationSeverity.Warning, "Missing password", "Please enter both current and new password.");
            return;
        }

        try
        {
            await SettingsClient.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, NewPassword));
            CurrentPassword = "";
            NewPassword = "";
            Notify.Notify(NotificationSeverity.Success, "Password changed", "Your password has been updated.");
        }
        catch (Exception ex)
        {
            Notify.Notify(NotificationSeverity.Error, "Password change failed", ex.Message);
        }
    }

    protected async Task DeleteAccountAsync()
    {
        try
        {
            await SettingsClient.DeleteAccountAsync();
            await AuthClient.LogoutAsync();
            Nav.NavigateTo("/", forceLoad: true);
        }
        catch (Exception ex)
        {
            ConfirmDelete = false;
            Notify.Notify(NotificationSeverity.Error, "Delete failed", ex.Message);
        }
    }

    private async Task PollStripeStatusAsync(CancellationToken ct)
    {
        const int pollIntervalSeconds = 5;
        const int maxAttempts = 24; // 2 minutes max

        PollingStripe = true;
        StripeCountdown = pollIntervalSeconds;
        var attempts = 0;
        var elapsed = 0;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                elapsed++;
                StripeCountdown = pollIntervalSeconds - elapsed;
                await InvokeAsync(StateHasChanged);

                if (elapsed < pollIntervalSeconds)
                    continue;

                elapsed = 0;
                attempts++;

                var status = await HostPaymentsClient.GetStatusAsync(LoadedUserId, ct);
                if (status is not null)
                {
                    StripeStatus = status;

                    if (status.Ready)
                    {
                        await InvokeAsync(StateHasChanged);

                        var authState = await AuthState.GetAuthenticationStateAsync();
                        if (!authState.User.IsInRole("Host"))
                            await AuthClient.RefreshAsync();

                        break;
                    }
                }

                if (attempts >= maxAttempts)
                    break;

                StripeCountdown = pollIntervalSeconds;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            PollingStripe = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        AuthState.AuthenticationStateChanged -= OnAuthStateChanged;
        _cts?.Cancel();
        _cts?.Dispose();
    }

    protected sealed record StripeConnectLinkResponse(string Url);
}
