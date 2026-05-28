using Microsoft.AspNetCore.Components;

namespace TicketPlatform.Web.Pages;

public class PaymentProcessingBase : ComponentBase, IDisposable
{
    [Inject] protected HttpClient Http { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;

    protected string? Error { get; set; }
    private string? SessionId;
    private CancellationTokenSource? _cts;

    protected override void OnInitialized()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        SessionId = query.Get("session_id");

        if (string.IsNullOrWhiteSpace(SessionId))
        {
            Error = "Missing payment reference.";
            return;
        }

        _ = StartPollingAsync();
    }

    protected async Task StartPollingAsync()
    {
        Error = null;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(2000, token);

                var response = await Http.GetAsync(
                    $"api/payments/success?sessionId={Uri.EscapeDataString(SessionId!)}",
                    token);

                if (response.IsSuccessStatusCode)
                {
                    Nav.NavigateTo($"/checkout/success?session_id={Uri.EscapeDataString(SessionId!)}");
                    return;
                }
            }

            Error = "Payment confirmation is taking longer than expected. Please check your email or try again.";
            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            Error = "Something went wrong while confirming your payment. Please try again.";
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
