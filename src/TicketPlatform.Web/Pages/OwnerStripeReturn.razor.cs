using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.Web.Pages;

public class OwnerStripeReturnBase : ComponentBase, IDisposable
{
    [Inject] protected HttpClient Http { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Parameter] public Guid HostId { get; set; }

    protected enum PageState { Checking, Ready, IncompleteSetup, UnderReview }
    protected PageState _state = PageState.Checking;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        await PollStatusAsync();
    }

    private async Task PollStatusAsync()
    {
        _cts = new CancellationTokenSource();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        var attempts = 0;

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token) && attempts < 10)
            {
                attempts++;

                var status = await Http.GetFromJsonAsync<StripeConnectStatusDto>(
                    $"api/stripe-connect/status/{HostId}",
                    _cts.Token);

                if (status is null)
                    continue;

                if (status.Ready)
                {
                    _state = PageState.Ready;
                    break;
                }

                if (!status.DetailsSubmitted)
                {
                    _state = PageState.IncompleteSetup;
                    break;
                }

                await InvokeAsync(StateHasChanged);
            }

            if (_state == PageState.Checking)
                _state = PageState.UnderReview;
        }
        catch (OperationCanceledException) { }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
