using Invelop.Contacts.Api.Validation;
using Invelop.Contacts.Application.Contacts.Create;
using Invelop.Contacts.Application.Contacts.Delete;
using Invelop.Contacts.Application.Contacts.Get;
using Invelop.Contacts.Application.Contacts.GetAll;
using Invelop.Contacts.Application.Contacts.Update;

namespace Invelop.Contacts.Api;

/// <summary>
/// The HTTP surface for contacts.
/// </summary>
/// <remarks>
/// Each endpoint takes its handler as a parameter, resolved from DI by the framework. That is the
/// whole of the "CQRS wiring": no mediator, no registry, no reflection. Reading this file tells
/// you every operation the application supports and exactly which class performs it.
/// </remarks>
public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        var contacts = app.MapGroup("/api/contacts").WithTags("Contacts");

        contacts.MapGet("/", async (GetContactsHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.Handle(cancellationToken)))
            .WithName("GetContacts")
            .WithSummary("Lists all contacts, ordered by surname.");

        contacts.MapGet("/{id:guid}", async (
                Guid id,
                GetContactHandler handler,
                CancellationToken cancellationToken) =>
            {
                var contact = await handler.Handle(id, cancellationToken);
                return contact is null
                    ? Results.NotFound()
                    : Results.Ok(contact);
            })
            .WithName("GetContact")
            .WithSummary("Gets a contact by id.");

        contacts.MapPost("/", async (
                CreateContactCommand command,
                CreateContactHandler handler,
                CancellationToken cancellationToken) =>
            {
                var created = await handler.Handle(command, cancellationToken);
                return TypedResults.Created($"/api/contacts/{created.Id}", created);
            })
            .ValidateRequest<CreateContactCommand>()
            .WithName("CreateContact")
            .WithSummary("Creates a contact.");

        contacts.MapPut("/{id:guid}", async (
                Guid id,
                UpdateContactCommand command,
                UpdateContactHandler handler,
                CancellationToken cancellationToken) =>
            {
                var updated = await handler.Handle(id, command, cancellationToken);
                return updated is null
                    ? Results.NotFound()
                    : Results.Ok(updated);
            })
            .ValidateRequest<UpdateContactCommand>()
            .WithName("UpdateContact")
            .WithSummary("Updates a contact.");

        contacts.MapDelete("/{id:guid}", async (
                Guid id,
                DeleteContactHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound())
            .WithName("DeleteContact")
            .WithSummary("Deletes a contact.");

        return app;
    }
}
