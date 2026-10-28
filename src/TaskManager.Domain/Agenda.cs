namespace TaskManager.Domain;

/// <summary>
/// Atividades e exceções por data. As ocorrências são calculadas pela regra de recorrência;
/// só as alterações de uma data (status, reagendamento, edição, exclusão) são gravadas.
/// Os registros ficam em UTC; as datas recebidas e exibidas são do fuso configurado em <see cref="Fuso"/>.
/// </summary>
public class Agenda
{
    private readonly List<Atividade> _atividades;
    private readonly List<ExcecaoOcorrencia> _excecoes;

    public Agenda() : this([], []) { }

    public Agenda(IEnumerable<Atividade> atividades, IEnumerable<ExcecaoOcorrencia> excecoes)
    {
        _atividades = [.. atividades];
        _excecoes = [.. excecoes];
    }

    public IReadOnlyList<Atividade> Atividades => _atividades;
    public IReadOnlyList<ExcecaoOcorrencia> Excecoes => _excecoes;

    /// <summary>Fuso configurado pelo usuário, usado na exibição e nas datas recebidas.</summary>
    public TimeZoneInfo Fuso { get; set; } = FusoHorario.Padrao;

    public IReadOnlyList<string> Criar(Atividade atividade, DateTime agoraUtc)
    {
        var erros = ValidadorAtividade.Validar(atividade, agoraUtc, Fuso);
        if (erros.Count > 0)
            return erros;

        atividade.SerieId = atividade.Id;
        atividade.FimUtc = null;
        _atividades.Add(atividade);
        return erros;
    }

    /// <summary>Ocorrências da data do fuso configurado, ordenadas por horário.</summary>
    public IReadOnlyList<Ocorrencia> OcorrenciasDoDia(DateOnly data)
    {
        // As regras são calculadas nos dias de Portugal; no fuso configurado, a ocorrência fica no máximo um dia antes ou depois.
        var lista = new List<Ocorrencia>();
        for (var dia = data.AddDays(-1); dia <= data.AddDays(1); dia = dia.AddDays(1))
            lista.AddRange(OcorrenciasDoDiaEmPortugal(dia).Where(o => o.Data == data));

        return lista
            .OrderBy(o => o.InicioUtc)
            .ThenBy(o => o.TerminoUtc)
            .ThenBy(o => o.Titulo, StringComparer.CurrentCulture)
            .ToList();
    }

    public IReadOnlyList<Ocorrencia> AgendaDoDia(DateOnly data, FiltroAgenda filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return filtro.Aplicar(OcorrenciasDoDia(data));
    }

    /// <summary>
    /// Ocorrências Pendente ou Em andamento dos 7 dias anteriores à data selecionada,
    /// só com dias anteriores a hoje. Reagendadas contam pela nova data. Datas do fuso configurado.
    /// </summary>
    public IReadOnlyList<Ocorrencia> PendenciasAnteriores(DateOnly dataSelecionada, DateTime agoraUtc)
    {
        var hoje = FusoHorario.Data(agoraUtc, Fuso);
        var ultimoDia = dataSelecionada < hoje ? dataSelecionada.AddDays(-1) : hoje.AddDays(-1);
        var lista = new List<Ocorrencia>();

        for (var dia = dataSelecionada.AddDays(-7); dia <= ultimoDia; dia = dia.AddDays(1))
            lista.AddRange(OcorrenciasDoDia(dia).Where(o => o.Status != Status.Concluida));

        return lista;
    }

    public PainelDia Painel(DateOnly dataSelecionada, DateTime agoraUtc)
    {
        var doDia = OcorrenciasDoDia(dataSelecionada);
        return new PainelDia(
            doDia.Count,
            doDia.Count(o => o.Status == Status.Pendente),
            doDia.Count(o => o.Status == Status.EmAndamento),
            doDia.Count(o => o.Status == Status.Concluida),
            PendenciasAnteriores(dataSelecionada, agoraUtc).Count);
    }

    /// <summary>Altera o status só da ocorrência informada.</summary>
    /// <param name="originalUtc"><see cref="Ocorrencia.OriginalUtc"/> da ocorrência.</param>
    public void AlterarStatus(Guid atividadeId, DateTime originalUtc, Status status)
    {
        var (atividade, dataOriginal, excecao) = BuscarOcorrencia(atividadeId, originalUtc);
        excecao ??= NovaExcecao(atividade, dataOriginal);
        excecao.Status = status;
    }

    public void Concluir(Guid atividadeId, DateTime originalUtc) =>
        AlterarStatus(atividadeId, originalUtc, Status.Concluida);

    /// <summary>Muda só a data e o horário da ocorrência; o status e o restante da série ficam como estão.</summary>
    public IReadOnlyList<string> Reagendar(
        Guid atividadeId, DateTime originalUtc, DateTime novoInicioUtc, DateTime novoTerminoUtc, DateTime agoraUtc)
    {
        var (atividade, dataOriginal, excecao) = BuscarOcorrencia(atividadeId, originalUtc);
        var erros = ValidadorAtividade.ValidarReagendamento(novoInicioUtc, novoTerminoUtc, agoraUtc, Fuso);
        if (erros.Count > 0)
            return erros;

        excecao ??= NovaExcecao(atividade, dataOriginal);
        excecao.InicioUtc = novoInicioUtc;
        excecao.TerminoUtc = novoTerminoUtc;
        return erros;
    }

    /// <summary>Edição de "só esta ocorrência". Bloqueia data e hora no passado.</summary>
    public IReadOnlyList<string> EditarOcorrencia(Guid atividadeId, DateTime originalUtc, DadosOcorrencia dados, DateTime agoraUtc)
    {
        var (atividade, dataOriginal, excecao) = BuscarOcorrencia(atividadeId, originalUtc);
        var erros = ValidadorAtividade.ValidarOcorrencia(dados, agoraUtc, Fuso);
        if (erros.Count > 0)
            return erros;

        excecao ??= NovaExcecao(atividade, dataOriginal);
        excecao.EditadaIndividualmente = true;
        excecao.Titulo = dados.Titulo;
        excecao.Descricao = dados.Descricao;
        excecao.Categoria = dados.Categoria;
        excecao.Prioridade = dados.Prioridade;
        excecao.InicioUtc = dados.InicioUtc;
        excecao.TerminoUtc = dados.TerminoUtc;
        return erros;
    }

    /// <summary>
    /// Edita uma atividade Única ou a "série completa" de uma recorrente. A nova série começa na
    /// data de início de <paramref name="dados"/> e a antiga termina na véspera dela; as ocorrências
    /// anteriores ficam como estavam, inclusive a de hoje que já começou, e as editadas individualmente não mudam.
    /// Se a ocorrência da data de início já começou, a série nova não se repete nessa data.
    /// O status de <paramref name="dados"/> vale só para a ocorrência da data de início.
    /// Bloqueia data e hora no passado.
    /// </summary>
    public IReadOnlyList<string> EditarAtividade(Guid atividadeId, Atividade dados, DateTime agoraUtc)
    {
        var atual = BuscarAtividade(atividadeId) ?? throw new InvalidOperationException("Atividade não encontrada.");
        var erros = ValidadorAtividade.ValidarEdicao(dados, agoraUtc, Fuso);
        if (erros.Count > 0)
            return erros;

        if (atual.Frequencia == Frequencia.Unica)
        {
            CopiarCampos(dados, atual);
            _excecoes.RemoveAll(e => e.AtividadeId == atual.Id);
            return erros;
        }

        var nova = new Atividade { SerieId = atual.SerieId };
        CopiarCampos(dados, nova);
        var corte = nova.DataInicio;
        var corteJaComecou = false;

        foreach (var parte in _atividades.Where(a => a.SerieId == atual.SerieId).ToList())
        {
            // A ocorrência da data de corte que já começou é passado e fica na parte antiga.
            var jaComecou = JaComecou(parte, corte, agoraUtc);
            corteJaComecou |= jaComecou;
            var fimAntigo = jaComecou ? corte : corte.AddDays(-1);

            if (parte.DataInicio > fimAntigo)
            {
                GravarStatusDoInicio(parte);
                MoverExcecoes(parte, nova, _ => true);
                _atividades.Remove(parte);
            }
            else if (parte.DataFim is null || parte.DataFim > fimAntigo)
            {
                MoverExcecoes(parte, nova, e => e.DataOriginal > fimAntigo);
                parte.FimUtc = FusoHorario.ParaUtc(fimAntigo, TimeOnly.MinValue, FusoHorario.Portugal);
            }
        }

        _atividades.Add(nova);
        AjustarExcecoes(nova);

        if (corteJaComecou && Recorrencia.OcorreEm(nova, corte))
            NovaExcecao(nova, corte).Excluida = true;

        return erros;
    }

    /// <summary>Exclui só a ocorrência informada.</summary>
    public void ExcluirOcorrencia(Guid atividadeId, DateTime originalUtc)
    {
        var (atividade, dataOriginal, excecao) = BuscarOcorrencia(atividadeId, originalUtc);
        excecao ??= NovaExcecao(atividade, dataOriginal);
        excecao.Excluida = true;
    }

    /// <summary>Exclui a série inteira, inclusive as partes criadas pela edição da série.</summary>
    public void ExcluirSerie(Guid atividadeId)
    {
        var atividade = BuscarAtividade(atividadeId) ?? throw new InvalidOperationException("Atividade não encontrada.");
        var ids = _atividades.Where(a => a.SerieId == atividade.SerieId).Select(a => a.Id).ToHashSet();

        _atividades.RemoveAll(a => ids.Contains(a.Id));
        _excecoes.RemoveAll(e => ids.Contains(e.AtividadeId));
    }

    /// <summary>Ocorrências cuja data efetiva é o dia de Portugal informado, sem ordenação.</summary>
    private List<Ocorrencia> OcorrenciasDoDiaEmPortugal(DateOnly data)
    {
        var lista = new List<Ocorrencia>();

        foreach (var atividade in _atividades)
        {
            if (!Recorrencia.OcorreEm(atividade, data))
                continue;

            var excecao = BuscarExcecao(atividade.Id, data);
            if (excecao is null)
                lista.Add(Montar(atividade, data, null));
            else if (!excecao.Excluida && DataEfetiva(excecao) == data)
                lista.Add(Montar(atividade, data, excecao));
        }

        // Ocorrências que vieram de outra data e editadas que não caem mais na regra.
        foreach (var excecao in _excecoes)
        {
            if (excecao.Excluida || DataEfetiva(excecao) != data)
                continue;

            var atividade = BuscarAtividade(excecao.AtividadeId);
            if (atividade is null)
                continue;

            var jaIncluida = excecao.DataOriginal == data && Recorrencia.OcorreEm(atividade, data);
            if (!jaIncluida && ExcecaoValida(atividade, excecao))
                lista.Add(Montar(atividade, excecao.DataOriginal, excecao));
        }

        return lista;
    }

    /// <param name="dataOriginal">Dia de Portugal em que a ocorrência cai pela regra.</param>
    private Ocorrencia Montar(Atividade atividade, DateOnly dataOriginal, ExcecaoOcorrencia? excecao)
    {
        var editada = excecao is { EditadaIndividualmente: true } ? excecao : null;
        var originalUtc = InicioPelaRegra(atividade, dataOriginal);
        var inicioUtc = excecao?.InicioUtc ?? originalUtc;
        var terminoUtc = excecao?.TerminoUtc ?? FusoHorario.ParaUtc(dataOriginal, atividade.HorarioTermino, FusoHorario.Portugal);

        return new Ocorrencia(
            atividade.Id,
            atividade.SerieId,
            originalUtc,
            inicioUtc,
            terminoUtc,
            FusoHorario.Data(originalUtc, Fuso),
            FusoHorario.Data(inicioUtc, Fuso),
            FusoHorario.Horario(inicioUtc, Fuso),
            FusoHorario.Horario(terminoUtc, Fuso),
            editada?.Titulo ?? atividade.Titulo,
            editada is null ? atividade.Descricao : editada.Descricao,
            editada?.Categoria ?? atividade.Categoria,
            editada?.Prioridade ?? atividade.Prioridade,
            excecao?.Status ?? StatusInicial(atividade, dataOriginal),
            atividade.Frequencia,
            editada is not null);
    }

    /// <summary>Início em UTC da ocorrência da regra no dia de Portugal informado.</summary>
    private static DateTime InicioPelaRegra(Atividade atividade, DateOnly dataOriginal) =>
        FusoHorario.ParaUtc(dataOriginal, atividade.HorarioInicio, FusoHorario.Portugal);

    private static Status StatusInicial(Atividade atividade, DateOnly dataOriginal) =>
        dataOriginal == atividade.DataInicio ? atividade.Status : Status.Pendente;

    private static DateOnly DataEfetiva(ExcecaoOcorrencia excecao) => excecao.Data ?? excecao.DataOriginal;

    private static bool ExcecaoValida(Atividade atividade, ExcecaoOcorrencia excecao) =>
        excecao.EditadaIndividualmente || Recorrencia.OcorreEm(atividade, excecao.DataOriginal);

    private static bool ExcecaoVazia(ExcecaoOcorrencia excecao) =>
        !excecao.Excluida && !excecao.EditadaIndividualmente && excecao.Status is null
        && excecao.InicioUtc is null && excecao.TerminoUtc is null;

    private static void CopiarCampos(Atividade origem, Atividade destino)
    {
        destino.Titulo = origem.Titulo;
        destino.Descricao = origem.Descricao;
        destino.Categoria = origem.Categoria;
        destino.InicioUtc = origem.InicioUtc;
        destino.TerminoUtc = origem.TerminoUtc;
        destino.Prioridade = origem.Prioridade;
        destino.Status = origem.Status;
        destino.Frequencia = origem.Frequencia;
    }

    private Atividade? BuscarAtividade(Guid atividadeId) =>
        _atividades.FirstOrDefault(a => a.Id == atividadeId);

    private ExcecaoOcorrencia? BuscarExcecao(Guid atividadeId, DateOnly dataOriginal) =>
        _excecoes.FirstOrDefault(e => e.AtividadeId == atividadeId && e.DataOriginal == dataOriginal);

    private (Atividade Atividade, DateOnly DataOriginal, ExcecaoOcorrencia? Excecao) BuscarOcorrencia(Guid atividadeId, DateTime originalUtc)
    {
        var atividade = BuscarAtividade(atividadeId) ?? throw new InvalidOperationException("Atividade não encontrada.");
        var dataOriginal = FusoHorario.Data(originalUtc, FusoHorario.Portugal);
        var excecao = BuscarExcecao(atividadeId, dataOriginal);
        var existe = excecao is null
            ? Recorrencia.OcorreEm(atividade, dataOriginal)
            : !excecao.Excluida && ExcecaoValida(atividade, excecao);

        if (!existe)
            throw new InvalidOperationException("Ocorrência não encontrada.");

        return (atividade, dataOriginal, excecao);
    }

    private ExcecaoOcorrencia NovaExcecao(Atividade atividade, DateOnly dataOriginal)
    {
        var excecao = new ExcecaoOcorrencia { AtividadeId = atividade.Id, OriginalUtc = InicioPelaRegra(atividade, dataOriginal) };
        _excecoes.Add(excecao);
        return excecao;
    }

    private bool JaComecou(Atividade parte, DateOnly data, DateTime agoraUtc)
    {
        if (!Recorrencia.OcorreEm(parte, data))
            return false;

        return Montar(parte, data, BuscarExcecao(parte.Id, data)).InicioUtc < agoraUtc;
    }

    /// <summary>Grava o status da data de início como exceção, para não perdê-lo quando a parte da série for removida.</summary>
    private void GravarStatusDoInicio(Atividade parte)
    {
        if (parte.Status != Status.Pendente && BuscarExcecao(parte.Id, parte.DataInicio) is null)
            NovaExcecao(parte, parte.DataInicio).Status = parte.Status;
    }

    private void MoverExcecoes(Atividade origem, Atividade destino, Func<ExcecaoOcorrencia, bool> condicao)
    {
        foreach (var excecao in _excecoes.Where(e => e.AtividadeId == origem.Id && condicao(e)))
        {
            var statusAtual = StatusInicial(origem, excecao.DataOriginal);
            if (excecao.EditadaIndividualmente || statusAtual != Status.Pendente)
                excecao.Status ??= statusAtual;

            excecao.AtividadeId = destino.Id;
            excecao.OriginalUtc = InicioPelaRegra(destino, excecao.DataOriginal);
        }
    }

    /// <summary>
    /// Após a edição da série: descarta as exceções de datas que não existem mais na nova regra
    /// (as editadas individualmente ficam) e aplica o status do formulário à data de início.
    /// </summary>
    private void AjustarExcecoes(Atividade nova)
    {
        foreach (var excecao in _excecoes.Where(e => e.AtividadeId == nova.Id && !e.EditadaIndividualmente).ToList())
        {
            if (!Recorrencia.OcorreEm(nova, excecao.DataOriginal))
            {
                _excecoes.Remove(excecao);
                continue;
            }

            if (excecao.DataOriginal == nova.DataInicio)
                excecao.Status = null;

            if (ExcecaoVazia(excecao))
                _excecoes.Remove(excecao);
        }
    }
}
