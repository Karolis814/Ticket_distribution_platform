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

    private IJSObjectReference? _scanModule;
    private DotNetObjectReference<ScannerBase>? _selfRef;

    protected AlertStyle ResultAlertStyle => Result?.Status switch
    {
        ValidationStatus.Ok => AlertStyle.Success,
        ValidationStatus.AdmissionNotStarted or
            ValidationStatus.AdmissionEnded => AlertStyle.Warning,
        _ => AlertStyle.Danger
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _selfRef = DotNetObjectReference.Create(this);
        _scanModule = await Js.InvokeAsync<IJSObjectReference>(
            "import", "./js/scanner.js");
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
        await _scanModule!.InvokeVoidAsync("pauseScanner");
        Scanning = false;

        if (!Guid.TryParse(payload, out var ticketId))
        {
            Notify.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Invalid QR code",
                Detail = "This QR code does not contain a valid ticket key.",
                Duration = 4000
            });
            await _scanModule!.InvokeVoidAsync("resumeScanner");
            Scanning = true;
            StateHasChanged();
            return; // ← was missing
        }

        try
        {
            Result = await Http.GetFromJsonAsync<TicketValidationResultDto>(
                $"api/scan/{ticketId}");
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
            await _scanModule!.InvokeVoidAsync("resumeScanner");
            Scanning = true;
        }

        StateHasChanged();
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
