using TaskManager.Domain;

namespace TaskManager.Web.Componentes;

/// <summary>O que o formulário mostra e o que fica gravado.</summary>
public enum ModoFormulario
{
    /// <summary>Cadastro de uma atividade nova.</summary>
    Nova,

    /// <summary>Edição de uma atividade Única.</summary>
    Unica,

    /// <summary>Edição da "série completa" de uma recorrente.</summary>
    Serie,

    /// <summary>Edição de "só esta ocorrência".</summary>
    Ocorrencia,

    /// <summary>Reagendamento de uma pendência: só data e horário.</summary>
    Reagendar
}

/// <summary>Campos do formulário. Data e horários no fuso configurado.</summary>
public sealed class CamposAtividade
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Termino { get; set; }
    public Prioridade Prioridade { get; set; } = Prioridade.Media;
    public Status Status { get; set; } = Status.Pendente;
    public Frequencia Frequencia { get; set; } = Frequencia.Unica;
}
