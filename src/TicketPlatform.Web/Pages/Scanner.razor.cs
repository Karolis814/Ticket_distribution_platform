using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using TicketPlatform.Shared.Dtos;
using TicketPlatform.Shared.Enums;

namespace TicketPlatform.Web.Pages;

public class ScannerBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected HttpClient Http { get; set; } = null!;
    [Inject] protected IJSRuntime Js { get; set; } = null!;
    [Inject] protected NotificationService Notify { get; set; } = null!;

    protected bool CameraActive;
    protected bool Scanning;
    protected TicketValidationResultDto? Result;
    private bool _processing;

    private IJSObjectReference? _scanModule;
    private DotNetObjectReference<ScannerBase>? _selfRef;
    private string? _timezone;

    protected AlertStyle ResultAlertStyle => Result?.Status switch
    {
        ValidationStatus.Ok => AlertStyle.Success,
        ValidationStatus.AdmissionNotStarted or
            ValidationStatus.AdmissionEnded => AlertStyle.Warning,
        _ => AlertStyle.Danger
    };

    protected string StatusBorderColor => Result?.Status switch
    {
        ValidationStatus.Ok => "var(--rz-success)",
        ValidationStatus.AdmissionNotStarted or
            ValidationStatus.AdmissionEnded => "var(--rz-warning)",
        _ => "var(--rz-danger)"
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _selfRef = DotNetObjectReference.Create(this);
        _scanModule = await Js.InvokeAsync<IJSObjectReference>(
            "import", "./js/scanner.js");
        _timezone = await _scanModule.InvokeAsync<string>("getTimezone");
    }

    protected async Task StartCamera()
    {
        if (_scanModule is null) return;

        CameraActive = true;
        Scanning = true;
        Result = null;

        await _scanModule.InvokeVoidAsync("startScanner", "qr-video", _selfRef);
        StateHasChanged();
    }

    protected async Task StopCamera()
    {
        if (_scanModule is null) return;

        await _scanModule.InvokeVoidAsync("stopScanner");
        CameraActive = false;
        Scanning = false;
        StateHasChanged();
    }

    protected async Task ResetScan()
    {
        if (_scanModule is null) return;
        Result = null;
        Scanning = true;
        await _scanModule.InvokeVoidAsync("resumeScanner");
        StateHasChanged();
    }


    [JSInvokable]
    public async Task OnQrDecoded(string payload)
    {
        if (_processing) return;
        _processing = true;

        Scanning = false;
        await InvokeAsync(StateHasChanged);

        if (!Guid.TryParse(payload, out var ticketId))
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Invalid QR code",
                Detail = "This QR code does not contain a valid ticket key.",
                Duration = 4000
            });
            Scanning = true;
            _processing = false;
            await _scanModule!.InvokeVoidAsync("resumeScanner");
            await InvokeAsync(StateHasChanged);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/scan/{ticketId}");
            if (_timezone is not null)
                request.Headers.TryAddWithoutValidation("X-Timezone", _timezone);
            var response = await Http.SendAsync(request);
            Result = await response.Content.ReadFromJsonAsync<TicketValidationResultDto>();
        }
        catch (Exception ex)
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Validation error",
                Detail = ex.Message,
                Duration = 5000
            });
            Scanning = true;
            await _scanModule!.InvokeVoidAsync("resumeScanner");
        }

        _processing = false;
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_scanModule is not null)
        {
            await _scanModule.InvokeVoidAsync("stopScanner");
            await _scanModule.DisposeAsync();
        }

        _selfRef?.Dispose();
    }
}
