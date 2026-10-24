# ARTE — direção de arte e pipeline de sprites

> Versão 0.2 (23/out/2026). **Decidido: estilo REALISTA, sprite de 96 px** (Aulo realista aprovado pelo João; reserva: 80 px; confirmação final da escala no M0). Origens, companheiros e paleta ainda dependem de ver os personagens. **A lista de todas as imagens a gerar e os prompts estão em `ELENCO_IMAGENS.md`.**

---

## 1. Visual geral

- **Câmera diagonal estilo Hades**, com pixel art de resolução média e iluminação em 3D (HD-2D): mais pixelizado que o Hades (pintado, sem pixels visíveis), menos que o Octopath Traveler.
- Personagens e sombras são **sprites em pixel art em pé** num cenário 3D low-poly com texturas em pixel art; luz e névoa dão o clima.
- **A arte do Damnatio é só referência.** Os personagens foram redefinidos do zero para poder fazer os sprites.

## 2. Regras fixas (a testar no M0)

1. **Uma só densidade de pixel** para sprites e texturas do cenário. Pixels de tamanhos muito diferentes entre 2D e 3D ficam estranhos.
2. **Tamanho do sprite: realista, 96 px de altura** (decidido; reserva 80 px). Rosto legível a partir de ~80 px. Numa base 960×540 ampliada 2×, 96 px ≈ 18% da altura da tela. O estilo chibi 48 px foi testado e descartado por preferência do João. Custo: ~4,5 mil px por quadro; reduzir com roupas em camadas, andar com 4 a 6 quadros e hurt/idle por código.
3. **Só 2 direções desenhadas** (nordeste e sudeste); as outras duas saem por espelhamento.
4. **Poucos quadros por ação:** parado 4, andar 6, demais ações 3 a 6. **Cerca de 40 a 55 quadros por personagem humano.** Reduzir quadros com animação por código (inclinação, balanço, deslocamento) sempre que possível.
5. **Retratos de diálogo 64×64** (companheiros, com 3 a 4 expressões). O retrato do protagonista é um **mosaico** (efeito de shader sobre o mesmo retrato).
6. **Escala de trabalho:** ordem de grandeza de **1.500 a 2.000 quadros** no total (protagonista com 5 roupas, 5 companheiros, ~15 a 20 sombras nomeadas, 6 chefes maiores, 6 corpos-base de sombras comuns, ícones dos ~65 itens).

### 2.1 Teste de tamanho e densidade (M0) — **montado, decisão pendente**

> Atualização (24/out/2026): o tamanho foi decidido fora deste teste (sprite de mapa com 56 px, retrato com 96 px; ver 7.8). A fileira 32/48/64 ficou como registro. O andar do Aulo e da Ismena em 56 px está em `src/Lete.Game/Sandbox/WalkTest.tscn` (1 pixel de arte = 3 px de tela a 1080p). **Continua em aberto:** o modo de render e o enquadramento.

A cena de teste (`src/Lete.Game/World/TestRoom.tscn`) aplica **uma só densidade de pixel** a sprites e texturas do cenário (`Lete.Core/Presentation/PixelDensity.cs`):

- **Enquadramento fixo:** personagem de **1,75 m** ocupa **192 px de altura numa tela 1080p** (câmera ortográfica com ~9,8 m de altura visível). Nos três tamanhos o personagem tem a mesma altura na tela; muda só quantos pixels ele tem. 192 é múltiplo de 32, 48 e 64, o que permite o viewport de baixa resolução com fator inteiro (6, 4 e 3).
- **Texturas do cenário** usam mapeamento triplanar no espaço do mundo, com a mesma quantidade de texels por metro dos sprites.
- **Fileira 32/48/64** ao lado do ponto de teleporte "Fileira"; o protagonista usa o tamanho escolhido no painel de debug (F1).
- **Dois modos de render** (painel de debug): *viewport em baixa resolução* (a cena 3D é renderizada pequena e ampliada com vizinho mais próximo; tudo fica na mesma grade de pixels) e *render direto* (resolução cheia; bordas 3D lisas, sprites pixelizados à parte).
- Comparação pronta em `art/testes_m0/m0_comparacao.png` (e os seis prints soltos na mesma pasta). Ainda com **sprites placeholder**, não com Aulo e Ismena.

Observação do teste: no modo de baixa resolução, qualquer sprite com densidade diferente da escolhida fica reamostrado (ver a fileira). Isso é a regra 1 acima na prática: escolher o tamanho também fixa a densidade de todo o cenário.

**Falta decidir (João):** tamanho do sprite, modo de render, e se o enquadramento (192 px a 1080p) e o ângulo da câmera (inclinação 35°, giro 45°) estão bons. Ver `PENDENCIAS.md`.

**Sprites e texturas placeholder:** gerados por `tools/placeholders.py` (o "conceito" do protagonista passa pelo `pixelizar.py`, como uma imagem do Gemini). Ficam em `art/sprites/placeholder/` e `art/tiles/`; substituir quando houver arte real.

## 3. Paleta "cerâmica dos mortos"

Paleta curta e terrosa, com um acento por região, que lembra as figuras em vasos usadas no Damnatio. **É uma paleta de partida, não a final.**

| Nome | Hex | Uso |
|---|---|---|
| Osso | `#D9CBB0` | Tons claros, pele, tecido |
| Terracota | `#B5573B` | Argila, couro, calor |
| Romã | `#8C1F2E` | **O único vermelho forte do jogo** (Marca e romã) |
| Carvão | `#25222B` | Sombras, contornos, fundo |
| Água do Lete | `#4E7F86` | Água, névoa, esquecimento |
| Ouro | `#C8A24A` | Relíquias, brilho, Elísios |

**Regra:** a romã é o único vermelho forte, para que a Marca sempre se destaque. **Cuidado:** o Veterano e Aulo usam "vermelho desbotado"; mantê-lo dessaturado (mais terracota que romã) para não competir com a Marca. **Acento por região** (exemplos): azul-gelo do Cocito, laranja do Flegetonte, dourado de Elísios; os demais são TBD.

## 4. Protagonista

- **Aparência escolhida como "como você se lembra"** (diegética, no reflexo da poça em Tênaro). Ver `GDD.md` seção 9.
- **v1:** troca de paleta por shader para pele, cabelo e olhos; **3 ou 4 estilos de cabelo** (camada); **3 marcas visíveis** (cicatriz, tatuagem, nenhuma; camada pequena); **roupa definida pela origem**; **uma silhueta de corpo**. Gênero escolhido no início (efeito no corpo é decisão pendente, `PENDENCIAS.md`).
- **A Marca altera o sprite:** dedos e lábios manchados de romã, perda de cor, e nas faixas altas sem sombra e sem pegadas.
- **Retrato:** mosaico cujas pedras se preenchem conforme as Lembranças crescem.
- Todos levam a **Tabuinha Rasurada** no cinto.

### Origens (silhueta e cores)
- **Pescador:** túnica curta, corda e rede no ombro, pés descalços; azul-esverdeado desbotado.
- **Sacerdote:** manto longo, faixa de fita na cabeça, cesto de oferendas; osso e ocre.
- **Veterano:** couraça de couro gasta, ombreiras, capa curta, cicatriz; vermelho desbotado e bronze.
- **Artesão:** avental de couro, mangas arregaçadas, mãos manchadas; terracota e marrom.
- **Estrangeiro:** capa de viagem com capuz, bolsa, sem insígnias; areia e cinza.

## 5. Companheiros (uma cor dominante e uma silhueta por pessoa)

As cores não se repetem, para os cinco serem reconhecíveis de longe no mapa.

| Companheiro | Silhueta | Cores |
|---|---|---|
| Aulo, o Soldado | Armadura de legionário velha, elmo sob o braço, postura ereta | Vermelho (desbotado) e ferro |
| Ismena, a Carpideira | Véu escuro, ombros caídos, mãos abertas | Preto e azul-gelo |
| Dimas, o Saqueador | Saco às costas, lanterna e gancho, roupa remendada | Marrom e dourado sujo |
| Septímia, a Vestal | Túnica longa e véu claros, lamparina acesa | Branco e laranja de chama |
| Cálias, o Aedo | Lira nas costas, único manto colorido (desbotado), coroa de louros murchos | Oliva e roxo |

## 6. Sombras e chefes

- **Silhueta translúcida** que se desfaz embaixo, com a tonalidade da região e do que a prende.
- **Sombras nomeadas** são únicas, com gesto e adereço do ofício em vida (a tecelã com fios, o padeiro com a pá).
- **Sombras comuns** usam **6 corpos-base** com troca de adereço e de paleta.
- **Chefes** são maiores, com formas próprias.

## 7. Pipeline de sprites (Gemini → jogo)

O Gemini (plano básico, com limite de uso) serve para **conceito e referência em alta resolução**; ele **não** entrega pixel art consistente quadro a quadro. Fluxo:

1. **Gemini:** uma imagem por personagem, de corpo inteiro, em fundo magenta liso (`#FF00FF`), vista 3/4 voltada para a frente-direita. Não pedir animação nem spritesheet ao Gemini.
2. **`tools/pixelizar.py`:** remove o fundo e gera o sprite em 32, 48 e 64 px (para o estilo realista usar `--alturas 80 96 --cores 28`), mais folhas de comparação.
3. **Acabamento manual** em Aseprite, LibreSprite ou Pixelorama: olhos, mãos, contorno, limpeza de cores.
4. **Animação:** poucos quadros feitos à mão e/ou animação por código; exportar **folha de sprites** (PNG + JSON de quadros) para o Godot.
5. **Variações** (cabelo, roupa, paleta) por troca de paleta e camadas, sem nova geração.

### 7.1 Uso do `pixelizar.py`

Requer Python 3.8+ e `pip install pillow`.

```
python tools/pixelizar.py aulo.png
python tools/pixelizar.py aulo.png ismena.png --alturas 32 48 64
python tools/pixelizar.py aulo.png --cores 12
python tools/pixelizar.py aulo.png --paleta "#D9CBB0,#B5573B,#8C1F2E,#25222B,#4E7F86,#C8A24A"
```

Saída (pasta `saida_sprites`): `<nome>_<altura>px.png` (sprite real, fundo transparente), `<nome>_comparacao.png` (tamanhos lado a lado ampliados), `<nome>_na_tela.png` (os tamanhos numa tela 1920×1080 para sentir a escala).
Ajustes: `--tolerancia` (fundo ligado à borda, padrão 90; subir se sobrou borda magenta, descer se sumiu parte do personagem), `--tolerancia-interna` (fundo preso, 40), `--erosao` (pixels comidos na borda, 1), `--zoom`, `--zoom-tela`, `--cores` (16), `--paleta`.

Testada com imagens reais do Gemini (Aulo): usar `--cores 28` no estilo realista; há remoção de respingo magenta nas bordas.

### 7.2 Modelo de prompt para o Gemini

```
Pixel art character concept, full body, standing, 3/4 view facing front-right,
on a flat solid magenta (#FF00FF) background, no shadow, no ground.
Sober Greco-Roman dark fantasy. Slightly stylized proportions (head about
1/4 of body height), simple readable silhouette, hard pixel edges,
no anti-aliasing, no gradients, no dithering, maximum 16 colors.
Palette: bone #D9CBB0, terracotta #B5573B, pomegranate #8C1F2E,
charcoal #25222B, Lethe water #4E7F86, gold #C8A24A.
Character: [DESCRIÇÃO]
```

Descrições dos dois personagens do primeiro teste:
- **Aulo:** weathered middle-aged legionary, red and iron armor, helmet held under one arm, short graying hair, short sword at the belt.
- **Ismena:** mourner with a long black veil, dark dress with ice-blue trim, drooping shoulders, open hands, small cloth in one hand.

**Primeiro teste:** gerar **Aulo** (muito detalhe) e **Ismena** (silhueta simples) e rodar em 32 / 48 / 64 px; decidir o tamanho olhando `_na_tela.png`.

### 7.3 Economizar o limite do Gemini básico
Uma geração por personagem; usar a primeira imagem aprovada como referência de estilo nas seguintes ("same style as this image"); guardar os prompts em arquivo de texto; variações por paleta e editor, não por nova geração. A cota diária exata do plano do João não foi verificada.

### 7.4 Transparência sobre IA
Os conceitos são gerados com IA e depois refeitos/limpos à mão. **Conferir as regras de divulgação de arte gerada por IA do itch.io** (ou de onde publicar) antes de publicar.

### 7.5 Folhas de animação (`tools/folha_para_quadros.py`)

Corta uma folha de animação do Gemini (grade de quadros em fundo magenta), **alinha os quadros pelos pés e pelo eixo do tronco**, aplica **uma única paleta para todos os quadros** (as cores não mudam entre quadros) e gera: um PNG por quadro, uma folha horizontal para o Godot, uma folha de comparação e um **GIF** para ver o movimento. Usa o `pixelizar.py` (ficam na mesma pasta).

```
python tools/folha_para_quadros.py aulo_andar.png --colunas 3 --linhas 2 --alturas 48 64 --cores 28 --fps 8
```

Teste feito com o Aulo realista (folha de caminhada de 6 quadros): rosto, armadura e cores ficaram consistentes entre os quadros; com 16 cores a paleta única estragou a cor (armadura marrom), com 28 ficou boa. O Gemini pode devolver quadros a menos ou encostados: pedir grade 3×2 com margem larga (prompt na conversa de arte).

**Direções (decidido, 23/out/2026):** só **duas vistas desenhadas**, de frente (para baixo/direita) e de costas (para cima/direita); as outras duas saem por espelho. No combate o grupo aparece **de costas** (embaixo à esquerda) e a sombra **de frente, espelhada** (em cima à direita).

### 7.6 Padrão de ciclo de andar: pernas alternando (`tools/montar_ciclo_pernas.py`)

> **Presets salvos em `tools/ciclos/`** (um JSON por personagem e vista). **Aulo de costas (aprovado em 23/out/2026):** `python tools/montar_ciclo_pernas.py --config tools/ciclos/aulo_costas.json`. Para um novo personagem, copie `tools/ciclos/_modelo.json` (traz o passo a passo) e ajuste. (Os arquivos do Aulo antigo foram apagados; o Aulo oficial é o simétrico em `aulo_sim/`.)

**Problema:** o Gemini desenha o andar com uma perna só se mexendo (parece "sapateado"). Pedir de novo melhora pouco. **Padrão para TODOS os personagens:** usar o Gemini só para as **poses** e montar a **alternância** no código.

Receita (Aulo de costas, testada):
1. Gerar a folha 3×2 e rodar `folha_para_quadros.py` (96 px, `--cores 28`) para ter os quadros alinhados com paleta única.
2. Olhar os 6 quadros e escolher: **tronco** (um quadro parado e centrado), **contato** (passo mais largo) e **passagem** (uma perna levantada).
3. Rodar:
```
python tools/montar_ciclo_pernas.py <pasta_quadros> --prefixo <nome>_96px --tronco 4 --contato 1 --passagem 3 --troca 5,2,3,6 --contatos 3,7 --corte 72 --sobra 8 --sobra-x 39 --fps 8 --saida <pasta>
```
4. O script usa **um tronco fixo** e as pernas dos quadros escolhidos, **na ordem dada em `--pernas`**, **sem espelhar**. **Regra: nunca espelhar as pernas de personagem que anda na diagonal** (cabeça e tronco virados para um lado): os pés passam a apontar para o lado oposto e parece que ele anda para a esquerda num quadro e para a direita no outro (erro cometido e corrigido em 23/out/2026). O modo antigo com `--contato/--passagem` (espelhado) só serve para vistas exatamente frontais ou de costas, sem virada. O tronco desce 1 px nos contatos (balanço).
5. `--corte` é a linha da barra da saia/túnica (acima do joelho). `--sobra`/`--sobra-x` preservam a ponta da espada (ou outro objeto que passa do corte). `--inverter` troca a ordem se parecer ao contrário.
6. Conferir o GIF: pés alternam, nada "pula", saia e espada inteiras. Ajustar `--corte`.

**Modo recomendado: `--troca` (testado, medido).** A folha do Gemini costuma ser **3 poses repetidas duas vezes**: só uma perna dá o passo (o pé direito ia a x≈37 duas vezes por ciclo e o esquerdo quase não saía do lugar; parece o personagem indo "para frente e para trás"). `--troca 1,2,3` usa as 3 primeiras poses como 1ª metade e monta a 2ª metade **trocando as pernas de lugar (esquerda↔direita) sem espelhar**, Cada perna é **encaixada pela coxa no quadril do lado certo** (o quadril é a média do centro da coxa nos quadros usados) e o topo da coxa é esticado para baixo da saia (sem fenda); restos da espada são removidos (maior grupo conectado). Erro corrigido em 23/out/2026: com deslocamento fixo (`--d`) a perna saía do corpo e o corte ficava visível. `--d` não é mais usado. Resultado: cada pé desliza para trás por 3 quadros e balança para frente por 3, defasados em meio ciclo. **Escolha das poses (regra):** a pose de passo largo (contato com o pé muito à frente, ex.: quadro 1 do Aulo) **não pode ser trocada de perna**: o pé atravessa na frente da outra perna e a perna parece dobrada para frente (erro corrigido em 23/out/2026). Antes de montar, gerar `--troca` com TODOS os quadros (ex.: `1,2,3,4,5,6`), olhar as 12 poses e usar só as que ficam naturais nas duas versões. No Aulo: `5,2,3,6` (perna esquerda balança para frente e pousa; direita desliza para trás) → ciclo de 8 quadros a 8 fps. Passo curto, mas sem pose impossível.

**Andar de FRENTE (Aulo) = modo `--originais` (sem cortar).** No andar de frente as pernas ficam coladas e a espada/bainha cruza a coxa esquerda; a troca de pernas deixava as coxas **cortadas** (separação vertical por dentro da coxa) e o passo assimétrico (22 px contra 8 a 10 px). Tentativas descartadas em 23/out/2026: `--troca` com `--bainha`/`--topo-pele`/`--zona-coxa` (opções continuam no script, mas não usar para frente). **Solução adotada:** `--originais 4,1,6,3,5,2` só **reordena os quadros inteiros** do Gemini (corpo e pernas intactos, sem nenhum corte), escolhendo a ordem em que o pé de apoio desliza para trás de forma contínua (medir o x do pé). Preset: `tools/ciclos/aulo_frente.json` (6 quadros, 7 fps). **Regra geral:** usar `--troca` quando as pernas ficam bem separadas (andar de costas); usar `--originais` quando ficam coladas ou há objeto cruzando a coxa. **Limite:** o movimento das pernas é o do Gemini (perna esquerda sempre atrás); para movimento melhor pedir nova folha ao Gemini.

**Como validar sempre:** medir o x médio de cada pé por quadro; cada pé deve ter UMA fase de recuo contínuo e UMA de avanço, e os dois defasados. Se um pé aparecer duas vezes à frente no ciclo, está errado.

**Mais quadros sem espelho (modo antigo, não recomendado):** os quadros 5 e 6 da folha repetem 2 e 3 com pequenas diferenças, então servem como intermediários. Ordem usada no Aulo (ciclo de 6): `1,2,5,4,3,6` (`--contatos 1,4` = quadros em que o tronco desce). 6 quadros a 6 fps = 1 s por ciclo (2 passos/s).

**Velocidade:** o ciclo de 4 quadros a 6 fps é rápido demais (3 passos/s). Usar `--fps 4` (2 passos/s). **Não usar `--transicao` com poses repetidas:** com a folha atual do Gemini ele repete a mesma perna levantada em dois quadros seguidos e a perna parece saltar. Só vale se houver um quadro de transição realmente diferente (perna balançando para frente). **Sempre conferir as pernas ampliadas (zoom 8) quadro a quadro antes de aprovar:** contato → perna de trás levanta → contato oposto → outra perna levanta.

Limites: sem balanço de braços (o tronco é fixo); luz das botas espelha (quase imperceptível a 96 px); personagens de capa/manto longo escondem as pernas e podem dispensar o ciclo de 6 quadros do Gemini. Para andar de frente usar a mesma receita.

## 8. Lista do que precisa ser desenhado (ordem de grandeza)

Protagonista (base + 5 roupas + 3–4 cabelos + 3 marcas), 5 companheiros (+ retratos 64×64 com 3–4 expressões), 15–20 sombras nomeadas, 6 corpos-base de sombras comuns (com adereços e paletas), 6 chefes + 1 opcional, ~65 ícones de itens, 5 gestos (UI), HUD de combate, telas de menu (Diário, Mosaico, Bolsa, mapa), tiles/texturas por região (~15 regiões, cada uma com acento de cor), modelos 3D modulares low-poly por região, efeitos de névoa, a romã (evolução visual da Marca), Trívias e estruturas (tochas), anéis de juramento no braço.

## 9. Interface (a definir na arte)

Menu principal, tela de início (origem, gênero, aparência, modo), Diário, Mosaico (mosaico sem moldura, pedras por tipo com cor), Bolsa/alforje, equipamento, mapa que se preenche, opções, combate (layout em `GDD.md` 4.7), julgamento, despedida no portão. Fonte legível, tamanho ajustável, opção para daltonismo.

### 7.7 Andar de frente (pernas coladas): receita aprovada (Aulo, 12 quadros)

Quando o `--troca` corta as coxas (personagem de frente, pernas coladas), use `tools/refazer_pernas_opostas.py`: separa as duas pernas abaixo da barra da saia (componentes conectados; se estiverem coladas, divide numa coluna de vale), troca uma pela outra no quadril e gira cada uma em torno do quadril para manter a passada. Giro maximo 12 graus nos contatos e ~27 nas passagens (mais que isso a perna da frente parece quebrada). Ciclo = 6 quadros originais + 6 trocados = 12 quadros, 10 fps. Preset em `tools/ciclos/aulo_frente_12.json`; o script de lote do Aulo é `tools/refazer_pernas_lote_aulo_frente.py` (copiar e ajustar `ordem` e o arquivo de entrada para outros personagens). Aulo de costas continua com `tools/ciclos/aulo_costas.json` (aprovado).

> Nota (Ismena): personagens muito escuros (preto + detalhe azul-gelo) perdem o detalhe colorido com `--cores 28` (o azul virou cinza). Use `--cores 56` e pixelize frente e costas JUNTAS (folha_para_quadros com as duas imagens lado a lado, `--colunas 2`) para que as duas dividam a mesma paleta; separadas, o azul vira cinza na costas.

### 7.8 Dois visuais por personagem (decisão 2026-10-24)

- **Retrato detalhado** (inventário, menu, diálogo, tela cheia): a imagem do Gemini, 96 px ou maior.
- **Sprite de andar** (mapa): ~56 px de altura (referência: JRPG estilo Suikoden II, personagens de ~57 px com muito detalhe e cores bem distintas entre si). Feito por `tools/render3d/` (esqueleto 3D + marionete do sprite pintado) e reduzido com `tools/sprite_andar.py` (redução suave + nitidez + contorno escuro de 1 px, 32 cores). Reduzir direto com `pixelizar.py` vira mancha; usar sempre `sprite_andar.py`.

### 7.9 Receita completa do personagem (marionete 3D, perfis, 56 px)
Ver `docs/PIPELINE_PERSONAGEM.md` (feita com o Aulo; vale para todos os personagens de pernas visíveis).
