using System.Text.Json;
using TaskManager.Domain;

namespace TaskManager.Tests;

public class FusoHorarioTests
{
    private static readonly TimeZoneInfo Portugal = FusoHorario.Portugal;
    private static readonly TimeZoneInfo Brasilia = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly TimeZoneInfo Toquio = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    private static DateOnly D(int ano, int mes, int dia) => new(ano, mes, dia);

    private static DateTime Utc(DateOnly dia, int hora, int minuto, TimeZoneInfo fuso) =>
        FusoHorario.ParaUtc(dia, new TimeOnly(hora, minuto), fuso);

    private static Atividade Nova(string titulo, Frequencia frequencia, DateOnly dia, int hora, int minuto = 0) => new()
    {
        Titulo = titulo,
        Categoria = "Projetos",
        InicioUtc = Utc(dia, hora, minuto, Portugal),
        TerminoUtc = Utc(dia, hora, minuto, Portugal).AddMinutes(45),
        Frequencia = frequencia
    };

    // ---------- Conversões ----------

    [Fact]
    public void Inverno_PortugalIgualAUtc_BrasiliaTresHorasAntes()
    {
        var utc = Utc(D(2026, 11, 3), 16, 0, Portugal);

        Assert.Equal(new DateTime(2026, 11, 3, 16, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        Assert.Equal(new TimeOnly(13, 0), FusoHorario.Horario(utc, Brasilia));
    }

    [Fact]
    public void Verao_PortugalUmaHoraAFrenteDeUtc_BrasiliaQuatroHorasAntes()
    {
        var utc = Utc(D(2027, 4, 5), 16, 0, Portugal);

        Assert.Equal(new DateTime(2027, 4, 5, 15, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(new TimeOnly(12, 0), FusoHorario.Horario(utc, Brasilia));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Fuso/Inexistente")]
    public void Buscar_SemConfiguracaoValida_UsaLisboa(string? id)
    {
        Assert.Equal(FusoHorario.IdPortugal, FusoHorario.Buscar(id).Id);
    }

    [Fact]
    public void Buscar_FusoGravado()
    {
        Assert.Equal("America/Sao_Paulo", FusoHorario.Buscar("America/Sao_Paulo").Id);
    }

    [Fact]
    public void Todos_TrazOsFusosDoSistema()
    {
        var ids = FusoHorario.Todos().Select(f => f.Id).ToList();

        Assert.Contains("Europe/Lisbon", ids);
        Assert.Contains("America/Sao_Paulo", ids);
        Assert.Contains("Asia/Tokyo", ids);
    }

    // ---------- Gravação em UTC ----------

    [Fact]
    public void Gravacao_SoTemDataEHoraEmUtc()
    {
        var atividade = Nova("Customer Success", Frequencia.Quinzenal, D(2026, 11, 3), 16);

        var json = JsonSerializer.Serialize(atividade);
        var lida = JsonSerializer.Deserialize<Atividade>(json)!;

        Assert.Contains("\"InicioUtc\":\"2026-11-03T16:00:00Z\"", json);
        Assert.DoesNotContain("DataInicio", json);
        Assert.DoesNotContain("HorarioInicio", json);
        Assert.Equal(atividade.InicioUtc, lida.InicioUtc);
        Assert.Equal(DateTimeKind.Utc, lida.InicioUtc.Kind);
    }

    // ---------- Exibição no fuso configurado ----------

    [Fact]
    public void Agenda_EmBrasilia_MostraHorarioConvertido()
    {
        var agenda = new Agenda { Fuso = Brasilia };
        Assert.Empty(agenda.Criar(Nova("Diária", Frequencia.Diaria, D(2026, 11, 2), 10), Utc(D(2026, 11, 2), 9, 0, Portugal)));

        var ocorrencia = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 11, 3)));

        Assert.Equal(new TimeOnly(7, 0), ocorrencia.HorarioInicio);
        Assert.Equal(new TimeOnly(7, 45), ocorrencia.HorarioTermino);
        Assert.Equal(new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc), ocorrencia.InicioUtc);
    }

    [Fact]
    public void Recorrencia_NaTrocaDeHorario_ContinuaAs10DePortugal()
    {
        // 25/10/2026: fim do horário de verão em Portugal.
        var agenda = new Agenda();
        Assert.Empty(agenda.Criar(Nova("Diária", Frequencia.Diaria, D(2026, 10, 23), 10), Utc(D(2026, 10, 23), 9, 0, Portugal)));

        var sexta = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 10, 23)));
        var segunda = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 10, 26)));
        Assert.Equal(new TimeOnly(10, 0), sexta.HorarioInicio);
        Assert.Equal(new TimeOnly(10, 0), segunda.HorarioInicio);
        Assert.Equal(new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc), sexta.InicioUtc);
        Assert.Equal(new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc), segunda.InicioUtc);

        agenda.Fuso = Brasilia;
        Assert.Equal(new TimeOnly(6, 0), Assert.Single(agenda.OcorrenciasDoDia(D(2026, 10, 23))).HorarioInicio);
        Assert.Equal(new TimeOnly(7, 0), Assert.Single(agenda.OcorrenciasDoDia(D(2026, 10, 26))).HorarioInicio);
    }

    [Fact]
    public void Agenda_EmToquio_OcorrenciaPassaParaODiaSeguinte()
    {
        // 16:00 de Portugal em 02/11 = 01:00 de 03/11 em Tóquio.
        var agenda = new Agenda { Fuso = Toquio };
        Assert.Empty(agenda.Criar(Nova("Única", Frequencia.Unica, D(2026, 11, 2), 16), Utc(D(2026, 11, 2), 9, 0, Portugal)));

        Assert.Empty(agenda.OcorrenciasDoDia(D(2026, 11, 2)));
        var ocorrencia = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 11, 3)));
        Assert.Equal(D(2026, 11, 3), ocorrencia.Data);
        Assert.Equal(D(2026, 11, 3), ocorrencia.DataOriginal);
        Assert.Equal(new TimeOnly(1, 0), ocorrencia.HorarioInicio);
    }

    [Fact]
    public void Pendencias_EmBrasilia_UsamODiaDoFusoConfigurado()
    {
        var agenda = new Agenda { Fuso = Brasilia };
        var atividade = Nova("Diária", Frequencia.Diaria, D(2026, 11, 2), 10);
        Assert.Empty(agenda.Criar(atividade, Utc(D(2026, 11, 2), 9, 0, Portugal)));

        // 03/11 às 23:00 em Brasília já é 04/11 em Portugal; para Brasília, hoje ainda é 03/11.
        var agora = Utc(D(2026, 11, 3), 23, 0, Brasilia);
        var pendencia = Assert.Single(agenda.PendenciasAnteriores(D(2026, 11, 3), agora));

        Assert.Equal(D(2026, 11, 2), pendencia.DataOriginal);
    }

    // ---------- Expediente convertido ----------

    [Fact]
    public void Expediente_EmBrasilia_NoInverno_07Às16()
    {
        var dia = D(2026, 11, 2);
        var atividade = Nova("Tarefa", Frequencia.Unica, dia, 10);
        atividade.InicioUtc = Utc(dia, 6, 30, Brasilia);
        atividade.TerminoUtc = Utc(dia, 16, 30, Brasilia);

        var erros = ValidadorAtividade.Validar(atividade, Utc(dia, 0, 0, Portugal), Brasilia);

        Assert.Contains("O horário de início deve estar entre 07:00 e 16:00.", erros);
        Assert.Contains("O horário de término deve estar entre 07:00 e 16:00.", erros);
    }

    [Fact]
    public void Expediente_EmBrasilia_NoVerao_06Às15()
    {
        var dia = D(2027, 4, 5);
        var atividade = Nova("Tarefa", Frequencia.Unica, dia, 10);
        atividade.InicioUtc = Utc(dia, 5, 30, Brasilia);
        atividade.TerminoUtc = Utc(dia, 15, 0, Brasilia);

        var erros = ValidadorAtividade.Validar(atividade, Utc(dia, 0, 0, Portugal), Brasilia);

        Assert.Equal(["O horário de início deve estar entre 06:00 e 15:00."], erros);
    }

    [Fact]
    public void Expediente_EmBrasilia_LimitesConvertidos_SaoValidos()
    {
        var dia = D(2026, 11, 2);
        var atividade = Nova("Tarefa", Frequencia.Unica, dia, 10);
        atividade.InicioUtc = Utc(dia, 7, 0, Brasilia);
        atividade.TerminoUtc = Utc(dia, 16, 0, Brasilia);

        Assert.Empty(ValidadorAtividade.Validar(atividade, Utc(dia, 0, 0, Portugal), Brasilia));
    }

    [Fact]
    public void Expediente_TerminoNoDiaSeguinte_EhInvalido()
    {
        var dia = D(2026, 11, 2);
        var atividade = Nova("Tarefa", Frequencia.Unica, dia, 18);
        atividade.TerminoUtc = Utc(dia.AddDays(1), 10, 30, Portugal);

        var erros = ValidadorAtividade.Validar(atividade, Utc(dia, 0, 0, Portugal), Portugal);

        Assert.Contains("O horário de término deve estar entre 10:00 e 19:00.", erros);
    }

    [Fact]
    public void Reagendar_EmBrasilia_ConverteParaUtc()
    {
        var agenda = new Agenda { Fuso = Brasilia };
        var atividade = Nova("Única", Frequencia.Unica, D(2026, 11, 2), 10);
        var agora = Utc(D(2026, 11, 2), 9, 0, Portugal);
        Assert.Empty(agenda.Criar(atividade, agora));
        var original = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 11, 2))).OriginalUtc;

        var erros = agenda.Reagendar(atividade.Id, original, Utc(D(2026, 11, 4), 15, 0, Brasilia), Utc(D(2026, 11, 4), 16, 0, Brasilia), agora);

        Assert.Empty(erros);
        var reagendada = Assert.Single(agenda.OcorrenciasDoDia(D(2026, 11, 4)));
        Assert.Equal(new TimeOnly(15, 0), reagendada.HorarioInicio);
        Assert.Equal(new DateTime(2026, 11, 4, 18, 0, 0, DateTimeKind.Utc), Assert.Single(agenda.Excecoes).InicioUtc);
    }
}
