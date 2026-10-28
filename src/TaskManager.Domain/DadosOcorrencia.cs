namespace TaskManager.Domain;

/// <summary>Campos que podem ser alterados em "só esta ocorrência". Início e término em UTC.</summary>
public sealed record DadosOcorrencia(
    string Titulo,
    string? Descricao,
    string Categoria,
    Prioridade Prioridade,
    DateTime InicioUtc,
    DateTime TerminoUtc);
