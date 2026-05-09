using FluentAssertions;
using Moq;
using TicketPlatform.Core.Common;
using TicketPlatform.Core.Entities;
using TicketPlatform.Core.Services;

namespace TicketPlatform.UnitTests;

public class EventServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsEventAndReturnsIt()
    {
        var repo = new Mock<IRepository<Event>>();
        var service = new EventService(repo.Object);

        var input = new Event
        {
            Title = "Concert",
            Description = "Live show",
            Location = "Vilnius",
            StartsAt = DateTime.UtcNow.AddDays(7),
            TicketCount = 1000
        };

        var result = await service.CreateAsync(input);

        result.Should().BeSameAs(input);
        repo.Verify(r => r.AddAsync(input, It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        var expected = new List<Event> { new() { Title = "A" } };
        var repo = new Mock<IRepository<Event>>();
        repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new EventService(repo.Object);

        var result = await service.GetAllAsync();

        result.Should().BeEquivalentTo(expected);
    }
}
