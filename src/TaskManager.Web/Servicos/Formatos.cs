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

    /// <summary>Nomes em pt-BR dos fusos mais usados. Os outros mostram o id com espaço no lugar do sublinhado.</summary>
    private static readonly Dictionary<string, string> NomesFuso = new()
    {
        ["Europe/Lisbon"] = "Lisboa (Portugal)",
        ["Atlantic/Madeira"] = "Madeira (Portugal)",
        ["Atlantic/Azores"] = "Açores (Portugal)",
        ["America/Sao_Paulo"] = "São Paulo / Brasília (Brasil)",
        ["America/Noronha"] = "Fernando de Noronha (Brasil)",
        ["America/Bahia"] = "Salvador (Brasil)",
        ["America/Fortaleza"] = "Fortaleza (Brasil)",
        ["America/Recife"] = "Recife (Brasil)",
        ["America/Maceio"] = "Maceió (Brasil)",
        ["America/Belem"] = "Belém (Brasil)",
        ["America/Araguaina"] = "Araguaína (Brasil)",
        ["America/Manaus"] = "Manaus (Brasil)",
        ["America/Cuiaba"] = "Cuiabá (Brasil)",
        ["America/Campo_Grande"] = "Campo Grande (Brasil)",
        ["America/Porto_Velho"] = "Porto Velho (Brasil)",
        ["America/Boa_Vista"] = "Boa Vista (Brasil)",
        ["America/Rio_Branco"] = "Rio Branco (Brasil)",
        ["Europe/London"] = "Londres (Reino Unido)",
        ["Europe/Madrid"] = "Madri (Espanha)",
        ["Europe/Paris"] = "Paris (França)",
        ["Europe/Berlin"] = "Berlim (Alemanha)",
        ["America/New_York"] = "Nova York (Estados Unidos)",
        ["Africa/Luanda"] = "Luanda (Angola)",
        ["Africa/Maputo"] = "Maputo (Moçambique)"
    };

    private const string ComAcento = "áàâãäéèêëíìîïóòôõöúùûüçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ";
    private const string SemAcento = "aaaaaeeeeiiiiooooouuuucAAAAAEEEEIIIIOOOOOUUUUC";

    public static string NomeFuso(TimeZoneInfo fuso) =>
        NomesFuso.TryGetValue(fuso.Id, out var nome) ? nome : fuso.Id.Replace('_', ' ');

    public static string Fuso(TimeZoneInfo fuso, DateTime agoraUtc)
    {
        var diferenca = fuso.GetUtcOffset(agoraUtc);
        var sinal = diferenca < TimeSpan.Zero ? "-" : "+";
        return $"(UTC{sinal}{diferenca.Duration():hh\\:mm}) {NomeFuso(fuso)}";
    }

    /// <summary>Busca do fuso pelo nome exibido ou pelo id, sem diferenciar maiúsculas, acentos, espaço e sublinhado.</summary>
    public static bool FusoCorresponde(TimeZoneInfo fuso, string busca)
    {
        if (string.IsNullOrWhiteSpace(busca))
            return true;

        var termo = ParaBusca(busca.Trim());
        return ParaBusca(NomeFuso(fuso)).Contains(termo, StringComparison.OrdinalIgnoreCase)
            || ParaBusca(fuso.Id).Contains(termo, StringComparison.OrdinalIgnoreCase);
    }

    private static string ParaBusca(string texto)
    {
        var letras = texto.Replace('_', ' ').ToCharArray();
        for (var i = 0; i < letras.Length; i++)
        {
            var posicao = ComAcento.IndexOf(letras[i]);
            if (posicao >= 0)
                letras[i] = SemAcento[posicao];
        }
        return new string(letras);
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
