using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class PaymentSuccessBase : ComponentBase, IDisposable
{
    [Inject] protected HttpClient Http { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected PaymentSuccessDto? Payment { get; set; }
    protected bool Loading { get; set; } = true;
    protected string? Error { get; set; }
    protected string? _sessionId;
    protected bool _polling;
    private CancellationTokenSource? _cts;

    protected string DownloadTicketsUrl =>
        $"{Http.BaseAddress}api/payments/download-tickets?sessionId={Uri.EscapeDataString(_sessionId ?? "")}";

    protected override async Task OnInitializedAsync()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        _sessionId = System.Web.HttpUtility.ParseQueryString(uri.Query).Get("session_id");

        await LoadPayment();

        if (Payment is not null && !Payment.InvoiceReady)
            _ = PollInvoiceAsync();
    }

    protected async Task LoadPayment()
    {
        Loading = true;
        Error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                Error = "Missing Stripe session id.";
                return;
            }

            var response = await Http.GetAsync(
                $"api/payments/success?sessionId={Uri.EscapeDataString(_sessionId)}");

            if (!response.IsSuccessStatusCode)
            {
                Error = await response.Content.ReadAsStringAsync();
                return;
            }

            Payment = await response.Content.ReadFromJsonAsync<PaymentSuccessDto>();
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
        _cts = new CancellationTokenSource();
        _polling = true;

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
            _polling = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
