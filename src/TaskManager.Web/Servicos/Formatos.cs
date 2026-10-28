using System.Globalization;
using TaskManager.Domain;

namespace TaskManager.Web.Servicos;

/// <summary>Textos de data e hora em pt-BR e classes de cor das etiquetas.</summary>
public static class Formatos
{
    private static readonly string[] DiasDaSemana =
        ["domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado"];

    private static readonly string[] DiasDaSemanaCurtos =
        ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];

    private static readonly string[] Meses =
        ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro"];

    private static readonly string[] CoresCategoria =
        ["tag-blue", "tag-green", "tag-purple", "tag-orange", "tag-pink", "tag-yellow", "tag-brown", "tag-red", "tag-gray"];

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string DiaEMes(DateOnly data) => data.ToString("dd/MM", CultureInfo.InvariantCulture);

    public static string DataPorExtenso(DateOnly data) =>
        $"{DiasDaSemana[(int)data.DayOfWeek]}, {data.Day} de {Meses[data.Month - 1]} de {data.Year}";

    public static string DiaDaSemana(DateOnly data) => DiasDaSemanaCurtos[(int)data.DayOfWeek];

    public static string Hora(TimeOnly horario) => horario.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string Fuso(TimeZoneInfo fuso, DateTime agoraUtc)
    {
        var diferenca = fuso.GetUtcOffset(agoraUtc);
        var sinal = diferenca < TimeSpan.Zero ? "-" : "+";
        return $"(UTC{sinal}{diferenca.Duration():hh\\:mm}) {fuso.Id}";
    }

    public static string ClassePrioridade(Prioridade prioridade) => prioridade switch
    {
        Prioridade.Alta => "tag-red",
        Prioridade.Media => "tag-purple",
        _ => "tag-blue"
    };

    public static string ClasseStatus(Status status) => status switch
    {
        Status.Concluida => "tag-green",
        Status.EmAndamento => "tag-yellow",
        _ => "tag-gray"
    };

    public static string ClasseCategoria(string categoria)
    {
        for (var i = 0; i < Categorias.Todas.Count; i++)
        {
            if (Categorias.Todas[i] == categoria)
                return CoresCategoria[i % CoresCategoria.Length];
        }

        return "tag-gray";
    }
}
