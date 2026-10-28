namespace TaskManager.Domain;

/// <summary>Filtros combináveis da agenda do dia. Conjunto vazio significa sem filtro.</summary>
public sealed class FiltroAgenda
{
    public HashSet<string> Categorias { get; } = [];
    public HashSet<Prioridade> Prioridades { get; } = [];
    public HashSet<Status> Status { get; } = [];

    public IReadOnlyList<Ocorrencia> Aplicar(IEnumerable<Ocorrencia> ocorrencias) =>
        ocorrencias
            .Where(o => Categorias.Count == 0 || Categorias.Contains(o.Categoria))
            .Where(o => Prioridades.Count == 0 || Prioridades.Contains(o.Prioridade))
            .Where(o => Status.Count == 0 || Status.Contains(o.Status))
            .ToList();
}
