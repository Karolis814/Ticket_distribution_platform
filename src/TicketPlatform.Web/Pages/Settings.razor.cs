using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
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
    [Inject] protected IPlacesClient PlacesClient { get; set; } = null!;

    protected List<PlacePredictionDto> AddressSuggestions { get; set; } = [];
    protected bool IsSearchingAddress { get; set; }

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
    protected decimal PlatformFeePercent { get; set; }

    protected ProfileFormModel ProfileModel { get; } = new();
    protected PasswordFormModel PasswordModel { get; } = new();
    protected EditContext ProfileEditContext { get; private set; } = null!;
    protected EditContext PasswordEditContext { get; private set; } = null!;

    protected override void OnInitialized()
    {
        ProfileEditContext = new EditContext(ProfileModel);
        PasswordEditContext = new EditContext(PasswordModel);
    }

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

        var feeResult = await Http.GetFromJsonAsync<FeeDto>("api/platform/fee");
        PlatformFeePercent = feeResult?.FeePercent ?? 5m;

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
                ProfileModel.FirstName = UserSettings.FirstName ?? "";
                ProfileModel.LastName = UserSettings.LastName ?? "";
                ProfileModel.PhoneNumber = UserSettings.PhoneNumber ?? "";
                ProfileModel.Company = UserSettings.Company ?? "";
                ProfileModel.Address = UserSettings.Address ?? "";
                ProfileModel.TaxCode = UserSettings.TaxCode ?? "";

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
        if (!ProfileEditContext.Validate()) return;
        try
        {
            await SettingsClient.UpdateProfileAsync(new UpdateProfileRequest(
                ProfileModel.FirstName.Trim(),
                ProfileModel.LastName.Trim(),
                ProfileModel.PhoneNumber.Trim(),
                ProfileModel.Company.Trim(),
                ProfileModel.Address.Trim(),
                ProfileModel.TaxCode.Trim()));

            Notify.Notify(NotificationSeverity.Success, "Profile saved", "Your profile has been updated.");
        }
        catch (Exception ex)
        {
            Notify.Notify(NotificationSeverity.Error, "Save failed", ex.Message);
        }
    }

    protected async Task ChangePasswordAsync()
    {
        if (!PasswordEditContext.Validate()) return;
        try
        {
            await SettingsClient.ChangePasswordAsync(new ChangePasswordRequest(
                PasswordModel.CurrentPassword,
                PasswordModel.NewPassword));
            PasswordModel.CurrentPassword = "";
            PasswordModel.NewPassword = "";
            PasswordModel.ConfirmPassword = "";
            PasswordEditContext = new EditContext(PasswordModel);
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

    protected async Task OnLoadAddressData(LoadDataArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Filter) || args.Filter.Length < 3)
        {
            AddressSuggestions.Clear();
            return;
        }

        IsSearchingAddress = true;
        StateHasChanged();
        try
        {
            AddressSuggestions = (await PlacesClient.SearchAsync(args.Filter)).ToList();
        }
        finally
        {
            IsSearchingAddress = false;
        }
    }

    protected async Task OnAddressSelected(object? value)
    {
        if (value?.ToString() is not string selected) return;

        var prediction = AddressSuggestions.FirstOrDefault(p =>
            string.Equals(p.MainText, selected, StringComparison.Ordinal));

        if (prediction is null) return;

        try
        {
            var details = await PlacesClient.GetDetailsAsync(prediction.PlaceId);
            ProfileModel.Address = details?.FormattedAddress ?? FallbackAddress(prediction);
        }
        catch
        {
            ProfileModel.Address = FallbackAddress(prediction);
        }
    }

    private static string FallbackAddress(PlacePredictionDto p) =>
        string.IsNullOrEmpty(p.SecondaryText) ? p.MainText : $"{p.MainText}, {p.SecondaryText}";

    protected sealed record StripeConnectLinkResponse(string Url);

    public class ProfileFormModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = "";

        [MaxLength(200)]
        public string Company { get; set; } = "";

        [MaxLength(500)]
        public string Address { get; set; } = "";

        [MaxLength(100)]
        public string TaxCode { get; set; } = "";
    }

    public class PasswordFormModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}

internal record FeeDto([property: System.Text.Json.Serialization.JsonPropertyName("feePercent")] decimal FeePercent);
