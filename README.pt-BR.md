<div align="center">

# Fouroam Server

**Sua sessão. Seu computador. Suas regras.**

Servidor de rede open-source e auto-hospedado para o [Fouroam-V](https://github.com/Fouroam-Dev/Fouroam-V), projeto em fase de pesquisa para sessões cooperativas privadas com até quatro amigos.

[![Licença MIT](https://img.shields.io/badge/licen%C3%A7a-MIT-blue.svg)](LICENSE)
![Estágio](https://img.shields.io/badge/status-planejamento-orange.svg)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-lightgrey.svg)

[English](README.md) · [Português](README.pt-BR.md) · [Roadmap](ROADMAP.md) · [Contribuição](CONTRIBUTING.md)

</div>

> [!IMPORTANT]
> **Planejamento / pré-alfa.** Ainda não existe servidor funcional ou executável para download. Os recursos apresentados são objetivos, não funcionalidades prontas.

## Proposta

O Fouroam Server pretende permitir que um jogador hospede uma **sessão privada no próprio computador**, com até três amigos conectados por LAN ou VPN privada, como o Radmin VPN.

Hospedagem em VPS poderá ser opcional, mas não será necessária para sessões locais. O projeto não pretende substituir o GTA Online nem criar um serviço pago de hospedagem.

## Recursos planejados

| Funcionalidade | Situação |
| --- | --- |
| Servidor local Windows | Planejado |
| Até quatro jogadores por sessão | Planejado |
| Conexão por LAN e VPN privada | Planejado |
| Hospedagem opcional em VPS | Planejado |
| Identificação de jogadores e controle de lotação | Planejado |
| Reconexões e logs seguros | Planejado |
| Sincronização de estado em tempo real | Em pesquisa |
| Integração autorizada ao GTA V | Depende de viabilidade e permissões |
| Campanha oficial em cooperativo | Não disponível ou garantida |
| Conquistas oficiais da Steam/Epic | Não suportadas nem prometidas |

## Como deverá funcionar

1. Um jogador inicia o futuro servidor no próprio PC Windows.
2. Os amigos entram na mesma rede local ou VPN privada.
3. Os clientes acessam o IP privado e a porta configurada do host.
4. O host mantém a sessão para até quatro participantes.

Cada participante precisará de uma cópia legítima do jogo e qualquer integração com o GTA V dependerá de autorização.

## Tecnologias propostas

O primeiro protótipo será **independente do GTA V**, inicialmente com **C# e .NET 10**, testes de conexão por **TCP** e avaliação posterior de **UDP** para dados em tempo real.

A arquitetura poderá mudar. **Ainda não existem comandos de instalação ou execução.**

## Documentação

- [Planejamento](ROADMAP.md)
- [Arquitetura](docs/ARCHITECTURE.md)
- [Como contribuir](CONTRIBUTING.md)
- [Código de conduta](CODE_OF_CONDUCT.md)
- [Segurança](SECURITY.md)
- [Issues](https://github.com/Fouroam-Dev/Fouroam-Server/issues)

## Aviso legal

Projeto independente e não oficial, sem vínculo ou endosso da Rockstar Games, Take-Two Interactive, Valve ou Epic Games. Os direitos sobre GTA V e seus conteúdos permanecem com seus titulares. A licença MIT cobre somente os arquivos originais deste projeto. Alterações multiplayer e missões oficiais podem estar sujeitas a restrições e exigem análise das permissões aplicáveis antes de implementação ou distribuição.

## Licença

[MIT](LICENSE), respeitados os direitos de terceiros. Copyright (c) 2026 Fouroam-Dev.
