namespace TaskManager.Domain;

/// <summary>Indicadores do painel (seção 1.5 do escopo). Não sofrem efeito dos filtros.</summary>
public sealed record PainelDia(int Total, int Pendentes, int EmAndamento, int Concluidas, int PendenciasAnteriores);
