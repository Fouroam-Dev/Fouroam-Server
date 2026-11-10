# Fouroam Server — executáveis Windows (build de desenvolvimento)

## O que você está baixando

O GitHub Actions compila o servidor e o cliente em **Windows x64**, cada um como executável .NET 10 auto-contido. Você **não precisa instalar o .NET SDK** para executar esses pacotes.

Esses arquivos são **builds de desenvolvimento sem assinatura digital**, distribuídos como artefatos temporários do GitHub Actions, **não são uma release estável de produção**. O projeto não oferece conexão ao GTA V nem gameplay cooperativo.

## Baixar os arquivos

1. Abra o repositório e acesse **Actions → Windows EXE (development build)**.
2. Escolha uma execução da branch \`main\` **com resultado verde (Success)**.
3. Baixe os artefatos \`Fouroam-Server-win-x64-dev\` e \`Fouroam-Client-win-x64-dev\`.
4. Extraia os arquivos ZIP em pastas separadas no seu computador.

Os artefatos são mantidos por **7 dias** depois da execução. O GitHub pode solicitar que você faça login para baixá-los.

## Configurar o servidor (somente uma vez)

Abra o **CMD** dentro da pasta que contém \`Fouroam.Server.exe\`.

~~~cmd
Fouroam.Server.exe init
~~~

Escolha nome e senha da sessão. Guarde o **fingerprint TLS SHA-256** exibido.

A pasta privada \`.fouroam\` será criada no diretório atual. Ela guarda a chave privada TLS e as configurações da sessão e **não deve ser publicada nem compartilhada**.

**Se você já criou uma sessão no código-fonte:** não execute \`init\` em uma pasta nova sem necessidade. Você pode manter o servidor executando da pasta antiga, ou mover a pasta **privada** \`.fouroam\` para a pasta do novo executável, somente no seu próprio PC, sem enviá-la a outras pessoas. Verifique sempre o fingerprint resultante.

## Iniciar o servidor

~~~cmd
Fouroam.Server.exe --bind 127.0.0.1 --port 7777
~~~

Para uso **exclusivamente numa LAN ou VPN privada confiável**, com firewall restritivo, use \`--bind 0.0.0.0\`. **Nunca faça port forwarding para a internet pública.**

## Conectar o cliente

Abra o **CMD** na pasta que contém \`Fouroam.Client.Test.exe\`:

~~~cmd
Fouroam.Client.Test.exe --host 127.0.0.1 --port 7777 --name Athilio --fingerprint SEU_FINGERPRINT_SHA256
~~~

Substitua \`SEU_FINGERPRINT_SHA256\` pelo valor de 64 dígitos que apareceu no console do servidor. O programa solicitará a senha sem exibi-la.

## Administrar jogadores

No console do **host**:

~~~text
/players
/status
/kick 1 Teste de expulsao
/kick Athilio Teste de expulsao
/ban 1
/bans
/unban ABCDEF12
/say O servidor esta funcionando
/help
/stop
~~~

A barra inicial é opcional. Para nomes iguais, use o **ID numérico**, pois o console recusa alvos ambíguos.

## Conferir a integridade

Cada pacote inclui \`SHA256SUMS.txt\`, com o hash do executável gerado no CI.

No PowerShell:

~~~powershell
Get-FileHash .\Fouroam.Server.exe -Algorithm SHA256
~~~

Compare com o hash do arquivo \`SHA256SUMS.txt\`. Um hash de integridade não substitui assinatura digital nem prova sozinho a origem do executável. Baixe somente dos Actions do repositório oficial.

## Limitações de segurança

- Senhas com quatro caracteres são permitidas, mas **muito fracas**. Prefira uma senha longa.
- Os banimentos por identidade de instalação podem ser burlados regenerando o arquivo da identidade.
- O projeto não passou por auditoria de segurança nem teste público de carga.
- Este artefato não é um servidor de GTA V; a integração com o jogo ainda não foi implementada.
