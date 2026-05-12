using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TicketPlatform.Shared.Dtos;

namespace TicketPlatform.IntegrationTests;

public class EventsEndpointTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{

}
