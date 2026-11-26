using FluentValidation;
using Invelop.Contacts.Application.Contacts.Create;
using Invelop.Contacts.Application.Contacts.Delete;
using Invelop.Contacts.Application.Contacts.Get;
using Invelop.Contacts.Application.Contacts.GetAll;
using Invelop.Contacts.Application.Contacts.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Invelop.Contacts.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the handlers and validators. Handlers are listed by name rather than discovered
    /// by assembly scanning: there are a handful of them, and an explicit list means a reviewer
    /// can see the entire set of operations this application supports in one glance.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateContactHandler>();
        services.AddScoped<GetContactsHandler>();
        services.AddScoped<GetContactHandler>();
        services.AddScoped<UpdateContactHandler>();
        services.AddScoped<DeleteContactHandler>();

        // Validators are scanned: unlike handlers, missing one is a silent hole rather than a
        // compile error, so discovery is the safer default here.
        services.AddValidatorsFromAssemblyContaining<CreateContactCommandValidator>();

        return services;
    }
}
