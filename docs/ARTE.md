# ARTE — direção de arte e pipeline de sprites

> Versão 0.1 (23/out/2026). **Nada aqui é final.** O João quer **ver os personagens de verdade** antes de dar o veredito sobre origens, companheiros e paleta. O tamanho dos sprites será decidido por **teste visual** (32 / 48 / 64 px).

---

## 1. Visual geral

- **Câmera diagonal estilo Hades**, com pixel art de resolução média e iluminação em 3D (HD-2D): mais pixelizado que o Hades (pintado, sem pixels visíveis), menos que o Octopath Traveler.
- Personagens e sombras são **sprites em pixel art em pé** num cenário 3D low-poly com texturas em pixel art; luz e névoa dão o clima.
- **A arte do Damnatio é só referência.** Os personagens foram redefinidos do zero para poder fazer os sprites.

## 2. Regras fixas (a testar no M0)

1. **Uma só densidade de pixel** para sprites e texturas do cenário. Pixels de tamanhos muito diferentes entre 2D e 3D ficam estranhos.
2. **Tamanho do sprite: ~48 px de altura é a recomendação**, mas o **teste de 32 / 48 / 64 px** decide. Resumo: 32 px dá silhueta e cor mas quase nenhum traço de rosto (difícil distinguir 5 origens e 5 companheiros); 48 px mostra rosto simples (olhos e boca), cabelo, roupa e instrumento; 64 a 72 px dá rosto expressivo mas exige quadros demais para uma pessoa só.
3. **Só 2 direções desenhadas** (nordeste e sudeste); as outras duas saem por espelhamento.
4. **Poucos quadros por ação:** parado 4, andar 6, demais ações 3 a 6. **Cerca de 40 a 55 quadros por personagem humano.** Reduzir quadros com animação por código (inclinação, balanço, deslocamento) sempre que possível.
5. **Retratos de diálogo 64×64** (companheiros, com 3 a 4 expressões). O retrato do protagonista é um **mosaico** (efeito de shader sobre o mesmo retrato).
6. **Escala de trabalho:** ordem de grandeza de **1.500 a 2.000 quadros** no total (protagonista com 5 roupas, 5 companheiros, ~15 a 20 sombras nomeadas, 6 chefes maiores, 6 corpos-base de sombras comuns, ícones dos ~65 itens).

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
2. **`tools/pixelizar.py`:** remove o fundo e gera o sprite em 32, 48 e 64 px, mais folhas de comparação.
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

Limitação conhecida: a ferramenta foi testada com imagem simulada; **ainda não foi testada com imagem real do Gemini**. Ajustar conforme os primeiros resultados.

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

## 8. Lista do que precisa ser desenhado (ordem de grandeza)

Protagonista (base + 5 roupas + 3–4 cabelos + 3 marcas), 5 companheiros (+ retratos 64×64 com 3–4 expressões), 15–20 sombras nomeadas, 6 corpos-base de sombras comuns (com adereços e paletas), 6 chefes + 1 opcional, ~65 ícones de itens, 5 gestos (UI), HUD de combate, telas de menu (Diário, Mosaico, Bolsa, mapa), tiles/texturas por região (~15 regiões, cada uma com acento de cor), modelos 3D modulares low-poly por região, efeitos de névoa, a romã (evolução visual da Marca), Trívias e estruturas (tochas), anéis de juramento no braço.

## 9. Interface (a definir na arte)

Menu principal, tela de início (origem, gênero, aparência, modo), Diário, Mosaico (mosaico sem moldura, pedras por tipo com cor), Bolsa/alforje, equipamento, mapa que se preenche, opções, combate (layout em `GDD.md` 4.7), julgamento, despedida no portão. Fonte legível, tamanho ajustável, opção para daltonismo.
