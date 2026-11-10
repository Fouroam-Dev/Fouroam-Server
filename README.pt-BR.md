<div align="center">

# Fouroam Server

**Sua sessão. Seu computador. Suas regras.**

Protótipo de servidor de rede auto-hospedado para o [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V).

[![Licença MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-blue.svg)](LICENSE)
![Versão](https://img.shields.io/badge/vers%C3%A3o-0.1.0--dev-orange.svg)
![.NET](https://img.shields.io/badge/.NET-10-purple.svg)
[![.NET CI](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Fouroam-Dev/Fouroam-Server/actions/workflows/dotnet.yml)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md) · [Contribuições](CONTRIBUTING.md)

</div>

> [!IMPORTANT]
> **Protótipo independente de rede.** O código possui um servidor TCP, cliente de teste e testes automatizados. **Não há integração com GTA V, sincronização do mundo ou campanha cooperativa.** Ainda não existe instalador ou release para baixar.

## O que já foi implementado no código

- Servidor TCP em **C# / .NET 10**, hospedado pelo próprio usuário.
- Limite de **quatro jogadores simultâneos**.
- Identificação por nome, handshake com versão de protocolo e IDs únicos.
- Avisos de entrada/saída, lista de jogadores, chat e ping.
- Mensagens JSON com limite de 4096 bytes e limitação básica de frequência de comandos.
- Cliente de console para simular jogadores sem abrir o GTA V.
- Testes de capacidade, desconexão, reconexão e comunicação.

**Atenção à segurança:** esta primeira versão **não tem senha, autenticação ou criptografia**. Não abra portas na internet nem faça port forwarding. Use somente uma LAN ou VPN privada confiável e restrinja o acesso pelo firewall. Não transmita informações sensíveis.

## Requisitos

Instale o [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) no Windows. O jogo GTA V **não é necessário para testar a rede**.

## Iniciar servidor (PowerShell)

No diretório raiz do projeto:

~~~powershell
dotnet restore Fouroam-Server.slnx
dotnet run --project src/Fouroam.Server -- --bind 127.0.0.1 --port 7777
~~~

Para aceitar outros computadores em uma **LAN/VPN privada de confiança**, use:

~~~powershell
dotnet run --project src/Fouroam.Server -- --bind 0.0.0.0 --port 7777
~~~

A porta é **TCP 7777**. Configure o firewall para permitir somente os participantes da rede privada. Quem usa Radmin VPN deve conectar pelo IPv4 do Radmin do computador host, não pelo IP público. **Não redirecione a porta no roteador para a internet.**

## Conectar um cliente de teste

Em outro terminal:

~~~powershell
dotnet run --project src/Fouroam.Client.Test -- --host 127.0.0.1 --port 7777 --name Athilio
~~~

Para testar com um amigo em outro PC, substitua o IP pelo endereço privado do host na LAN ou na VPN.

| Comando | Função |
| --- | --- |
| /players | Exibir lista de jogadores |
| /ping | Verificar latência |
| /quit | Desconectar |
| Outro texto | Enviar mensagem de teste |

Use **Ctrl+C** para encerrar o servidor.

## Executar testes

~~~powershell
dotnet build Fouroam-Server.slnx --configuration Release
dotnet run --project tests/Fouroam.SmokeTests --configuration Release
~~~

Os testes iniciam um servidor temporário no próprio computador, conectam quatro clientes, verificam a recusa do quinto, comunicação e liberação de vaga. O GitHub Actions também compila e executa esses testes em Windows e Linux.

## Próximas etapas

Primeiro vamos validar a comunicação e reforçar a segurança. A sincronização em tempo real será estudada depois. **Não há funcionalidade de GTA V nem garantia de suporte a missões oficiais.** Qualquer integração com o jogo depende de avaliação técnica e das permissões aplicáveis.

Veja o [ROADMAP](ROADMAP.md), a [arquitetura](docs/ARCHITECTURE.md) e como [contribuir](CONTRIBUTING.md).

## Aviso legal

Projeto independente e não oficial, sem afiliação com Rockstar Games, Take-Two Interactive, Valve ou Epic Games. GTA V e seus conteúdos pertencem aos titulares correspondentes. A licença MIT cobre somente o código e a documentação originais do projeto.

## Licença

[MIT](LICENSE). Copyright (c) 2026 Fouroam-Dev.
