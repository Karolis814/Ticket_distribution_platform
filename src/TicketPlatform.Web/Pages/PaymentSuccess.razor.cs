using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class PaymentSuccessBase : ComponentBase, IDisposable
{
    [Inject] protected HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IJSRuntime Js { get; set; } = null!;

    protected PaymentSuccessDto? Payment { get; set; }
    protected bool Loading { get; set; } = true;
    protected string? Error { get; set; }
    private string? _sessionId;
    protected bool IsFreeOrder => _freeOrderId.HasValue;
    private Guid? _freeOrderId;
    protected bool Polling;
    private CancellationTokenSource? _cts;

    protected async Task DownloadTicketsAsync()
    {
        var response = await Http.GetAsync(
            $"api/payments/{Payment!.OrderId}/tickets?sessionId={Uri.EscapeDataString(_sessionId ?? "")}");

        if (!response.IsSuccessStatusCode)
            return;

        var bytes = await response.Content.ReadAsByteArrayAsync();
        await using var module = await Js.InvokeAsync<IJSObjectReference>("import", "./js/downloads.js");
        await module.InvokeVoidAsync("triggerDownload", "tickets.pdf", "application/pdf", bytes);
    }

    protected override async Task OnInitializedAsync()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        _sessionId = query.Get("session_id");

        var orderIdRaw = query.Get("order_id");
        if (Guid.TryParse(orderIdRaw, out var parsed))
            _freeOrderId = parsed;

        await LoadPayment();
    }

    protected async Task LoadPayment()
    {
        Loading = true;
        Error = null;

        try
        {
            HttpResponseMessage response;

            if (_freeOrderId.HasValue)
            {
                response = await Http.GetAsync(
                    $"api/payments/free-success?orderId={_freeOrderId.Value}");
            }
            else if (!string.IsNullOrWhiteSpace(_sessionId))
            {
                response = await Http.GetAsync(
                    $"api/payments/success?sessionId={Uri.EscapeDataString(_sessionId)}");
            }
            else
            {
                Error = "Missing payment reference.";
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                Error = await response.Content.ReadAsStringAsync();
                return;
            }

            Payment = await response.Content.ReadFromJsonAsync<PaymentSuccessDto>();

            if (Payment is not null && !Payment.InvoiceReady && !_freeOrderId.HasValue)
                _ = PollInvoiceAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    private async Task PollInvoiceAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        Polling = true;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        var attempts = 0;

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token) && attempts < 10)
            {
                attempts++;

                var response = await Http.GetAsync(
                    $"api/payments/success?sessionId={Uri.EscapeDataString(_sessionId ?? "")}",
                    _cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<PaymentSuccessDto>(
                        cancellationToken: _cts.Token);

                    if (updated is not null)
                    {
                        Payment = updated;
                        await InvokeAsync(StateHasChanged);

                        if (Payment.InvoiceReady)
                            break;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            Polling = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
