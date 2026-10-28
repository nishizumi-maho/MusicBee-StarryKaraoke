# Starry Karaoke: plugin do MusicBee (design)

Plugin novo e independente do Desktop Lyrics. Tem projeto, repositório e DLL próprios
(`mb_StarryKaraoke.dll`) e não toca no Desktop Lyrics nem no Lyrics Panel (BETA).

## O que faz

- **Janela:** abre em *View > Starry Karaoke*. É uma janela normal redimensionável, com F11 ou
  duplo clique para tela cheia e Esc para sair dela. A posição e o tamanho ficam salvos.
- **Visual:** o mesmo do vídeo da NOX LUX.
  - Céu noturno em degradê, com estrelas piscando.
  - No topo, "✦ título — artista ✦".
  - Linha atual no meio, com as anteriores e as próximas apagadas acima e abaixo.
  - `♪ ♪ ♪` pulsando nos intervalos instrumentais e contagem regressiva antes da primeira linha.
  - Barra de progresso dourada embaixo.
- **Letra:** é a que o MusicBee carrega (`NowPlaying_GetLyrics`), num parser próprio de LRC:
  - LRC simples e vários timestamps por linha;
  - `[offset:]`;
  - LRC com tempo por palavra (`<mm:ss.xx>`, como o LYRICS BOCA gera), que acende palavra por
    palavra;
  - sem tempo por palavra, o brilho percorre a linha num tempo estimado.
- **Romaji:** usa o RomajiCore com NMeCab, a mesma engine do Desktop Lyrics, e o dicionário que já
  está em `Plugins\RomajiLyrics\IpaDic`. O romaji segue o tempo das palavras.
- **Japonês:** é uma configuração, com três opções: só romaji, japonês com romaji embaixo, ou só
  japonês.

## Transições

- **Borda suave:** o preenchimento dourado avança com a borda esfumada (máscara em degradê) e um
  glow, em vez de um corte seco.
- **Fade na troca de linha:** a linha que entra clareia e a que sai escurece aos poucos (cerca de
  350 ms).
- **Linha atual cresce:** a linha cantada aumenta até cerca de 108% e volta ao tamanho normal
  quando termina.
- **Rolagem macia:** as linhas deslizam para a nova posição com ease-in-out.

## Configurações

Ficam numa janela própria, aberta pelo botão direito na janela ou pelas preferências do plugin no
MusicBee:

- fonte e tamanho;
- cores: aceso, apagado, glow e fundo;
- modo do japonês;
- quantidade de estrelas, ligar ou desligar as estrelas;
- velocidade das transições;
- atraso da letra (ms);
- mostrar ou esconder o cabeçalho e a barra de progresso.

As configurações são salvas em JSON na pasta de dados do MusicBee.

## Como é feito

- .NET Framework 4.8, como o Desktop Lyrics.
- A janela é **WPF**, que desenha com aceleração de hardware. Glow (BlurEffect), máscara de
  opacidade em degradê, escala e fade saem lisos a 60 fps mesmo em tela cheia. Com GDI+, como no
  Desktop Lyrics, isso ficaria pesado.
- O RomajiCore entra como cópia em `external/RomajiCore`, para o repositório se manter sozinho.
- Testes unitários (xunit) para o parser, os tempos por caractere e o romaji por palavra.
- `build.ps1` gera o zip, com o conteúdo pronto para ir na pasta Plugins.
