using System.ComponentModel.DataAnnotations;

namespace CinemaShowtimesApi.Contracts;

public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("A non-empty id is required.")
    {
    }

    public override bool IsValid(object? value) => value is Guid guid && guid != Guid.Empty;
}
