namespace TaskManager.Domain;

/// <summary>
/// Fusos horários. Toda data e hora é gravada em UTC; a exibição usa o fuso configurado pelo usuário.
/// O expediente e as regras de recorrência seguem o dia de Portugal. O horário de verão vem das regras do próprio fuso.
/// </summary>
public static class FusoHorario
{
    public const string IdPortugal = "Europe/Lisbon";

    public static TimeZoneInfo Portugal { get; } = TimeZoneInfo.FindSystemTimeZoneById(IdPortugal);

    /// <summary>Fuso usado antes de o usuário gravar a configuração.</summary>
    public static TimeZoneInfo Padrao => Portugal;

    /// <summary>Todos os fusos disponíveis para a configuração.</summary>
    public static IReadOnlyList<TimeZoneInfo> Todos() => TimeZoneInfo.GetSystemTimeZones();

    /// <summary>Fuso gravado pelo usuário; sem configuração ou com um id inexistente, usa o padrão.</summary>
    public static TimeZoneInfo Buscar(string? id) =>
        !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var fuso) ? fuso : Padrao;

    /// <summary>Converte uma data e hora do fuso informado para UTC.</summary>
    public static DateTime ParaUtc(DateOnly data, TimeOnly horario, TimeZoneInfo fuso) =>
        TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(horario, DateTimeKind.Unspecified), fuso);

    public static DateTime ParaLocal(DateTime utc, TimeZoneInfo fuso) => TimeZoneInfo.ConvertTimeFromUtc(utc, fuso);

    public static DateOnly Data(DateTime utc, TimeZoneInfo fuso) => DateOnly.FromDateTime(ParaLocal(utc, fuso));

    public static TimeOnly Horario(DateTime utc, TimeZoneInfo fuso) => TimeOnly.FromDateTime(ParaLocal(utc, fuso));
}
