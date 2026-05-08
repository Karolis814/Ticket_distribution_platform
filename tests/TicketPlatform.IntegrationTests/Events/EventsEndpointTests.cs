using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TicketPlatform.Shared.Events;

namespace TicketPlatform.IntegrationTests.Events;

public class EventsEndpointTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public EventsEndpointTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyListInitially()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ThenGet_RoundTripsEvent()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("api/events", new CreateEventRequest(
            Title: "Test concert",
            Description: "Integration test event",
            Location: "Kaunas",
            StartsAt: DateTime.UtcNow.AddDays(30),
            EndsAt: DateTime.UtcNow.AddDays(30).AddHours(2),
            TicketCount: 500, 
            HostId: Guid.NewGuid()
            ));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<EventDto>();
        created.Should().NotBeNull();

        var getResponse = await client.GetAsync($"api/events/{created!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<EventDto>();
        fetched!.Title.Should().Be("Test concert");
    }
}
