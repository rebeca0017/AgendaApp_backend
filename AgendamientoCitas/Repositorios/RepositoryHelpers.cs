namespace AgendamientoCitas.Repositorios;

internal static class RepositoryHelpers
{
    public static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static TEnum ParseEnum<TEnum>(string value) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : default;
}
