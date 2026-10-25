# PENDENCIAS — o que falta decidir ou produzir

> Atualizar este arquivo sempre que uma decisão for tomada (mover para o GDD) ou surgir uma dúvida nova.
> **Regra:** nenhum número de balanceamento deve ser tratado como definitivo antes da fase de números (M5). Usar placeholders marcados `_tbd`.

---

## 1. Decisões em aberto (precisam do João)

| # | Tema | Situação | Proposta (se houver) |
|---|---|---|---|
| 1 | **Gênero e silhueta** | O João escolheu **escolher o gênero no início**. Falta decidir se o gênero muda só o **tratamento nos textos** ou também o **corpo do sprite** | Recomendado: só texto (um corpo, aparência por cabelo e marcas). Dois corpos dobram os trajes por origem |
| 2 | **Tamanho do sprite** | ✅ **Decidido (23/out/2026): realista 96 px** (reserva 80 px). Depois (24/out/2026): **sprite de mapa com 56 px** e retrato com 96 px ou mais (`ARTE.md` 7.8). Confirmar a escala no M0 dentro do cenário 3D | Ver `ARTE.md` e `ELENCO_IMAGENS.md` |
| 3 | **Maré: granularidade** | O GDD define 5 degraus por marco, **mais** avanço "pequeno" por viagem entre Trívias **e** 1 ou 2 degraus por derrota. Os dois usos de "degrau" precisam de uma unidade comum | Proposta: Maré como valor numérico (`TideValue`); marcos somam 1,0; derrota soma 0/1/2 por modo; viagem soma uma fração (TBD). Tabelas por região reagem ao **degrau inteiro** |
| 4 | **Paleta final** | A "cerâmica dos mortos" é paleta de partida; o João quer **ver** os personagens antes de aprovar | — |
| 5 | **Origens, companheiros e sombras visuais** | O João quer ver imagens reais antes de aprovar | Primeiro teste: Aulo e Ismena |
| 6 | **Nomes** | Aulo, Ismena, Dimas, Septímia, Cálias são **provisórios**; o **título do jogo** também | — |
| 7 | **Plano de cortes** | Ordem proposta em `GDD.md` seção 11; falta o João confirmar | Começar com 2 origens e 1 companheiro |
| 8 | **Idiomas** | Português do BR primeiro; inglês é opcional | Textos já nascem traduzíveis |
| 9 | **Stat por tipo de Lembrança** | A tabela de stats por tipo (Fôlego, Força, Defesa, Lucidez, Precisão) é proposta antiga; também foi decidido que **todos começam com os mesmos números**. Revisar na fase de números | — |
| 10 | **Barca de Caronte** como segunda rede de viagem | Opcional; fica para depois | — |
| 11 | **Fim do Diário e da Tabuinha** | O destino de cada um por desfecho ainda precisa ser escrito | — |
| 12 | **Modo de render** (M0) | ✅ **Decidido (24/out/2026): viewport em baixa resolução** (cenário e sprites na mesma grade de pixels). O render direto continua alternável no debug (F1) só para comparação | Ver `ARTE.md` 2.1 |
| 13 | **Enquadramento e câmera** (M0) | ✅ **Aprovado no teste do andar (24/out/2026):** personagem de 1,7 m com 56 px × 3 = 168 px a 1080p (~10,9 m visíveis na vertical); câmera com inclinação de 35° e giro de 45°. Andar compensa a inclinação (mesma velocidade na tela em todas as direções). **Em teste (24/out/2026):** exploração a 4× (224 px ≈ 20% da tela, alternável 2×–4× com +/- no teste do andar) e **batalha mais perto, 5×** (280 px ≈ 26%) | Ajustáveis no Inspector (`Main` e `CameraRig`) |
| 14 | **Billboard dos sprites** | Hoje o sprite encara a câmera por inteiro (pixels sem distorção) e a sombra é uma elipse no chão, não projetada | A elipse combina com a Marca alta (some a sombra) |
| 15 | **Exportar para Windows** | ~~Modelos não baixados~~ **Resolvido:** modelos 4.7.2 .NET (só Windows) instalados e preset `Windows Desktop` versionado; o `.exe` exportado roda | Falta ícone do jogo (`application/icon`) |

| 16 | **Sensação do Anel** (M1) | Primeiros valores de teste em `content/gestures/anel_basico.json` (encolher 1,2 s, anel começa 3× o círculo, valor zera a 0,25 s do encontro) e limiares do Estige em `content/difficulty/estige.json` (perfeito ≥ 0,8, bom ≥ 0,4), todos `_tbd` | Aprovados como ponto de partida (24/out/2026). Firmeza da sombra de teste (100) e dano do ataque por resultado (30/18/6) também `_tbd`, em `content/shades/sombra_de_teste.json` e `anel_basico.json` |
| 17 | **Arte de batalha** (M1) | ✅ **Decidido (24/out/2026): `pixel_160`**: imagem do Gemini pixelizada a 160 px, exibida a 4× (640 px a 1080p). Arte de batalha separada dos sprites simétricos de mapa (56 px) | Ver `ARTE.md` 2.2 |
| 18 | **Layout do HUD de combate** (M1) | ✅ **Decidido (25/out/2026): layout normal** (caixa de ações estreita embaixo à direita, descrição separada à esquerda dela). O painel grande foi descartado | — |
| 19 | **Números do turno da sombra** (M1) | Primeiros valores de teste, todos `_tbd`: Fôlego 100, Lucidez 60 (`content/battle/teste_ataque.json`); dano no Fôlego por defesa perfeita/boa/falha 0/8/20 no aparo e 0/10/22 na Esquiva (`content/shades/sombra_de_teste.json`); aparo com Anel de 1,0 s (`content/gestures/aparo_basico.json`); Esquiva de 5 s num quadro de 104×64 px da arte, zerando com 4 golpes (`content/gestures/esquiva_ondas.json`); Postura do Aulo a cada 2 rodadas, janelas 0,15 mais largas (`content/companions/aulo.json`) | Afinar jogando; balanceamento final no M5 |
| 20 | **Lia, a Retratista: protagonista jogável** (proposta, 25/out/2026) | Ficha em `docs/LIA_PROPOSTA.md`; arte em geração (Gemini), ainda sem sprites. **Decisões do João:** (1) Lia como **sexta origem** (proposta) ou **protagonista à parte** (história e prólogo próprios, sem escolher origem); (2) rosto e gênero **fixos** (aparência da referência) ou **escolhíveis** como nas outras origens; (3) impacto de escrita: +1 cadeia de 4 capítulos, 3 Lembranças, missão de origem e falas dos 5 companheiros reagindo a ela; (4) o açafrão da túnica fica perto do dourado do Dimas: conferir na tela (o azul-lápis do avental separa); (5) se a Lia for baseada em pessoa real, confirmar com ela antes de publicar (itch.io). Até aprovar, `HISTORIA.md` e `GDD.md` não mudam. **25/out: o João pôs a Lia como protagonista na batalha de teste** (`content/battle/teste_ataque.json`), no lugar da Ismena | Decidir 1 a 5; quando chegarem os sprites, registrar em dado (andar e batalha) |
| 21 | **Remador (rascunho)** (M1) | Escrito pelo Claude para o João revisar (25/out/2026), em `content/shades/remador.json` e chaves `shade.remador.*` de `content/locale/pt_BR.json`: nome **Lícon**; fixação **culpa** (soltou a mão de alguém na água); opções erradas saudade e medo, cada uma com a fala que corrige a pista; ciclo de 3 golpes (remo, ondas, mão estendida); escutas: 1º golpe pista da margem, 2º o nome, 3º a fixação. A arte provisória é o Dimas 3/4 | Revisar nome, fixação, falas e o ciclo |
| 22 | **Lugar do Libertar no menu** (M1) | O GDD 4.7 não lista o botão. Provisório: dentro de **Aproximar** (com Escutar, Oferenda, Presenciar, Poupar), indisponível e explicando o porquê até a Compreensão ficar completa | Confirmar ou mover |
| 23 | **Números de Escutar e Libertar** (M1) | Todos `_tbd`: Segurar enche em 1,6 s com o ponto certo em 75% da barra, zerando a 25% de distância (`content/gestures/escutar.json`); Arrastar num quadro de 120×56 px com a luz a 22 px/s, zerando com desvio médio de 8 px, 20 s no máximo (`content/gestures/arrastar_libertar.json`); paz 100, ganho 50/35/15 por perfeito/bom/falho, custo de 15 de Lucidez por tentativa, golpe 1,5× mais forte depois de um erro (`content/shades/remador.json`) | Afinar jogando |
| 24 | **Proposta de UI: identidade barro e ouro** (implementado para teste) | Pedido do João; detalhes propostos pelo Claude: azul-noite `#1E2430` para o fundo dos painéis (o pedido dizia "o já usado", que era carvão `#25222B`); marfim apagado `#B9AE98` para opção disponível; cinza-barro `#6E5E58` para desativado; fundo da tabuinha `#3A2219`; **perigo em terracota pulsando** abaixo de 30% do Fôlego (no lugar do vermelho cerâmico, para manter a romã como único vermelho forte); rótulos "Fôlego/Lucidez" trocados por ícones na mesma linha da barra (liberou espaço sobre a cabeça do Aulo); todos os ícones e molduras em `art/ui/` são desenhos provisórios gerados por `tools/ui_arte.py` | Aprovar ou ajustar cores; trocar a arte provisória pela definitiva |
| 25 | **Proposta de UI: extras simples** (implementado para teste) | Coroa de louro acima do personagem do grupo que age; tocha acesa ao lado do nome da sombra na vez dela; faísca dourada em todo acerto; aro de ouro que cresce e some no resultado perfeito do Anel (ataque e aparo) | Aprovar ou tirar |
| 26 | **Proposta de UI: extras para depois** (não implementado) | Colunas jônicas discretas nos cantos da tela, em baixo contraste; cartela de barro que vira (flip) mostrando o ícone ao acertar uma Memória (quando a Memória existir); cursor do mouse como cetro dourado, desligável nas opções; opções de acessibilidade **alto contraste** (barro → preto, ouro → branco) e **tamanho de texto** (precisam da tela de opções, que ainda não existe) | Decidir quando houver tela de opções e Memória |
| 27 | **Absorver e Eco** (M1) | ✅ **Decidido (25/out/2026):** Absorver usa o gesto **Segurar** (conter a sombra) e aparece no **menu principal** só com a Firmeza baixa; o Eco ocupa 1 de 3 espaços, vale uma vez por batalha e usa o gesto da sombra de origem (o do Remador é um **Anel duplo**, no ritmo da remada); absorver e usar o Eco sobem a Marca, que só aparece no debug. Proposto pelo Claude: quanto mais fraca a sombra, mais rápido a barra do Segurar enche; o falho também absorve, mas a Marca pesa mais; o mesmo Eco não ocupa dois espaços | Afinar jogando |
| 28 | **Números de Absorver e Eco** (M1) | Todos `_tbd`: absorver com Firmeza até 30%; barra enche em 1,4 s com o ponto certo em 80%, até 60% desse tempo com a Firmeza zerada (`content/gestures/absorver.json`, `content/shades/remador.json`); Marca +1/+1/+2 por perfeito/bom/falho; Eco "Remada": 2 anéis de 0,9 s, Firmeza −45/−30/−12, Marca +1 por uso (`content/gestures/eco_remada.json`) | Afinar jogando; Marca de verdade no M2 |

## 2. Números e balanceamento (por último, como o João pediu)

Dano e Firmeza das sombras; custo de Lucidez por Memória; janelas de perfeito/bom por gesto e por modo; limiares de assistência; economia (óbolos, preços); capacidade das oferendas (rascunho: 5 por tipo); intervalo de apoio do companheiro (rascunho: 2 turnos) e como o vínculo o reduz; curva da Maré por região; valores da Marca por faixa e quanto cada ação soma; efeito de cada Culpa/Leveza; cura de Fogos do Lar e hospitalidade; custo de viagem em Maré; bônus de perfeito; penalidade de gesto automático. Ver `DADOS.md` (campos `_tbd`).

## 3. Conteúdo ainda por listar e escrever

- **Sombras** (~40 a 60; 15 a 20 nomeadas; 6 corpos-base de comuns): lista por região com gesto, fixação, nome e vias.
- **Lembranças** (~45): lista completa (6 comuns por tipo + 3 por origem + 6 Pesadas + incertas/falsas) e a Memória (habilidade) de cada uma.
- **Itens** (~65), com dono, frase de história, afinidade e conjuntos de dono.
- **Juramentos** (8 a 10), **julgamentos** (4 a 6), **missões** (15 a 20), **mercadores por região**.
- **Regras de combate das regiões** ainda indefinidas (só Luto, Tártaro e Elísios estão definidas).
- **Mapas** (layout de cada uma das ~15 regiões).
- **Todas as falas e cenas** (o roteiro em si ainda não existe, só a estrutura).
- **Tutorial** (como o prólogo ensina gestos, vias e oferendas, uma coisa por vez).
- **Lista de relíquias** com a lição (bênção + ônus) de cada uma.
- **Eco** de cada sombra absorvível.
- **Interface:** layout das telas listadas em `GDD.md` seção 10.
- **Áudio:** trilha por região, efeitos, ambiências, créditos.
- **Arte:** conceitos de todos os personagens, sombras e chefes; modelos 3D modulares; texturas.
- **Acessibilidade:** daltonismo e escala de fonte (detalhes).
- **Publicação:** nome, capa, página, trailer, demo, política de arte com IA.

## 4. Pontos de atenção de design (já conhecidos)

- O **hub Asfódelos** precisa mudar a cada retorno.
- **Veterano e Aulo** usam vermelho: manter dessaturado para a romã continuar sendo o único vermelho forte.
- **Custo de escrita** de 5 origens × 4 capítulos + 5 companheiros × 5 capítulos é de todos, embora o jogador veja um de cada por partida.
- **Combinações de estado** (Marca × Maré × Mosaico × Pesadas × juramentos) geram bugs: painel de debug e testes desde o início.
- **Sprites consistentes**: o Gemini oscila entre quadros; manter processo e tamanho padrão.
- **Mnemósine × Lete:** as duas fontes se parecem; o puzzle depende de pistas interpretáveis.
- **Perjúrio por provas falsas** liga Tribunal e Mosaico: garantir que a regra fique clara para o jogador.

## 5. Registro de decisões (resumo, 23/out/2026)

- Tom sóbrio e fantasioso; combate por turnos; ~10h; executável Windows (não web); itch.io.
- Protagonista desceu vivo; Marca irreversível (romã); sem ponte direta com os vivos (só NPCs e relíquias de heróis).
- Asfódelos primeiro; hub; oferendas com partilha; origem importa do começo ao fim; Hécate como guia fixa.
- Origens sem vantagem; 5 origens; 5 companheiros independentes das origens, nenhum extremo.
- Combate: toda ação é um gesto (5 gestos-base, 3 níveis); alternado; companheiro só apoia; dificuldade por regras; derrota com custo real; HUD diagonal/3D; 3 modos travados ao iniciar.
- Mosaico com Chave de Memória (3 de 5 tipos); confirmação cruzada; incertas/falsas; Pesadas definitivas.
- Itens únicos com história (sem loot genérico); ~65 itens; cura sem derrotar sombras; Presenciar renomeado; Juramentos (máx. 3); Testemunhos só no Tribunal.
- 6 chefes; cadeia de 4 capítulos por origem; companheiros com 5 capítulos e oferta final.
- Godot 4 + C# + cenário 3D low-poly com sprites; Windows com controle.
- História: ambição, Venda do Nome, sem vilão externo, só ele sofre; Trocar como final.
- Arte: aparência como "como você se lembra"; gênero escolhido no início; teste de 32/48/64 px; Gemini para conceitos.
