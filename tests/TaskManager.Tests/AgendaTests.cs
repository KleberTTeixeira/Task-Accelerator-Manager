using TaskManager.Domain;

namespace TaskManager.Tests;

public class AgendaTests
{
    // 02/11/2026 é segunda-feira.
    // Em novembro, Portugal está em UTC+0.
    private static readonly DateOnly Segunda = new(2026, 11, 2);
    private static readonly DateTime AgoraNaSegunda = Agora(Segunda);

    private static DateOnly Dia(int dia) => new(2026, 11, dia);

    /// <summary>Data e hora de Portugal convertidas para UTC.</summary>
    private static DateTime Utc(DateOnly dia, int hora, int minuto = 0) =>
        FusoHorario.ParaUtc(dia, new TimeOnly(hora, minuto), FusoHorario.Portugal);

    /// <summary>Relógio às 9h do dia, antes do expediente.</summary>
    private static DateTime Agora(DateOnly dia) => Utc(dia, 9);

    /// <summary>Início original, em UTC, da ocorrência do dia.</summary>
    private static DateTime Original(DateOnly dia, int hora = 10) => Utc(dia, hora);

    private static Atividade Nova(
        string titulo, Frequencia frequencia, DateOnly inicio, int hora = 10, int minuto = 0,
        string categoria = "Projetos", Prioridade prioridade = Prioridade.Media, Status status = Status.Pendente) => new()
    {
        Titulo = titulo,
        Categoria = categoria,
        InicioUtc = Utc(inicio, hora, minuto),
        TerminoUtc = Utc(inicio, hora, minuto).AddMinutes(45),
        Prioridade = prioridade,
        Status = status,
        Frequencia = frequencia
    };

    private static (Agenda Agenda, Atividade Atividade) AgendaCom(Atividade atividade)
    {
        var agenda = new Agenda();
        Assert.Empty(agenda.Criar(atividade, AgoraNaSegunda));
        return (agenda, atividade);
    }

    private static Ocorrencia Unica(Agenda agenda, DateOnly data) => Assert.Single(agenda.OcorrenciasDoDia(data));

    // ---------- Cadastro ----------

    [Fact]
    public void Criar_ComErro_NaoAdiciona()
    {
        var agenda = new Agenda();
        var erros = agenda.Criar(Nova("", Frequencia.Unica, Segunda), AgoraNaSegunda);

        Assert.Contains("O título é obrigatório.", erros);
        Assert.Empty(agenda.Atividades);
    }

    [Fact]
    public void Criar_NoPassado_EhBloqueado()
    {
        var agenda = new Agenda();
        var erros = agenda.Criar(Nova("Tarefa", Frequencia.Unica, Dia(1)), AgoraNaSegunda);

        Assert.Contains("Não é possível criar uma atividade com início no passado.", erros);
    }

    // ---------- Agenda do dia ----------

    [Fact]
    public void OcorrenciasDoDia_OrdenadasPorHorario_ComRecorrentes()
    {
        var agenda = new Agenda();
        agenda.Criar(Nova("Tarde", Frequencia.Unica, Segunda, 15), AgoraNaSegunda);
        agenda.Criar(Nova("Diária", Frequencia.Diaria, Segunda, 10), AgoraNaSegunda);
        agenda.Criar(Nova("Semanal", Frequencia.Semanal, Segunda, 12, 15), AgoraNaSegunda);

        var titulos = agenda.OcorrenciasDoDia(Segunda).Select(o => o.Titulo);
        Assert.Equal(["Diária", "Semanal", "Tarde"], titulos);

        var terca = Assert.Single(agenda.OcorrenciasDoDia(Dia(3)));
        Assert.Equal("Diária", terca.Titulo);
        Assert.True(terca.Recorrente);
    }

    [Fact]
    public void HorariosSobrepostos_SaoPermitidos()
    {
        var agenda = new Agenda();
        agenda.Criar(Nova("A", Frequencia.Unica, Segunda, 10), AgoraNaSegunda);
        agenda.Criar(Nova("B", Frequencia.Unica, Segunda, 10, 30), AgoraNaSegunda);

        Assert.Equal(2, agenda.OcorrenciasDoDia(Segunda).Count);
    }

    // ---------- Status por ocorrência ----------

    [Fact]
    public void StatusDoFormulario_ValeSoParaADataDeInicio()
    {
        var (agenda, _) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda, status: Status.EmAndamento));

        Assert.Equal(Status.EmAndamento, Unica(agenda, Segunda).Status);
        Assert.Equal(Status.Pendente, Unica(agenda, Dia(3)).Status);
    }

    [Fact]
    public void ConcluirUmaOcorrencia_NaoAlteraAsOutras()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));

        agenda.Concluir(atividade.Id, Original(Dia(3)));

        Assert.Equal(Status.Concluida, Unica(agenda, Dia(3)).Status);
        Assert.Equal(Status.Pendente, Unica(agenda, Segunda).Status);
        Assert.Equal(Status.Pendente, Unica(agenda, Dia(4)).Status);
    }

    [Fact]
    public void AlterarStatus_DeUmaData()
    {
        var (agenda, atividade) = AgendaCom(Nova("Semanal", Frequencia.Semanal, Segunda));

        agenda.AlterarStatus(atividade.Id, Original(Dia(9)), Status.EmAndamento);

        Assert.Equal(Status.EmAndamento, Unica(agenda, Dia(9)).Status);
        Assert.Equal(Status.Pendente, Unica(agenda, Dia(16)).Status);
    }

    [Fact]
    public void AlterarStatus_DeDataSemOcorrencia_Falha()
    {
        var (agenda, atividade) = AgendaCom(Nova("Semanal", Frequencia.Semanal, Segunda));

        Assert.Throws<InvalidOperationException>(() => agenda.AlterarStatus(atividade.Id, Original(Dia(3)), Status.Concluida));
    }

    // ---------- Reagendamento ----------

    [Fact]
    public void Reagendar_MudaDataEHorario_MantemStatus_NaoAlteraASerie()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.AlterarStatus(atividade.Id, Original(Dia(3)), Status.EmAndamento);

        var erros = agenda.Reagendar(atividade.Id, Original(Dia(3)), Utc(Dia(5), 17, 0), Utc(Dia(5), 18, 0), AgoraNaSegunda);

        Assert.Empty(erros);
        Assert.Empty(agenda.OcorrenciasDoDia(Dia(3)));

        var quinta = agenda.OcorrenciasDoDia(Dia(5));
        Assert.Equal(2, quinta.Count);
        var reagendada = quinta.Single(o => o.DataOriginal == Dia(3));
        Assert.Equal(new TimeOnly(17, 0), reagendada.HorarioInicio);
        Assert.Equal(Status.EmAndamento, reagendada.Status);

        Assert.Equal(new TimeOnly(10, 0), Unica(agenda, Dia(4)).HorarioInicio);
    }

    [Fact]
    public void Reagendar_ParaOPassado_EhBloqueado()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var agora = Utc(Dia(5), 12);

        var erros = agenda.Reagendar(atividade.Id, Original(Dia(3)), Utc(Dia(4), 10, 0), Utc(Dia(4), 11, 0), agora);
        Assert.Contains("Não é possível reagendar para uma data no passado.", erros);

        erros = agenda.Reagendar(atividade.Id, Original(Dia(3)), Utc(Dia(5), 11, 0), Utc(Dia(5), 11, 30), agora);
        Assert.Contains("Não é possível reagendar para uma data no passado.", erros);

        Assert.Single(agenda.OcorrenciasDoDia(Dia(3)));
    }

    [Fact]
    public void Reagendar_ForaDoExpediente_EhBloqueado()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));

        var erros = agenda.Reagendar(atividade.Id, Original(Dia(3)), Utc(Dia(5), 18, 30), Utc(Dia(5), 19, 30), AgoraNaSegunda);

        Assert.Contains("O horário de término deve estar entre 10:00 e 19:00.", erros);
    }

    // ---------- Pendências anteriores ----------

    [Fact]
    public void Pendencias_ExemploDoEscopo_SegundaApareceNaTerca_SemImpedirATerca()
    {
        var (agenda, atividade) = AgendaCom(Nova("Atendimento", Frequencia.Diaria, Segunda));

        var pendencia = Assert.Single(agenda.PendenciasAnteriores(Dia(3), Agora(Dia(3))));
        Assert.Equal(Segunda, pendencia.DataOriginal);
        Assert.Single(agenda.OcorrenciasDoDia(Dia(3)));
    }

    [Fact]
    public void Pendencias_SoPendenteOuEmAndamento()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.Concluir(atividade.Id, Original(Dia(3)));
        agenda.AlterarStatus(atividade.Id, Original(Dia(4)), Status.EmAndamento);

        var datas = agenda.PendenciasAnteriores(Dia(6), Agora(Dia(6))).Select(o => o.DataOriginal);

        Assert.Equal([Dia(2), Dia(4), Dia(5)], datas);
    }

    [Fact]
    public void Pendencias_SoOs7DiasAnteriores()
    {
        var (agenda, _) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));

        // Seleção em 12/11: janela de 05/11 a 11/11 (dias úteis 5, 6, 9, 10, 11).
        var datas = agenda.PendenciasAnteriores(Dia(12), Agora(Dia(12))).Select(o => o.DataOriginal);

        Assert.Equal([Dia(5), Dia(6), Dia(9), Dia(10), Dia(11)], datas);
    }

    [Fact]
    public void Pendencias_ComDataFuturaSelecionada_SoDiasAnterioresAHoje()
    {
        var (agenda, _) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));

        var datas = agenda.PendenciasAnteriores(Dia(6), Agora(Dia(4))).Select(o => o.DataOriginal);

        Assert.Equal([Dia(2), Dia(3)], datas);
    }

    [Fact]
    public void Pendencias_ReagendadaParaHoje_SaiDaLista()
    {
        var (agenda, atividade) = AgendaCom(Nova("Única", Frequencia.Unica, Segunda));
        var agora = Utc(Dia(4), 9);
        Assert.Single(agenda.PendenciasAnteriores(Dia(4), Agora(Dia(4))));

        agenda.Reagendar(atividade.Id, Original(Segunda), Utc(Dia(4), 15, 0), Utc(Dia(4), 16, 0), agora);

        Assert.Empty(agenda.PendenciasAnteriores(Dia(4), Agora(Dia(4))));
        Assert.Single(agenda.OcorrenciasDoDia(Dia(4)));
    }

    [Fact]
    public void Pendencias_ConcluidaSaiDaLista()
    {
        var (agenda, atividade) = AgendaCom(Nova("Única", Frequencia.Unica, Segunda));

        agenda.Concluir(atividade.Id, Original(Segunda));

        Assert.Empty(agenda.PendenciasAnteriores(Dia(4), Agora(Dia(4))));
    }

    [Fact]
    public void Pendencias_ReagendadaVoltaQuandoANovaDataPassa_MostrandoADataOriginal()
    {
        var (agenda, atividade) = AgendaCom(Nova("Única", Frequencia.Unica, Segunda));
        agenda.Reagendar(atividade.Id, Original(Segunda), Utc(Dia(4), 15, 0), Utc(Dia(4), 16, 0), Utc(Dia(3), 9));

        var pendencia = Assert.Single(agenda.PendenciasAnteriores(Dia(5), Agora(Dia(5))));
        Assert.Equal(Segunda, pendencia.DataOriginal);
        Assert.Equal(Dia(4), pendencia.Data);
    }

    // ---------- Painel ----------

    [Fact]
    public void Painel_ContaODiaEAsPendenciasAnteriores()
    {
        var agenda = new Agenda();
        var diaria = Nova("Diária", Frequencia.Diaria, Segunda, 10);
        agenda.Criar(diaria, AgoraNaSegunda);
        agenda.Criar(Nova("Semanal", Frequencia.Semanal, Dia(4), 11), AgoraNaSegunda);
        var unica = Nova("Única", Frequencia.Unica, Dia(4), 12);
        agenda.Criar(unica, AgoraNaSegunda);
        agenda.Concluir(diaria.Id, Original(Dia(4)));
        agenda.AlterarStatus(unica.Id, Original(Dia(4), 12), Status.EmAndamento);
        agenda.Concluir(diaria.Id, Original(Segunda));

        var painel = agenda.Painel(Dia(4), Agora(Dia(4)));

        Assert.Equal(new PainelDia(3, 1, 1, 1, 1), painel);
    }

    // ---------- Filtros ----------

    [Fact]
    public void Filtros_CombinadosComVariosValores()
    {
        var agenda = new Agenda();
        agenda.Criar(Nova("A", Frequencia.Unica, Segunda, 10, categoria: "Projetos", prioridade: Prioridade.Alta), AgoraNaSegunda);
        agenda.Criar(Nova("B", Frequencia.Unica, Segunda, 11, categoria: "Marketing", prioridade: Prioridade.Baixa), AgoraNaSegunda);
        agenda.Criar(Nova("C", Frequencia.Unica, Segunda, 12, categoria: "Webinars", prioridade: Prioridade.Alta), AgoraNaSegunda);
        agenda.Criar(Nova("D", Frequencia.Unica, Segunda, 13, categoria: "Projetos", prioridade: Prioridade.Media, status: Status.Concluida), AgoraNaSegunda);

        var filtro = new FiltroAgenda();
        Assert.Equal(4, agenda.AgendaDoDia(Segunda, filtro).Count);

        filtro.Categorias.UnionWith(["Projetos", "Marketing"]);
        Assert.Equal(["A", "B", "D"], agenda.AgendaDoDia(Segunda, filtro).Select(o => o.Titulo));

        filtro.Prioridades.UnionWith([Prioridade.Alta, Prioridade.Media]);
        Assert.Equal(["A", "D"], agenda.AgendaDoDia(Segunda, filtro).Select(o => o.Titulo));

        filtro.Status.Add(Status.Pendente);
        Assert.Equal(["A"], agenda.AgendaDoDia(Segunda, filtro).Select(o => o.Titulo));

        Assert.Equal(4, agenda.Painel(Segunda, Agora(Segunda)).Total);
    }

    // ---------- Edição de "só esta ocorrência" ----------

    [Fact]
    public void EditarOcorrencia_AlteraSoAquelaData()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var dados = new DadosOcorrencia("Outro título", "Obs", "Marketing", Prioridade.Alta, Utc(Dia(3), 16, 0), Utc(Dia(3), 17, 0));

        Assert.Empty(agenda.EditarOcorrencia(atividade.Id, Original(Dia(3)), dados, AgoraNaSegunda));

        var editada = Unica(agenda, Dia(3));
        Assert.Equal("Outro título", editada.Titulo);
        Assert.Equal("Obs", editada.Descricao);
        Assert.Equal("Marketing", editada.Categoria);
        Assert.Equal(Prioridade.Alta, editada.Prioridade);
        Assert.Equal(new TimeOnly(16, 0), editada.HorarioInicio);
        Assert.True(editada.EditadaIndividualmente);

        Assert.Equal("Diária", Unica(agenda, Dia(4)).Titulo);
    }

    [Fact]
    public void EditarOcorrencia_MudandoAData_MoveSoAquelaOcorrencia()
    {
        var (agenda, atividade) = AgendaCom(Nova("Semanal", Frequencia.Semanal, Segunda));
        var dados = new DadosOcorrencia("Semanal", null, "Projetos", Prioridade.Media, Utc(Dia(10), 10, 0), Utc(Dia(10), 10, 45));

        agenda.EditarOcorrencia(atividade.Id, Original(Dia(9)), dados, AgoraNaSegunda);

        Assert.Empty(agenda.OcorrenciasDoDia(Dia(9)));
        Assert.Equal(Dia(9), Unica(agenda, Dia(10)).DataOriginal);
        Assert.Single(agenda.OcorrenciasDoDia(Dia(16)));
    }

    [Fact]
    public void EditarOcorrencia_Invalida_RetornaErros()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var dados = new DadosOcorrencia("", null, "Inexistente", Prioridade.Alta, Utc(Dia(3), 11, 0), Utc(Dia(3), 10, 0));

        var erros = agenda.EditarOcorrencia(atividade.Id, Original(Dia(3)), dados, AgoraNaSegunda);

        Assert.Contains("O título é obrigatório.", erros);
        Assert.Contains("A categoria informada não existe.", erros);
        Assert.Contains("O horário de término deve ser maior que o de início.", erros);
        Assert.Equal("Diária", Unica(agenda, Dia(3)).Titulo);
    }

    // ---------- Edição da "série completa" ----------

    private static Atividade DadosDaSerie(Atividade origem, Action<Atividade> alterar)
    {
        var dados = new Atividade
        {
            Titulo = origem.Titulo,
            Descricao = origem.Descricao,
            Categoria = origem.Categoria,
            InicioUtc = origem.InicioUtc,
            TerminoUtc = origem.TerminoUtc,
            Prioridade = origem.Prioridade,
            Status = origem.Status,
            Frequencia = origem.Frequencia
        };
        alterar(dados);
        return dados;
    }

    private static void Horario(Atividade dados, DateOnly dia, int hi, int mi, int ht, int mt)
    {
        dados.InicioUtc = Utc(dia, hi, mi);
        dados.TerminoUtc = Utc(dia, ht, mt);
    }

    /// <summary>Muda a data de início mantendo o horário de Portugal.</summary>
    private static void Mover(Atividade dados, DateOnly dia)
    {
        var inicio = FusoHorario.Horario(dados.InicioUtc, FusoHorario.Portugal);
        var termino = FusoHorario.Horario(dados.TerminoUtc, FusoHorario.Portugal);
        Horario(dados, dia, inicio.Hour, inicio.Minute, termino.Hour, termino.Minute);
    }

    [Fact]
    public void EditarSerie_ValeDaquelaDataEmDiante()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.Concluir(atividade.Id, Original(Dia(3)));
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Horario(d, Dia(4), 14, 0, 14, 45); });

        Assert.Empty(agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda));

        Assert.Equal("Diária", Unica(agenda, Segunda).Titulo);
        var terca = Unica(agenda, Dia(3));
        Assert.Equal("Diária", terca.Titulo);
        Assert.Equal(Status.Concluida, terca.Status);

        var quarta = Unica(agenda, Dia(4));
        Assert.Equal("Novo", quarta.Titulo);
        Assert.Equal(new TimeOnly(14, 0), quarta.HorarioInicio);
        Assert.Equal("Novo", Unica(agenda, Dia(20)).Titulo);
    }

    [Fact]
    public void EditarSerie_NovaDataDeInicioAntesDaOcorrenciaAberta_NaoRepete()
    {
        // Ocorrência aberta: 10/11. Nova data de início: 05/11.
        var (agenda, atividade) = AgendaCom(Nova("Responder e-mails", Frequencia.Diaria, Segunda));
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "E-mails"; Mover(d, Dia(5)); });

        Assert.Empty(agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda));

        Assert.Equal("Responder e-mails", Unica(agenda, Dia(4)).Titulo);
        Assert.Equal("E-mails", Unica(agenda, Dia(5)).Titulo);
        Assert.Equal("E-mails", Unica(agenda, Dia(6)).Titulo);
        Assert.Equal("E-mails", Unica(agenda, Dia(9)).Titulo);
        Assert.Equal("E-mails", Unica(agenda, Dia(10)).Titulo);
    }

    [Fact]
    public void EditarSerie_NovaDataDeInicioDepoisDaOcorrenciaAberta_AntigaVaiAteAVespera()
    {
        // Ocorrência aberta: 10/11. Nova data de início: 16/11.
        var (agenda, atividade) = AgendaCom(Nova("Responder e-mails", Frequencia.Diaria, Segunda));
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "E-mails"; Mover(d, Dia(16)); });

        agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda);

        Assert.Equal("Responder e-mails", Unica(agenda, Dia(10)).Titulo);
        Assert.Equal("Responder e-mails", Unica(agenda, Dia(13)).Titulo);
        Assert.Equal("E-mails", Unica(agenda, Dia(16)).Titulo);
    }

    [Fact]
    public void EditarSerie_ComInicioHoje_OcorrenciaDeHojeQueJaComecouNaoMudaENaoSeRepete()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda, 10));
        agenda.Concluir(atividade.Id, Original(Dia(4)));
        var agora = Utc(Dia(4), 12);
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Horario(d, Dia(4), 14, 0, 14, 45); });

        Assert.Empty(agenda.EditarAtividade(atividade.Id, dados, agora));

        var quarta = Unica(agenda, Dia(4));
        Assert.Equal("Diária", quarta.Titulo);
        Assert.Equal(new TimeOnly(10, 0), quarta.HorarioInicio);
        Assert.Equal(Status.Concluida, quarta.Status);

        var quinta = Unica(agenda, Dia(5));
        Assert.Equal("Novo", quinta.Titulo);
        Assert.Equal(new TimeOnly(14, 0), quinta.HorarioInicio);
        Assert.DoesNotContain(agenda.PendenciasAnteriores(Dia(5), Agora(Dia(5))), o => o.Titulo == "Novo");
    }

    [Fact]
    public void EditarSerie_ComInicioHoje_TrocaAOcorrenciaDeHojeQueAindaNaoComecou()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda, 15));
        var agora = Utc(Dia(4), 12);
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Mover(d, Dia(4)); });

        agenda.EditarAtividade(atividade.Id, dados, agora);

        Assert.Equal("Novo", Unica(agenda, Dia(4)).Titulo);
        Assert.Equal("Diária", Unica(agenda, Dia(3)).Titulo);
    }

    [Fact]
    public void EditarSerie_SerieQueComecouHoje_OcorrenciaQueJaComecouNaoMudaENaoSeRepete()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Dia(4), 10));
        var agora = Utc(Dia(4), 12);
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Horario(d, Dia(4), 14, 0, 14, 45); });

        agenda.EditarAtividade(atividade.Id, dados, agora);

        Assert.Equal("Diária", Unica(agenda, Dia(4)).Titulo);
        Assert.Equal("Novo", Unica(agenda, Dia(5)).Titulo);
    }

    [Fact]
    public void EditarSerie_SemanalComInicioHoje_JaAconteceu_ProximaNaSemanaSeguinte()
    {
        var (agenda, atividade) = AgendaCom(Nova("Semanal", Frequencia.Semanal, Segunda, 10));
        var agora = Utc(Dia(9), 12);
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Horario(d, Dia(9), 15, 0, 15, 30); });

        agenda.EditarAtividade(atividade.Id, dados, agora);

        Assert.Equal("Semanal", Unica(agenda, Dia(9)).Titulo);
        Assert.Empty(agenda.OcorrenciasDoDia(Dia(10)));
        Assert.Equal("Novo", Unica(agenda, Dia(16)).Titulo);
    }

    [Fact]
    public void EditarSerie_NaoAlteraOcorrenciaEditadaIndividualmente()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var individual = new DadosOcorrencia("Individual", null, "Projetos", Prioridade.Baixa, Utc(Dia(5), 11, 0), Utc(Dia(5), 11, 30));
        agenda.EditarOcorrencia(atividade.Id, Original(Dia(5)), individual, AgoraNaSegunda);

        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Mover(d, Dia(3)); d.Frequencia = Frequencia.Semanal; });
        agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda);

        var quinta = Unica(agenda, Dia(5));
        Assert.Equal("Individual", quinta.Titulo);
        Assert.Equal(new TimeOnly(11, 0), quinta.HorarioInicio);
        Assert.Equal(Prioridade.Baixa, quinta.Prioridade);

        Assert.Empty(agenda.OcorrenciasDoDia(Dia(4)));
        Assert.Equal("Novo", Unica(agenda, Dia(10)).Titulo);
    }

    [Fact]
    public void EditarSerie_MantemStatusEReagendamentoDasOcorrenciasSeguintes()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.AlterarStatus(atividade.Id, Original(Dia(5)), Status.EmAndamento);
        agenda.Reagendar(atividade.Id, Original(Dia(6)), Utc(Dia(7), 15, 0), Utc(Dia(7), 16, 0), AgoraNaSegunda);

        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Mover(d, Dia(4)); });
        agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda);

        var quinta = Unica(agenda, Dia(5));
        Assert.Equal("Novo", quinta.Titulo);
        Assert.Equal(Status.EmAndamento, quinta.Status);

        Assert.Empty(agenda.OcorrenciasDoDia(Dia(6)));
        var sabado = Unica(agenda, Dia(7));
        Assert.Equal("Novo", sabado.Titulo);
        Assert.Equal(Dia(6), sabado.DataOriginal);
    }

    [Fact]
    public void EditarSerie_NaDataDeInicio_StatusDoFormularioValeParaAquelaData()
    {
        var (agenda, atividade) = AgendaCom(Nova("Semanal", Frequencia.Semanal, Segunda));
        var dados = DadosDaSerie(atividade, d => d.Status = Status.Concluida);

        agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda);

        Assert.Equal(Status.Concluida, Unica(agenda, Segunda).Status);
        Assert.Equal(Status.Pendente, Unica(agenda, Dia(9)).Status);
        Assert.Single(agenda.Atividades);
    }

    [Fact]
    public void EditarSerie_ComInicioNoPassado_EhBloqueada()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var agora = Utc(Dia(5), 12);

        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Mover(d, Dia(4)); });
        Assert.Contains("Não é possível editar uma atividade para início no passado.", agenda.EditarAtividade(atividade.Id, dados, agora));

        dados = DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Horario(d, Dia(5), 11, 0, 11, 45); });
        Assert.Contains("Não é possível editar uma atividade para início no passado.", agenda.EditarAtividade(atividade.Id, dados, agora));

        Assert.Equal("Diária", Unica(agenda, Dia(4)).Titulo);
        Assert.Equal("Diária", Unica(agenda, Dia(6)).Titulo);
        Assert.Single(agenda.Atividades);
    }

    [Fact]
    public void EditarUnica_ParaOPassado_EhBloqueada()
    {
        var (agenda, atividade) = AgendaCom(Nova("Única", Frequencia.Unica, Dia(6)));
        var agora = Utc(Dia(5), 12);
        var dados = DadosDaSerie(atividade, d => Mover(d, Dia(4)));

        Assert.Contains("Não é possível editar uma atividade para início no passado.", agenda.EditarAtividade(atividade.Id, dados, agora));
        Assert.Single(agenda.OcorrenciasDoDia(Dia(6)));
    }

    [Fact]
    public void EditarOcorrencia_ParaOPassado_EhBloqueada()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var agora = Utc(Dia(5), 12);

        var paraDataPassada = new DadosOcorrencia("Outro", null, "Projetos", Prioridade.Alta, Utc(Dia(4), 16, 0), Utc(Dia(4), 17, 0));
        Assert.Contains("Não é possível editar uma atividade para início no passado.", agenda.EditarOcorrencia(atividade.Id, Original(Dia(6)), paraDataPassada, agora));

        var paraHoraPassada = new DadosOcorrencia("Outro", null, "Projetos", Prioridade.Alta, Utc(Dia(5), 11, 0), Utc(Dia(5), 11, 30));
        Assert.Contains("Não é possível editar uma atividade para início no passado.", agenda.EditarOcorrencia(atividade.Id, Original(Dia(6)), paraHoraPassada, agora));

        Assert.Equal("Diária", Unica(agenda, Dia(6)).Titulo);
        Assert.Single(agenda.OcorrenciasDoDia(Dia(4)));
    }

    [Fact]
    public void EditarSerie_Invalida_NaoAltera()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var dados = DadosDaSerie(atividade, d => d.Titulo = "");

        Assert.Contains("O título é obrigatório.", agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda));
        Assert.Equal("Diária", Unica(agenda, Dia(5)).Titulo);
    }

    [Fact]
    public void EditarSerie_DuasVezes_AUltimaValeDaquelaDataEmDiante()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.EditarAtividade(atividade.Id, DadosDaSerie(atividade, d => { d.Titulo = "B"; Mover(d, Dia(9)); }), AgoraNaSegunda);
        agenda.EditarAtividade(atividade.Id, DadosDaSerie(atividade, d => { d.Titulo = "C"; Mover(d, Dia(4)); }), AgoraNaSegunda);

        Assert.Equal("Diária", Unica(agenda, Dia(3)).Titulo);
        Assert.Equal("C", Unica(agenda, Dia(4)).Titulo);
        Assert.Equal("C", Unica(agenda, Dia(10)).Titulo);
    }

    [Fact]
    public void EditarUnica_AlteraAAtividade()
    {
        var (agenda, atividade) = AgendaCom(Nova("Única", Frequencia.Unica, Dia(4)));
        var dados = DadosDaSerie(atividade, d => { d.Titulo = "Alterada"; Mover(d, Dia(6)); d.Status = Status.EmAndamento; });

        Assert.Empty(agenda.EditarAtividade(atividade.Id, dados, AgoraNaSegunda));

        Assert.Empty(agenda.OcorrenciasDoDia(Dia(4)));
        var sexta = Unica(agenda, Dia(6));
        Assert.Equal("Alterada", sexta.Titulo);
        Assert.Equal(Status.EmAndamento, sexta.Status);
        Assert.Single(agenda.Atividades);
    }

    // ---------- Exclusão ----------

    [Fact]
    public void ExcluirOcorrencia_SoAquelaData()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));

        agenda.ExcluirOcorrencia(atividade.Id, Original(Dia(3)));

        Assert.Empty(agenda.OcorrenciasDoDia(Dia(3)));
        Assert.Single(agenda.OcorrenciasDoDia(Dia(4)));
        Assert.Single(agenda.OcorrenciasDoDia(Segunda));
    }

    [Fact]
    public void ExcluirSerie_ApagaASerieInteira_InclusivePartesDaEdicao()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        var outra = Nova("Outra", Frequencia.Semanal, Segunda, 15);
        agenda.Criar(outra, AgoraNaSegunda);
        agenda.EditarAtividade(atividade.Id, DadosDaSerie(atividade, d => { d.Titulo = "Novo"; Mover(d, Dia(4)); }), AgoraNaSegunda);
        agenda.Concluir(atividade.Id, Original(Dia(3)));

        agenda.ExcluirSerie(atividade.Id);

        Assert.Equal(["Outra"], agenda.OcorrenciasDoDia(Segunda).Select(o => o.Titulo));
        Assert.Empty(agenda.OcorrenciasDoDia(Dia(4)));
        Assert.Empty(agenda.Excecoes);
        Assert.Single(agenda.Atividades);
    }

    // ---------- Persistência ----------

    [Fact]
    public void AgendaRecarregada_MantemRegrasEStatusDasOcorrencias()
    {
        var (agenda, atividade) = AgendaCom(Nova("Diária", Frequencia.Diaria, Segunda));
        agenda.Concluir(atividade.Id, Original(Dia(3)));

        var recarregada = new Agenda(agenda.Atividades, agenda.Excecoes);

        Assert.Equal(Status.Concluida, Unica(recarregada, Dia(3)).Status);
        Assert.Equal(Status.Pendente, Unica(recarregada, Dia(4)).Status);
    }
}
