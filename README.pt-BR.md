<div align="center">

# Fouroam Server

**Sua sessão. Seu computador. Suas regras.**

Protótipo de servidor de rede auto-hospedado para o [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V).

[![MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-blue.svg)](LICENSE)
![Versão](https://img.shields.io/badge/vers%C3%A3o-0.2.1--dev-orange.svg)
![.NET](https://img.shields.io/badge/.NET-10-purple.svg)
[![.NET CI](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md)

</div>

> [!IMPORTANT]
> **Protótipo independente de rede.** Ainda não conecta ao GTA V nem oferece multiplayer no jogo ou missões oficiais. Não há instalador/release publicada.

## Novidades da v0.2.0-dev

- **TLS 1.2/1.3 obrigatório** em todas as conexões, com verificação do fingerprint SHA-256 do certificado do host. Não existe conexão sem criptografia.
- **Configuração privada**, com nome da sessão e senha armazenada em forma de hash PBKDF2-SHA256 com salt.
- **Console administrativo do host**: \`help\`, \`status\`, \`players\`, \`kick\`, \`ban\`, \`bans\`, \`unban\`, \`say\` e \`stop\`.
- **Banimentos persistentes por identidade de instalação**: a identidade do cliente é gerada aleatoriamente e salva localmente; o servidor armazena apenas seu fingerprint.
- **Proteção contra tentativas repetidas**: cinco falhas de autenticação em 60 segundos por endereço remoto.
- **Testes automatizados** para expulsão, permissões, senha, banimentos e funções antigas.

O servidor mantém chat, ping, lista de jogadores e limite de quatro conexões, mas a mudança para **protocolo v2** impede a conexão dos clientes antigos sem TLS.

## Executáveis Windows x64 para testes

O [GitHub Actions](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/windows-exe.yml) compila **Fouroam.Server.exe** e **Fouroam.Client.Test.exe** auto-contidos. Baixe os ZIPs na seção **Artifacts** de uma execução concluída com sucesso. O .NET SDK não é necessário para rodar esses EXEs.

São **builds de desenvolvimento sem assinatura digital**, não versões de produção. Leia o [guia dos executáveis](docs/WINDOWS-EXE.md) e o [checklist antes da produção](docs/PRODUCTION-READINESS.md).

Os comandos do console administrativo também podem começar com `/`, por exemplo `/kick 1`, ou usar o nome do jogador quando não houver ambiguidade, por exemplo `/kick Athilio`.

## Como configurar

Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) e abra um terminal na pasta raiz do repositório.

**1. Inicialize uma vez**, criando o nome da sessão e a senha de 4–128 caracteres:

~~~powershell
dotnet restore Fouroam-Server.slnx
dotnet run --project src/Fouroam.Server -- init
~~~

**Aviso de segurança:** senhas de 4 caracteres são fracas. Prefira uma senha longa e única; use o servidor somente em LAN/VPN privada e não abra a porta na internet.

Guarde o **fingerprint SHA-256** exibido no terminal. Ele permite que os clientes confirmem que estão conectando ao servidor correto.

A pasta \`.fouroam/\` guarda o hash da senha, a chave privada TLS e os banimentos. **Não compartilhe nem envie essa pasta para o GitHub.** Proteja seu acesso no Windows.

**2. Inicie o servidor local:**

~~~powershell
dotnet run --project src/Fouroam.Server -- --bind 127.0.0.1 --port 7777
~~~

Para conectar outros PCs em LAN ou Radmin VPN privada e confiável, execute com \`--bind 0.0.0.0\`, restringindo a porta TCP 7777 no Firewall do Windows. **Não faça port forwarding ou exposição pública desse protótipo.**

**3. Conecte o cliente**, em outro terminal (substitua o fingerprint pelo exibido no servidor):

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Athilio --fingerprint COLE_AQUI_O_FINGERPRINT_SHA256
~~~

O cliente pedirá a senha sem exibi-la na tela. **Não escreva a senha na linha de comando.**

Para testar com quatro clientes no mesmo PC, cada um deve usar um arquivo de identidade diferente, por exemplo:

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Michael --fingerprint SEU_FINGERPRINT --identity-file .fouroam-client/michael.key
~~~

Repita com \`franklin.key\` e \`trevor.key\`. O primeiro cliente pode manter a identidade padrão. O cliente salva sua identidade padrão na pasta LocalAppData/Fouroam do Windows.

## Comandos administrativos

No **terminal do servidor**, não no terminal dos clientes:

| Comando | Efeito |
| --- | --- |
| \`help\` | Mostra os comandos |
| \`players\` | Lista nome, ID e início do fingerprint |
| \`status\` | Mostra informações da sessão |
| \`kick 3 motivo\` | Expulsa a conexão de ID 3 |
| \`ban 3 motivo\` | Bloqueia a identidade e expulsa o jogador |
| \`bans\` | Lista fingerprints banidos |
| \`unban ABCDEF12\` | Remove banimento por prefixo único, com pelo menos 8 caracteres |
| \`say mensagem\` | Envia anúncio a todos |
| \`stop\` | Encerra o servidor corretamente |

IDs como \`#3\` mudam a cada reconexão. O banimento usa o fingerprint da identidade persistida pelo cliente, **não um ID temporário nem um IP**.

**Limitação:** sem contas verificadas, a pessoa pode gerar outro arquivo de identidade para contornar o banimento. Esse mecanismo não é um anti-cheat.

## Segurança

- A senha trafega somente por um canal TLS autenticado pelo fingerprint.
- O certificado é autoassinado e a chave privada fica no PC do host. A proteção do arquivo é responsabilidade do sistema operacional e do usuário.
- O fingerprint deve ser compartilhado e confirmado por um canal confiável, diferente da conexão que está sendo estabelecida.
- Restringir conexões via LAN/VPN e firewall continua sendo necessário. **Não é um servidor endurecido para a internet pública.**

## Testes

~~~powershell
dotnet build Fouroam-Server.slnx --configuration Release
dotnet run --project tests/Fouroam.SmokeTests --configuration Release
~~~

Acompanhe o [GitHub Actions](https://github.com/Fouroam-Dev/Fouroam-Server/actions) para os resultados dos testes Windows e Linux.

Mais detalhes: [Roadmap](ROADMAP.md), [Arquitetura](docs/ARCHITECTURE.md) e [Contribuições](CONTRIBUTING.md).

## Aviso legal

Projeto independente e não oficial, sem afiliação com Rockstar Games, Take-Two Interactive, Valve ou Epic Games. GTA V e suas propriedades pertencem aos titulares correspondentes. A licença MIT cobre somente nosso código e documentação originais. Integrações com o jogo e missões oficiais dependem de análise de viabilidade e permissões.

## Licença

[MIT](LICENSE), copyright (c) 2026 Fouroam-Dev.
