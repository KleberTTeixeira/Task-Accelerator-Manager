# TaskManager — Agenda de gestão de tarefas e atendimentos

Agenda individual para organizar tarefas e atendimentos, com recorrências, pendências anteriores, filtros e painel do dia. Feita em C# com Blazor WebAssembly (standalone, sem servidor) e .NET 10. Os dados ficam gravados no localStorage do navegador.

## Requisito

- [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

## Como executar localmente

Na pasta raiz do projeto:

```bash
dotnet run --project src/TaskManager.Web
```

Depois, abra http://localhost:5154 no navegador.

Na primeira abertura, os dados de demonstração são carregados automaticamente. O botão "Restaurar demonstração" apaga os dados atuais e carrega a demonstração de novo (ele pede confirmação antes).

## Como executar os testes

```bash
dotnet test TaskManager.sln
```

## Estrutura da solução

- `src/TaskManager.Domain`: regras de negócio (atividades, validações, recorrência, ocorrências, painel, pendências, filtros e fuso horário).
- `src/TaskManager.Web`: interface em Blazor WebAssembly.
- `tests/TaskManager.Tests`: testes automatizados (xUnit).
