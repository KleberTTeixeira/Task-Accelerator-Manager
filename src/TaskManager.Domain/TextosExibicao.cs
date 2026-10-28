namespace TaskManager.Domain;

/// <summary>Textos em pt-BR para exibir os enums na interface.</summary>
public static class TextosExibicao
{
    public static string Texto(this Prioridade prioridade) => prioridade switch
    {
        Prioridade.Alta => "Alta",
        Prioridade.Media => "Média",
        Prioridade.Baixa => "Baixa",
        _ => throw new ArgumentOutOfRangeException(nameof(prioridade))
    };

    public static string Texto(this Status status) => status switch
    {
        Status.Pendente => "Pendente",
        Status.EmAndamento => "Em andamento",
        Status.Concluida => "Concluída",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string Texto(this Frequencia frequencia) => frequencia switch
    {
        Frequencia.Unica => "Única",
        Frequencia.Diaria => "Diária",
        Frequencia.Semanal => "Semanal",
        Frequencia.Quinzenal => "Quinzenal",
        Frequencia.Mensal => "Mensal",
        _ => throw new ArgumentOutOfRangeException(nameof(frequencia))
    };
}
