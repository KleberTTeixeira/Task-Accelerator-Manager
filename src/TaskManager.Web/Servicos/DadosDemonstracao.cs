using System.Globalization;
using TaskManager.Domain;

namespace TaskManager.Web.Servicos;

/// <summary>
/// Rotina fictícia da seção 2 do escopo. Diárias, semanais e pontuais têm as datas calculadas a partir do dia da carga;
/// quinzenais e mensais usam as datas do escopo. Parte das ocorrências passadas fica Concluída e parte fica
/// Pendente ou Em andamento. Os registros são montados direto, sem a validação do cadastro, porque começam no passado.
/// </summary>
public static class DadosDemonstracao
{
    /// <summary>Diárias e semanais começam cerca de duas semanas antes, para já existirem pendências anteriores.</summary>
    private const int DiasAntes = 14;

    public static Agenda Criar(DateTime agoraUtc)
    {
        //return new Agenda(); Inicia sem registros de teste - agenda zerada
        var hoje = FusoHorario.Data(agoraUtc, FusoHorario.Portugal);
        var inicio = hoje.AddDays(-DiasAntes);
        var atividades = new List<Atividade>();

        void Adicionar(string titulo, string categoria, Prioridade prioridade, Frequencia frequencia,
            DateOnly data, string horarioInicio, string horarioTermino, Status status = Status.Pendente)
        {
            var atividade = new Atividade
            {
                Titulo = titulo,
                Categoria = categoria,
                Prioridade = prioridade,
                Frequencia = frequencia,
                Status = status,
                InicioUtc = FusoHorario.ParaUtc(data, Horario(horarioInicio), FusoHorario.Portugal),
                TerminoUtc = FusoHorario.ParaUtc(data, Horario(horarioTermino), FusoHorario.Portugal)
            };
            atividade.SerieId = atividade.Id;
            atividades.Add(atividade);
        }

        // Diárias (segunda a sexta)
        var segunda = Proximo(inicio, DayOfWeek.Monday);
        Adicionar("Responder e-mails e demandas de suporte", "Atendimento e Suporte", Prioridade.Alta, Frequencia.Diaria, segunda, "10:00", "10:45");
        Adicionar("Atendimento aos talentos", "Atendimento e Suporte", Prioridade.Alta, Frequencia.Diaria, segunda, "10:45", "11:30");
        Adicionar("Correção de projetos", "Projetos", Prioridade.Media, Frequencia.Diaria, segunda, "11:30", "12:15");
        Adicionar("Elaboração e envio de feedbacks", "Feedbacks", Prioridade.Media, Frequencia.Diaria, segunda, "12:15", "13:00");
        Adicionar("Acompanhamento dos talentos", "Acompanhamento", Prioridade.Media, Frequencia.Diaria, segunda, "14:00", "14:45");
        Adicionar("Criação de novos projetos", "Projetos", Prioridade.Baixa, Frequencia.Diaria, segunda, "14:45", "15:30");

        // Semanais (a comunidade são 3 atividades semanais separadas)
        Adicionar("Revisão de prioridades da semana", "Planejamento", Prioridade.Alta, Frequencia.Semanal, segunda, "15:30", "16:00");
        Adicionar("Gerenciamento da comunidade", "Comunidade", Prioridade.Baixa, Frequencia.Semanal, segunda, "16:00", "16:30");
        Adicionar("Gerenciamento da comunidade", "Comunidade", Prioridade.Baixa, Frequencia.Semanal, Proximo(inicio, DayOfWeek.Wednesday), "15:30", "16:00");
        Adicionar("Gerenciamento da comunidade", "Comunidade", Prioridade.Baixa, Frequencia.Semanal, Proximo(inicio, DayOfWeek.Friday), "15:30", "16:00");

        // Quinzenais
        Adicionar("Análise de métricas de Customer Success", "Customer Success", Prioridade.Alta, Frequencia.Quinzenal, new DateOnly(2026, 11, 3), "16:00", "17:00");
        Adicionar("Análise de dados operacionais", "Análise de Dados", Prioridade.Media, Frequencia.Quinzenal, new DateOnly(2026, 11, 5), "16:00", "17:00");
        Adicionar("Análise de métricas de marketing", "Marketing", Prioridade.Media, Frequencia.Quinzenal, new DateOnly(2026, 11, 6), "16:00", "17:00");

        // Mensais
        Adicionar("Planejamento de treinamentos e cursos", "Treinamentos", Prioridade.Media, Frequencia.Mensal, new DateOnly(2026, 11, 10), "16:00", "17:00");
        Adicionar("Organização de webinars", "Webinars", Prioridade.Baixa, Frequencia.Mensal, new DateOnly(2026, 11, 12), "16:00", "17:00");
        Adicionar("Organização da mentoria mensal", "Mentorias", Prioridade.Media, Frequencia.Mensal, new DateOnly(2026, 11, 17), "16:00", "17:00");
        Adicionar("Realização da mentoria mensal", "Mentorias", Prioridade.Alta, Frequencia.Mensal, new DateOnly(2026, 11, 19), "17:00", "18:00");
        Adicionar("Análise de reputação e satisfação dos clientes", "Estratégia e Reputação", Prioridade.Alta, Frequencia.Mensal, new DateOnly(2026, 11, 24), "16:00", "17:00");

        // Pontuais
        Adicionar("Atualizar materiais de treinamento", "Treinamentos", Prioridade.Media, Frequencia.Unica, hoje.AddDays(-4), "17:00", "18:00", Status.Concluida);
        Adicionar("Revisar o conteúdo de um novo curso", "Treinamentos", Prioridade.Alta, Frequencia.Unica, hoje.AddDays(-2), "16:30", "17:30");
        Adicionar("Investigar uma queda em um indicador de satisfação", "Estratégia e Reputação", Prioridade.Alta, Frequencia.Unica, hoje, "17:00", "18:00");
        Adicionar("Preparar uma apresentação especial", "Outras", Prioridade.Media, Frequencia.Unica, hoje.AddDays(2), "16:30", "17:30");

        return new Agenda(atividades, StatusDoPassado(atividades, inicio, hoje));
    }

    /// <summary>Status das ocorrências recorrentes antes de hoje: a maioria Concluída, parte Pendente ou Em andamento.</summary>
    private static List<ExcecaoOcorrencia> StatusDoPassado(List<Atividade> atividades, DateOnly inicio, DateOnly hoje)
    {
        var excecoes = new List<ExcecaoOcorrencia>();
        var recorrentes = atividades.Where(a => a.Frequencia != Frequencia.Unica).ToList();

        for (var dia = inicio; dia < hoje; dia = dia.AddDays(1))
        {
            for (var i = 0; i < recorrentes.Count; i++)
            {
                var atividade = recorrentes[i];
                if (!Recorrencia.OcorreEm(atividade, dia))
                    continue;

                var status = ((dia.DayNumber + i) % 7) switch
                {
                    0 => Status.Pendente,
                    3 => Status.EmAndamento,
                    _ => Status.Concluida
                };
                if (status == Status.Pendente)
                    continue;

                var horario = FusoHorario.Horario(atividade.InicioUtc, FusoHorario.Portugal);
                excecoes.Add(new ExcecaoOcorrencia
                {
                    AtividadeId = atividade.Id,
                    OriginalUtc = FusoHorario.ParaUtc(dia, horario, FusoHorario.Portugal),
                    Status = status
                });
            }
        }

        return excecoes;
    }

    /// <summary>Primeira data igual ou posterior a <paramref name="data"/> que cai no dia da semana informado.</summary>
    private static DateOnly Proximo(DateOnly data, DayOfWeek diaDaSemana) =>
        data.AddDays(((int)diaDaSemana - (int)data.DayOfWeek + 7) % 7);

    private static TimeOnly Horario(string texto) => TimeOnly.Parse(texto, CultureInfo.InvariantCulture);
}
