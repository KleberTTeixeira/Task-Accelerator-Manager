namespace TaskManager.Domain;

/// <summary>
/// Ocorrência calculada de uma atividade em uma data, já com as exceções aplicadas.
/// Os campos em UTC são os registros; datas e horários já vêm convertidos para o fuso configurado.
/// </summary>
public sealed record Ocorrencia(
    Guid AtividadeId,
    Guid SerieId,
    DateTime OriginalUtc,
    DateTime InicioUtc,
    DateTime TerminoUtc,
    DateOnly DataOriginal,
    DateOnly Data,
    TimeOnly HorarioInicio,
    TimeOnly HorarioTermino,
    string Titulo,
    string? Descricao,
    string Categoria,
    Prioridade Prioridade,
    Status Status,
    Frequencia Frequencia,
    bool EditadaIndividualmente)
{
    public bool Recorrente => Frequencia != Frequencia.Unica;
}
