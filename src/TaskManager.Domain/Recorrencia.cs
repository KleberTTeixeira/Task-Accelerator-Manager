namespace TaskManager.Domain;

/// <summary>Regras de recorrência da seção 1.4 do escopo, calculadas sem gravar as datas futuras.</summary>
public static class Recorrencia
{
    /// <summary>Indica se a regra gera uma ocorrência na data, que é um dia de Portugal.</summary>
    public static bool OcorreEm(Atividade atividade, DateOnly data)
    {
        ArgumentNullException.ThrowIfNull(atividade);

        if (data < atividade.DataInicio)
            return false;
        if (atividade.DataFim is { } fim && data > fim)
            return false;

        var dias = data.DayNumber - atividade.DataInicio.DayNumber;
        return atividade.Frequencia switch
        {
            Frequencia.Unica => dias == 0,
            Frequencia.Diaria => data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
            Frequencia.Semanal => dias % 7 == 0,
            Frequencia.Quinzenal => dias % 14 == 0,
            Frequencia.Mensal => data.Day == Math.Min(atividade.DataInicio.Day, DateTime.DaysInMonth(data.Year, data.Month)),
            _ => throw new ArgumentOutOfRangeException(nameof(atividade))
        };
    }
}
