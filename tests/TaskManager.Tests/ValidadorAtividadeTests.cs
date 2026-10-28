using TaskManager.Domain;

namespace TaskManager.Tests;

public class ValidadorAtividadeTests
{
    private static readonly DateOnly Segunda = new(2026, 11, 2);
    private static readonly DateTime Agora = Utc(Segunda, 9, 0);
    private static readonly TimeZoneInfo Portugal = FusoHorario.Portugal;

    /// <summary>Data e hora de Portugal convertidas para UTC.</summary>
    private static DateTime Utc(DateOnly data, int hora, int minuto) =>
        FusoHorario.ParaUtc(data, new TimeOnly(hora, minuto), FusoHorario.Portugal);

    private static void Horario(Atividade atividade, int hi, int mi, int ht, int mt)
    {
        atividade.InicioUtc = Utc(Segunda, hi, mi);
        atividade.TerminoUtc = Utc(Segunda, ht, mt);
    }

    private static Atividade AtividadeValida() => new()
    {
        Titulo = "Responder e-mails",
        Categoria = "Atendimento e Suporte",
        InicioUtc = Utc(Segunda, 10, 0),
        TerminoUtc = Utc(Segunda, 10, 45),
        Prioridade = Prioridade.Alta,
        Status = Status.Pendente,
        Frequencia = Frequencia.Diaria
    };

    [Fact]
    public void AtividadeValida_NaoTemErros()
    {
        Assert.Empty(ValidadorAtividade.Validar(AtividadeValida(), Agora, Portugal));
    }

    [Fact]
    public void DescricaoVazia_EhPermitida()
    {
        var atividade = AtividadeValida();
        atividade.Descricao = null;

        Assert.Empty(ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TituloVazio_EhInvalido(string titulo)
    {
        var atividade = AtividadeValida();
        atividade.Titulo = titulo;

        Assert.Contains("O título é obrigatório.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Fact]
    public void CategoriaVazia_EhInvalida()
    {
        var atividade = AtividadeValida();
        atividade.Categoria = "";

        Assert.Contains("A categoria é obrigatória.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Fact]
    public void CategoriaForaDaLista_EhInvalida()
    {
        var atividade = AtividadeValida();
        atividade.Categoria = "Financeiro";

        Assert.Contains("A categoria informada não existe.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Fact]
    public void TodasAsCategoriasDaLista_SaoValidas()
    {
        Assert.Equal(14, Categorias.Todas.Count);
        foreach (var categoria in Categorias.Todas)
        {
            var atividade = AtividadeValida();
            atividade.Categoria = categoria;

            Assert.Empty(ValidadorAtividade.Validar(atividade, Agora, Portugal));
        }
    }

    [Theory]
    [InlineData(10, 0, 19, 0)]
    [InlineData(10, 7, 10, 23)]
    [InlineData(18, 59, 19, 0)]
    public void HorariosDentroDoExpediente_SaoValidos(int hi, int mi, int ht, int mt)
    {
        var atividade = AtividadeValida();
        Horario(atividade, hi, mi, ht, mt);

        Assert.Empty(ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Theory]
    [InlineData(9, 59)]
    [InlineData(19, 1)]
    public void InicioForaDoExpediente_EhInvalido(int hora, int minuto)
    {
        var atividade = AtividadeValida();
        Horario(atividade, hora, minuto, 19, 0);

        Assert.Contains("O horário de início deve estar entre 10:00 e 19:00.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Theory]
    [InlineData(19, 1)]
    [InlineData(9, 59)]
    public void TerminoForaDoExpediente_EhInvalido(int hora, int minuto)
    {
        var atividade = AtividadeValida();
        Horario(atividade, 10, 0, hora, minuto);

        Assert.Contains("O horário de término deve estar entre 10:00 e 19:00.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Theory]
    [InlineData(11, 0)]
    [InlineData(10, 30)]
    public void TerminoIgualOuMenorQueInicio_EhInvalido(int hora, int minuto)
    {
        var atividade = AtividadeValida();
        Horario(atividade, 11, 0, hora, minuto);

        Assert.Contains("O horário de término deve ser maior que o de início.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Fact]
    public void DataNoPassado_EhInvalida()
    {
        var atividade = AtividadeValida();
        atividade.InicioUtc = Utc(new DateOnly(2026, 11, 1), 10, 0);
        atividade.TerminoUtc = Utc(new DateOnly(2026, 11, 1), 10, 45);

        Assert.Contains("Não é possível criar uma atividade com início no passado.", ValidadorAtividade.Validar(atividade, Agora, Portugal));
    }

    [Fact]
    public void HorarioJaPassadoNoDiaDeHoje_EhInvalido()
    {
        var atividade = AtividadeValida();
        var agora = Utc(Segunda, 10, 1);

        Assert.Contains("Não é possível criar uma atividade com início no passado.", ValidadorAtividade.Validar(atividade, agora, Portugal));
    }

    [Fact]
    public void InicioExatamenteAgora_EhValido()
    {
        var atividade = AtividadeValida();
        var agora = Utc(Segunda, 10, 0);

        Assert.Empty(ValidadorAtividade.Validar(atividade, agora, Portugal));
    }

    [Fact]
    public void TextosDeExibicao_EmPortugues()
    {
        Assert.Equal("Média", Prioridade.Media.Texto());
        Assert.Equal("Em andamento", Status.EmAndamento.Texto());
        Assert.Equal("Concluída", Status.Concluida.Texto());
        Assert.Equal("Única", Frequencia.Unica.Texto());
        Assert.Equal("Diária", Frequencia.Diaria.Texto());
    }
}
