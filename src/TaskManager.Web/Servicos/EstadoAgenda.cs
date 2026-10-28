using Blazored.LocalStorage;
using TaskManager.Domain;

namespace TaskManager.Web.Servicos;

/// <summary>
/// Agenda carregada do localStorage e gravada a cada alteração. Com o localStorage vazio, carrega a demonstração.
/// </summary>
public class EstadoAgenda(ILocalStorageService armazenamento, TimeProvider relogio)
{
    private const string ChaveAtividades = "taskmanager:atividades";
    private const string ChaveExcecoes = "taskmanager:excecoes";
    private const string ChaveFuso = "taskmanager:fuso";

    public Agenda Agenda { get; private set; } = new();

    public DateTime AgoraUtc => relogio.GetUtcNow().UtcDateTime;

    /// <summary>Data de hoje no fuso configurado.</summary>
    public DateOnly Hoje => FusoHorario.Data(AgoraUtc, Agenda.Fuso);

    public async Task CarregarAsync()
    {
        var fuso = FusoHorario.Buscar(await armazenamento.GetItemAsStringAsync(ChaveFuso));

        if (!await armazenamento.ContainKeyAsync(ChaveAtividades))
        {
            Agenda = DadosDemonstracao.Criar(AgoraUtc);
            Agenda.Fuso = fuso;
            await SalvarAsync();
            return;
        }

        var atividades = await armazenamento.GetItemAsync<List<Atividade>>(ChaveAtividades) ?? [];
        var excecoes = await armazenamento.GetItemAsync<List<ExcecaoOcorrencia>>(ChaveExcecoes) ?? [];
        Agenda = new Agenda(atividades, excecoes) { Fuso = fuso };
    }

    public async Task SalvarAsync()
    {
        await armazenamento.SetItemAsync(ChaveAtividades, Agenda.Atividades);
        await armazenamento.SetItemAsync(ChaveExcecoes, Agenda.Excecoes);
    }

    /// <summary>Apaga os dados atuais e carrega a demonstração de novo. O fuso configurado é mantido.</summary>
    public async Task RestaurarDemonstracaoAsync()
    {
        var fuso = Agenda.Fuso;
        Agenda = DadosDemonstracao.Criar(AgoraUtc);
        Agenda.Fuso = fuso;
        await SalvarAsync();
    }

    public async Task GravarFusoAsync(TimeZoneInfo fuso)
    {
        Agenda.Fuso = fuso;
        await armazenamento.SetItemAsStringAsync(ChaveFuso, fuso.Id);
    }
}
