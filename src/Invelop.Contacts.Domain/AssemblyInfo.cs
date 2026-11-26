using System.Runtime.CompilerServices;

// Infrastructure needs the `FromStorage` factories on the value objects so EF Core can
// rehydrate rows without re-running validation. Those factories are deliberately
// `internal`: Application and Api must go through the validating `Create` factories.
// See ContactValidator for why rehydration skips validation rather than throwing on read.
[assembly: InternalsVisibleTo("Invelop.Contacts.Infrastructure")]
