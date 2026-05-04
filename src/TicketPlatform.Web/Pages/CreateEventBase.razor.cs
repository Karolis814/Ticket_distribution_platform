using Microsoft.AspNetCore.Components;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using TicketPlatform.Core.Events;

namespace TicketPlatform.Web.Pages; // Adjust to your actual namespace

public partial class CreateEventBase : ComponentBase
{
    // This is the object your form will bind to.
    protected CreateEventFormModel Model { get; set; } = new()
    {
        StartsAt = DateTime.Now // Provide a sensible default
    };

    // You would inject whatever service you use to talk to the DB/API here
    [Inject] protected IEventService EventService { get; set; } = default!;


    protected async Task HandleValidSubmit()
    {
        // At this point, Blazor guarantees the data passes validation.
        // You would map 'Model' to your actual DB Entity and save it here.
        var newEvent = new Event
        {
            Title = Model.Title,
            Description = Model.Description,
            Location = Model.Location,
            StartsAt = Model.StartsAt,
            Capacity = Model.Capacity
        };

        await EventService.CreateAsync(newEvent);

        Console.WriteLine($"Event '{Model.Title}' is ready to save!");
    }
}

public class CreateEventFormModel
{
    [Required]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1024, ErrorMessage = "Description cannot exceed 1024 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required]
    public DateTime StartsAt { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }
}
