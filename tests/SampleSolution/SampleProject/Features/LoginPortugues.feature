#language: pt-BR
Funcionalidade: Login de Usuário
  Como um usuário registrado
  Eu quero fazer login no sistema
  Para que eu possa acessar minha conta

  Cenário: Login com credenciais válidas
    Dado que existe um usuário "joao"
    E o usuário está na página de login
    Quando o usuário informa a senha "senha123"
    E clica no botão de login
    Então o usuário deve ver o dashboard

  Cenário: Login com senha inválida
    Dado que existe um usuário "joao"
    Quando o usuário informa a senha "senhaerrada"
    Então o usuário deve ver mensagem de erro

  Esquema do Cenário: Login com diferentes tipos de usuário
    Dado que existe um usuário "<usuario>"
    Quando o usuário informa a senha "<senha>"
    Então o usuário deve ver o dashboard

    Exemplos:
      | usuario | senha     |
      | admin   | admin123  |
      | guest   | guest456  |
