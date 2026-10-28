namespace TaskManager.Domain;

public class Atividade
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Agrupa as partes de uma série dividida pela edição da "série completa".</summary>
    public Guid SerieId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Início da ocorrência da data de início, em UTC.</summary>
    public DateTime InicioUtc { get; set; }

    /// <summary>Término da ocorrência da data de início, em UTC.</summary>
    public DateTime TerminoUtc { get; set; }

    /// <summary>
    /// Último dia da regra, gravado como 00:00 de Portugal em UTC; preenchido quando a série é editada a partir de uma data.
    /// </summary>
    public DateTime? FimUtc { get; set; }

    public Prioridade Prioridade { get; set; } = Prioridade.Media;

    /// <summary>Status da ocorrência da data de início; as demais começam como Pendente.</summary>
    public Status Status { get; set; } = Status.Pendente;

    public Frequencia Frequencia { get; set; } = Frequencia.Unica;

    // Valores em Portugal, onde as regras de recorrência são calculadas.
    internal DateOnly DataInicio => FusoHorario.Data(InicioUtc, FusoHorario.Portugal);
    internal TimeOnly HorarioInicio => FusoHorario.Horario(InicioUtc, FusoHorario.Portugal);
    internal TimeOnly HorarioTermino => FusoHorario.Horario(TerminoUtc, FusoHorario.Portugal);
    internal DateOnly? DataFim => FimUtc is { } fim ? FusoHorario.Data(fim, FusoHorario.Portugal) : null;
}
