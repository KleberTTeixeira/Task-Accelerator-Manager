namespace TaskManager.Domain;

/// <summary>Lista fixa de categorias (seção 1.2 do escopo, mais "Outras").</summary>
public static class Categorias
{
    public static IReadOnlyList<string> Todas { get; } =
    [
        "Atendimento e Suporte",
        "Projetos",
        "Feedbacks",
        "Acompanhamento",
        "Comunidade",
        "Customer Success",
        "Análise de Dados",
        "Estratégia e Reputação",
        "Marketing",
        "Treinamentos",
        "Webinars",
        "Mentorias",
        "Planejamento",
        "Outras"
    ];

    public static bool Existe(string? categoria) =>
        categoria is not null && Todas.Contains(categoria);
}
