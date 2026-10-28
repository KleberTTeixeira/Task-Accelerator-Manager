namespace TaskManager.Domain;

/// <summary>Alteração gravada para uma única data de uma atividade: status, reagendamento, edição ou exclusão.</summary>
public class ExcecaoOcorrencia
{
    public Guid AtividadeId { get; set; }

    /// <summary>Início, em UTC, da ocorrência pela regra de recorrência; identifica a data original.</summary>
    public DateTime OriginalUtc { get; set; }

    public bool Excluida { get; set; }
    public Status? Status { get; set; }

    /// <summary>Novo início em UTC, quando reagendada ou editada.</summary>
    public DateTime? InicioUtc { get; set; }

    /// <summary>Novo término em UTC, quando reagendada ou editada.</summary>
    public DateTime? TerminoUtc { get; set; }

    /// <summary>Editada em "só esta ocorrência": usa os campos abaixo e nunca é alterada pela edição da série.</summary>
    public bool EditadaIndividualmente { get; set; }

    public string? Titulo { get; set; }
    public string? Descricao { get; set; }
    public string? Categoria { get; set; }
    public Prioridade? Prioridade { get; set; }

    // Datas em Portugal, onde as regras de recorrência são calculadas.
    internal DateOnly DataOriginal => FusoHorario.Data(OriginalUtc, FusoHorario.Portugal);
    internal DateOnly? Data => InicioUtc is { } inicio ? FusoHorario.Data(inicio, FusoHorario.Portugal) : null;
}
