// Posição do cursor em um campo de texto, usada para inserir emojis no ponto em que o usuário está.
window.cursorCampo = {
  selecao: function (id) {
    var campo = document.getElementById(id);
    return campo ? [campo.selectionStart, campo.selectionEnd] : null;
  },
  posicionar: function (id, posicao) {
    var campo = document.getElementById(id);
    if (!campo) return;
    campo.focus();
    campo.setSelectionRange(posicao, posicao);
  }
};
