using System.Globalization;

namespace TaskManager.Domain;

/// <summary>
/// Regras de cadastro, edição e reagendamento. Datas e horas chegam em UTC. O expediente é de Portugal
/// e as mensagens mostram o expediente convertido para o fuso configurado.
/// </summary>
public static class ValidadorAtividade
{
    /// <summary>Abertura do expediente, em hora de Portugal.</summary>
    public static readonly TimeOnly InicioExpediente = new(10, 0);

    /// <summary>Fechamento do expediente, em hora de Portugal.</summary>
    public static readonly TimeOnly FimExpediente = new(19, 0);

    private const string ErroEdicaoNoPassado = "Não é possível editar uma atividade para início no passado.";

    /// <summary>Validação do cadastro. Bloqueia início no passado. Retorna os erros encontrados; lista vazia significa atividade válida.</summary>
    /// <param name="agoraUtc">Data e hora atuais em UTC, obtidas do <see cref="TimeProvider"/> pelo chamador.</param>
    /// <param name="fuso">Fuso configurado, usado nas mensagens.</param>
    public static IReadOnlyList<string> Validar(Atividade atividade, DateTime agoraUtc, TimeZoneInfo fuso)
    {
        var erros = ValidarCampos(atividade, fuso);

        if (atividade.InicioUtc < agoraUtc)
            erros.Add("Não é possível criar uma atividade com início no passado.");

        return erros;
    }

    /// <summary>Validação da edição de uma atividade Única ou da "série completa". Bloqueia início no passado.</summary>
    public static IReadOnlyList<string> ValidarEdicao(Atividade atividade, DateTime agoraUtc, TimeZoneInfo fuso)
    {
        var erros = ValidarCampos(atividade, fuso);

        if (atividade.InicioUtc < agoraUtc)
            erros.Add(ErroEdicaoNoPassado);

        return erros;
    }

    /// <summary>Validação da edição de "só esta ocorrência". Bloqueia início no passado.</summary>
    public static IReadOnlyList<string> ValidarOcorrencia(DadosOcorrencia dados, DateTime agoraUtc, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(dados);
        var erros = new List<string>();
        ValidarTituloECategoria(dados.Titulo, dados.Categoria, erros);
        ValidarHorario(dados.InicioUtc, dados.TerminoUtc, fuso, erros);

        if (dados.InicioUtc < agoraUtc)
            erros.Add(ErroEdicaoNoPassado);

        return erros;
    }

    /// <summary>Validação do reagendamento: horário no expediente e nada no passado.</summary>
    public static IReadOnlyList<string> ValidarReagendamento(DateTime inicioUtc, DateTime terminoUtc, DateTime agoraUtc, TimeZoneInfo fuso)
    {
        var erros = new List<string>();
        ValidarHorario(inicioUtc, terminoUtc, fuso, erros);

        if (inicioUtc < agoraUtc)
            erros.Add("Não é possível reagendar para uma data no passado.");

        return erros;
    }

    /// <summary>Abertura e fechamento do expediente de um dia de Portugal, em UTC.</summary>
    public static (DateTime AberturaUtc, DateTime FechamentoUtc) Expediente(DateOnly diaPortugal) =>
        (FusoHorario.ParaUtc(diaPortugal, InicioExpediente, FusoHorario.Portugal),
         FusoHorario.ParaUtc(diaPortugal, FimExpediente, FusoHorario.Portugal));

    private static List<string> ValidarCampos(Atividade atividade, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(atividade);
        var erros = new List<string>();
        ValidarTituloECategoria(atividade.Titulo, atividade.Categoria, erros);
        ValidarHorario(atividade.InicioUtc, atividade.TerminoUtc, fuso, erros);
        return erros;
    }

    private static void ValidarTituloECategoria(string? titulo, string? categoria, List<string> erros)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            erros.Add("O título é obrigatório.");

        if (string.IsNullOrWhiteSpace(categoria))
            erros.Add("A categoria é obrigatória.");
        else if (!Categorias.Existe(categoria))
            erros.Add("A categoria informada não existe.");
    }

    /// <summary>Início e término dentro do expediente do mesmo dia de Portugal, e término maior que o início.</summary>
    private static void ValidarHorario(DateTime inicioUtc, DateTime terminoUtc, TimeZoneInfo fuso, List<string> erros)
    {
        ArgumentNullException.ThrowIfNull(fuso);
        var (abertura, fechamento) = Expediente(FusoHorario.Data(inicioUtc, FusoHorario.Portugal));
        var faixa = $"{HoraExibida(abertura, fuso)} e {HoraExibida(fechamento, fuso)}";

        if (inicioUtc < abertura || inicioUtc > fechamento)
            erros.Add($"O horário de início deve estar entre {faixa}.");

        if (terminoUtc < abertura || terminoUtc > fechamento)
            erros.Add($"O horário de término deve estar entre {faixa}.");

        if (terminoUtc <= inicioUtc)
            erros.Add("O horário de término deve ser maior que o de início.");
    }

    private static string HoraExibida(DateTime utc, TimeZoneInfo fuso) =>
        FusoHorario.Horario(utc, fuso).ToString("HH:mm", CultureInfo.InvariantCulture);
}
