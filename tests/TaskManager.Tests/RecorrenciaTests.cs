using TaskManager.Domain;

namespace TaskManager.Tests;

public class RecorrenciaTests
{
    private static Atividade Com(Frequencia frequencia, DateOnly inicio, DateOnly? fim = null) => new()
    {
        Titulo = "Atividade",
        Categoria = "Projetos",
        InicioUtc = FusoHorario.ParaUtc(inicio, new TimeOnly(10, 0), FusoHorario.Portugal),
        TerminoUtc = FusoHorario.ParaUtc(inicio, new TimeOnly(11, 0), FusoHorario.Portugal),
        FimUtc = fim is { } dia ? FusoHorario.ParaUtc(dia, TimeOnly.MinValue, FusoHorario.Portugal) : null,
        Frequencia = frequencia
    };

    private static DateOnly D(int ano, int mes, int dia) => new(ano, mes, dia);

    [Fact]
    public void Unica_SoNaDataCadastrada()
    {
        var atividade = Com(Frequencia.Unica, D(2026, 11, 7));

        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 7)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 8)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 14)));
    }

    [Fact]
    public void Diaria_SoDeSegundaASexta()
    {
        var atividade = Com(Frequencia.Diaria, D(2026, 11, 2)); // segunda-feira

        for (var dia = 2; dia <= 6; dia++)
            Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, dia)));

        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 7))); // sábado
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 8))); // domingo
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 9)));
    }

    [Fact]
    public void Diaria_IniciadaNoSabado_ComecaNaSegunda()
    {
        var atividade = Com(Frequencia.Diaria, D(2026, 11, 7));

        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 7)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 8)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 9)));
    }

    [Fact]
    public void NadaAntesDaDataInicial()
    {
        var atividade = Com(Frequencia.Diaria, D(2026, 11, 4));

        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 3)));
    }

    [Fact]
    public void Semanal_MesmoDiaDaSemana_InclusiveFimDeSemana()
    {
        var atividade = Com(Frequencia.Semanal, D(2026, 11, 7)); // sábado

        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 7)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 14)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 12, 5)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 8)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 9)));
    }

    [Fact]
    public void Quinzenal_ACada14DiasDaDataInicial()
    {
        var atividade = Com(Frequencia.Quinzenal, D(2026, 11, 3));

        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 3)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 17)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 12, 1)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 10)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 24)));
    }

    [Fact]
    public void Mensal_MesmoDiaDoMes()
    {
        var atividade = Com(Frequencia.Mensal, D(2026, 11, 10));

        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 10)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 12, 10)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2027, 1, 10)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 12, 11)));
    }

    [Fact]
    public void Mensal_Dia31_CaiNoUltimoDiaDosMesesMenores()
    {
        var atividade = Com(Frequencia.Mensal, D(2027, 1, 31));

        Assert.True(Recorrencia.OcorreEm(atividade, D(2027, 2, 28)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2027, 2, 27)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2027, 3, 31)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2027, 3, 30)));
        Assert.True(Recorrencia.OcorreEm(atividade, D(2027, 4, 30)));
    }

    [Theory]
    [InlineData(29, 2027, 28)]
    [InlineData(30, 2027, 28)]
    [InlineData(29, 2028, 29)]
    [InlineData(30, 2028, 29)]
    public void Mensal_Dias29e30_EmFevereiro(int diaInicial, int anoFevereiro, int diaEsperado)
    {
        var atividade = Com(Frequencia.Mensal, D(2026, 12, diaInicial));

        Assert.True(Recorrencia.OcorreEm(atividade, D(anoFevereiro, 2, diaEsperado)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(anoFevereiro, 2, diaEsperado - 1)));
    }

    [Fact]
    public void RespeitaADataFim()
    {
        var atividade = Com(Frequencia.Diaria, D(2026, 11, 2), D(2026, 11, 4));

        Assert.True(Recorrencia.OcorreEm(atividade, D(2026, 11, 4)));
        Assert.False(Recorrencia.OcorreEm(atividade, D(2026, 11, 5)));
    }
}
