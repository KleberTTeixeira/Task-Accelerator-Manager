// Confere se o navegador libera o que a agenda precisa antes de iniciar o Blazor. Se algo estiver bloqueado, mostra o aviso no lugar da agenda.
window.verificacaoNavegador = {
  armazenamentoLiberado: function () {
    try {
      var chave = "taskmanager:teste";
      window.localStorage.setItem(chave, "1");
      var lido = window.localStorage.getItem(chave);
      window.localStorage.removeItem(chave);
      return lido === "1";
    } catch (e) {
      return false;
    }
  },

  webAssemblyLiberado: function () {
    try {
      if (typeof WebAssembly !== "object" || typeof WebAssembly.instantiate !== "function") return false;
      // Módulo vazio: só o cabeçalho "\0asm" e a versão 1.
      var modulo = new WebAssembly.Module(new Uint8Array([0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00]));
      return modulo instanceof WebAssembly.Module;
    } catch (e) {
      return false;
    }
  },

  bloqueios: function () {
    var lista = [];
    if (!this.armazenamentoLiberado()) lista.push("armazenamento");
    if (!this.webAssemblyLiberado()) lista.push("webassembly");
    return lista;
  },

  mostrarAviso: function (bloqueios) {
    var itens = {
      armazenamento: "<li><strong>Armazenamento de dados do site</strong> (cookies e dados do site): é onde a agenda guarda as suas tarefas.</li>",
      webassembly: "<li><strong>WebAssembly</strong>: é a tecnologia que faz a agenda funcionar dentro do navegador.</li>"
    };

    var comoLiberar = {
      armazenamento:
        "<h2>Como liberar o armazenamento de dados do site</h2>" +
        "<details><summary>Google Chrome (computador)</summary><ol>" +
        "<li>Copie e cole na barra de endereço: <code>chrome://settings/content/siteData</code></li>" +
        "<li>Marque <em>Permitir que os sites salvem dados no seu dispositivo</em>, ou adicione este site em <em>Permitido salvar dados no seu dispositivo</em>.</li>" +
        "<li>Volte para esta página e clique em <em>Tentar de novo</em>.</li></ol></details>" +
        "<details><summary>Microsoft Edge (computador)</summary><ol>" +
        "<li>Copie e cole na barra de endereço: <code>edge://settings/content/cookies</code></li>" +
        "<li>Ative <em>Permitir que os sites salvem e leiam dados de cookie</em>, ou adicione este site em <em>Permitir</em>.</li>" +
        "<li>Volte para esta página e clique em <em>Tentar de novo</em>.</li></ol></details>" +
        "<details><summary>Mozilla Firefox (computador)</summary><ol>" +
        "<li>Copie e cole na barra de endereço: <code>about:preferences#privacy</code></li>" +
        "<li>Em <em>Cookies e dados de sites</em>, clique em <em>Gerenciar exceções</em>, informe o endereço deste site e clique em <em>Permitir</em>.</li>" +
        "<li>Salve, volte para esta página e clique em <em>Tentar de novo</em>.</li></ol></details>" +
        "<details><summary>Safari (Mac, iPhone e iPad)</summary><ol>" +
        "<li>No Mac: menu <em>Safari</em> → <em>Ajustes</em> → <em>Privacidade</em> e desmarque <em>Bloquear todos os cookies</em>.</li>" +
        "<li>No iPhone ou iPad: <em>Ajustes</em> → <em>Safari</em> (nas versões mais novas, <em>Ajustes</em> → <em>Apps</em> → <em>Safari</em>) e desative <em>Bloquear Todos os Cookies</em>.</li>" +
        "<li>Volte para esta página e clique em <em>Tentar de novo</em>.</li></ol></details>",
      webassembly:
        "<h2>Como liberar o WebAssembly</h2>" +
        "<p>Todos os navegadores atuais (Chrome, Edge, Firefox e Safari) têm o WebAssembly ligado por padrão. " +
        "Se ele está bloqueado, a causa costuma ser um modo de segurança reforçada do navegador ou do sistema. " +
        "Desative esse modo para este site, ou abra a agenda em outro navegador atualizado.</p>"
    };

    var lista = "";
    var instrucoes = "";
    for (var i = 0; i < bloqueios.length; i++) {
      lista += itens[bloqueios[i]];
      instrucoes += comoLiberar[bloqueios[i]];
    }

    var app = document.getElementById("app");
    app.innerHTML =
      "<section class=\"aviso-bloqueio\">" +
      "<h1>O navegador está bloqueando recursos necessários para a agenda</h1>" +
      "<p>Para a agenda funcionar, libere neste navegador:</p>" +
      "<ul>" + lista + "</ul>" +
      "<div class=\"aviso-seguranca\"><strong>A liberação é segura.</strong> A agenda guarda os dados somente neste navegador, no seu próprio aparelho. " +
      "Nada é enviado para servidores, e não há login, anúncios nem rastreamento. A liberação é necessária para a agenda abrir e guardar as suas tarefas.</div>" +
      "<p class=\"aviso-site\">Endereço deste site: <code>" + location.origin + "</code></p>" +
      instrucoes +
      "<button type=\"button\" class=\"aviso-botao\" onclick=\"location.reload()\">Tentar de novo</button>" +
      "</section>";
  },

  iniciar: function () {
    var bloqueios = this.bloqueios();
    if (bloqueios.length > 0) {
      this.mostrarAviso(bloqueios);
      return;
    }
    Blazor.start();
  }
};

window.verificacaoNavegador.iniciar();
