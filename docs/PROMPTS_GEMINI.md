# PROMPTS_GEMINI — pedidos de conceito para o Gemini

> ⚠️ **Substituído por `ELENCO_IMAGENS.md`** (estilo realista 96 px). Este arquivo fica como histórico do estilo antigo (chibi/16 cores); as linhas CHARACTER continuam valendo.

> Prompts em inglês (o Gemini responde melhor). Cada prompt = **BLOCO BASE** + **bloco do personagem**. Cole os dois juntos, sempre no mesmo formato.
> Fluxo: gerar no Gemini → salvar o PNG na pasta indicada → pedir ao Claude Code para rodar `tools/pixelizar.py` → avaliar `_comparacao.png` e `_na_tela.png`.

---

## 0. Como usar (e gastar pouco do limite)

1. **Comece só com o teste:** Aulo e Ismena (seções 2.1 e 2.2). Decida o tamanho (32/48/64 px) olhando `_na_tela.png` antes de gerar o resto.
2. **Uma geração por personagem.** Só refaça se estiver realmente ruim.
3. **Fixe o estilo depois do primeiro aprovado:** nas gerações seguintes, anexe a imagem aprovada (Aulo ou Ismena) no Gemini e adicione no início do prompt: `Use the attached image only as a style reference (same pixel density, palette mood, proportions and level of detail). Create a NEW character:`
4. **Não peça animação nem spritesheet** ao Gemini. Ele só faz o conceito parado.
5. **Se vier fundo que não é magenta liso, com chão, sombra, moldura ou texto**, escreva na mesma conversa: `Redo it on a flat solid #FF00FF background, no ground, no shadow, no text, no frame.`
6. **Se vier imagem lisa demais (ilustração, não pixel art),** escreva: `Make it look like real low-resolution pixel art: visible square pixels, hard edges, flat colors, max 16 colors.` O `pixelizar.py` também ajuda a "pixelizar" na redução.
7. **Guarde o texto dos prompts** que funcionaram.
8. A cota diária exata do seu plano não foi verificada; teste quantas imagens por dia ele dá.

---

## 1. BLOCO BASE (cole sempre, antes do bloco do personagem)

```
Single full-body pixel art character concept for a 2D video game sprite. Standing idle pose, 3/4 view facing front-right (toward the lower right of the image), centered, the entire body visible from head to feet with empty margin around it.

Background: one flat solid pure magenta color (#FF00FF) filling the whole image. Nothing else: no ground, no floor, no cast shadow, no text, no frame, no border, no grid, no color swatches, no extra characters, no extra props lying around. Do not use magenta anywhere on the character itself.

Style: crisp pixel art with hard 1-pixel edges, visible square pixels, flat colors, no anti-aliasing, no gradients, no dithering, no blur, no glow, no soft shading. Maximum 16 colors in total. Muted earthy palette, like painted Greek vase ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent, if at all.

Proportions: slightly stylized, head about one quarter of the body height, large simple hands and feet, strong simple silhouette that reads clearly at very small size, a few large shapes instead of many tiny details. Simple face: dot eyes, small mouth.

Mood: sober, somber Greco-Roman dark fantasy, worn and weathered ancient clothing, no modern elements, no fantasy glow effects.
```

---

## 2. Companheiros (gerar primeiro Aulo e Ismena, para o teste)

### 2.1 Aulo, o Soldado — salvar em `art/concept/gemini/companheiros/aulo.png`
```
CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.
```

### 2.2 Ismena, a Carpideira — salvar em `art/concept/gemini/companheiros/ismena.png`
```
CHARACTER: Ismena, a professional mourner (ancient Greek lamenting woman). Long black veil covering her hair and falling over her shoulders, dark long dress with ice-blue (#8FB3C9) trim at the hem and sleeves. Drooping shoulders, head slightly bowed, both hands open and relaxed at her sides, a small white cloth in one hand. Pale face, tired sad eyes. Dominant colors: black and pale ice blue.
```

### 2.3 Dimas, o Saqueador — `art/concept/gemini/companheiros/dimas.png`
```
CHARACTER: Dimas, a grave robber, lean and wiry, slightly hunched, shifty but not villainous. A heavy sack slung over his back, a small oil lantern hanging from one hand, a hooked iron tool (grappling hook) at his belt. Patched, mismatched ragged clothes in brown, with dirty gold (#C8A24A, dulled) details: trim, a coin pouch. Wrapped cloth around his hands, a headband. Dominant colors: brown and dirty gold.
```

### 2.4 Septímia, a Vestal — `art/concept/gemini/companheiros/septimia.png`
```
CHARACTER: Septímia, a former Vestal priestess, calm and dignified. Long white tunic and a pale veil (the Roman suffibulum) with a thin band at the head, simple and clean. She carries a small clay oil lamp with a lit flame in front of her in both hands; the flame is a flat orange pixel shape (#E0833A) with no glow. Straight posture, serene but guarded face. Dominant colors: off-white and flame orange.
```

### 2.5 Cálias, o Aedo — `art/concept/gemini/companheiros/calias.png`
```
CHARACTER: Cálias, a wandering Greek bard (aoidos), thin and expressive, middle-aged. A wooden lyre slung on his back. The only colorful garment in the whole cast: a faded olive-green (#7A7F45) mantle (himation) with muted purple (#6B4A7A) trim, worn and sun-bleached. A crown of wilted laurel leaves on his head. Gentle ironic smile, one hand raised slightly as if about to tell a story. Dominant colors: olive and muted purple.
```

---

## 3. Protagonista (uma imagem por origem)

O protagonista é personalizável (cabelo, pele, marcas viram variações **depois**, por paleta e camadas). Por isso, nas imagens-base: **rosto neutro, cabelo curto e liso, sem cicatriz nem tatuagem** (exceto o Veterano, que leva uma cicatriz pela origem). Todos levam a **Tabuinha Rasurada** no cinto.

**Gênero:** o jogo terá escolha de gênero. Para decidir se o corpo muda (decisão pendente), gere **o Pescador nas duas versões** e compare. Troque `[GENDER]` por `male` ou `female`. Se for só texto, o resto das origens pode sair em **uma** versão só.

**Linha comum a todos** (já incluída em cada prompt abaixo):
`Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. A small wax writing tablet with scratched-out writing hangs from the belt.`

### 3.1 Pescador de Tênaro — `art/concept/gemini/protagonista/pescador_m.png` / `pescador_f.png`
```
CHARACTER: the player character, origin "fisherman of Cape Taenarum". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Short coarse tunic in faded teal-green (#4E7F86, desaturated), a coil of rope and a folded fishing net over one shoulder, barefoot, sun-weathered skin, rolled-up sleeves. A small wax writing tablet with scratched-out writing hangs from the belt.
```

### 3.2 Sacerdote — `art/concept/gemini/protagonista/sacerdote.png`
```
CHARACTER: the player character, origin "priest of Demeter and Hecate". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Long ankle-length robe in bone (#D9CBB0) and ochre, a woolen ribbon band (fillet) tied around the head, a small woven basket of offerings held in one hand (bread, a small jar). A small wax writing tablet with scratched-out writing hangs from the belt.
```

### 3.3 Veterano de guerra — `art/concept/gemini/protagonista/veterano.png`
```
CHARACTER: the player character, origin "war veteran". Ordinary face with one thin scar on the cheek, short plain dark-brown hair, [GENDER], sturdy build. Worn leather cuirass with shoulder guards, a short faded cape, a desaturated brick-red tunic (closer to terracotta than bright red), bronze buckles and bracers. No weapon drawn. A small wax writing tablet with scratched-out writing hangs from the belt.
```

### 3.4 Artesão — `art/concept/gemini/protagonista/artesao.png`
```
CHARACTER: the player character, origin "potter and weaver craftsman". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Thick leather apron over a terracotta-brown tunic, sleeves rolled up, hands and forearms stained with clay, a small tool (wooden spatula) tucked in the belt. A small wax writing tablet with scratched-out writing hangs from the belt.
```

### 3.5 Estrangeiro — `art/concept/gemini/protagonista/estrangeiro.png`
```
CHARACTER: the player character, origin "foreigner". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Long traveling cloak in sand and gray with the hood pulled back onto the shoulders, plain worn tunic, a leather shoulder bag, dusty boots, no insignia or decoration of any kind. A small wax writing tablet with scratched-out writing hangs from the belt.
```

---

## 4. Sombras (segundo lote, depois de decidir o tamanho)

Sombras são **translúcidas no jogo**, mas no Gemini peça **opacas** (a translucidez entra depois por shader). O fundo continua magenta.

### 4.1 O Remador (sombra-tutorial) — `art/concept/gemini/sombras/remador.png`
```
CHARACTER: the shade (ghost) of a drowned oarsman, standing. Pale ghostly gray-teal body drawn as a solid opaque figure (no real transparency), the lower legs dissolving into pixelated wisps and fading to nothing at the bottom. Torn short tunic, a long wooden oar held in both hands like a staff, water-stained, hollow dark eyes, resigned posture. Dominant colors: pale Lethe teal and gray.
```

### 4.2 A Tecelã (Campos do Luto) — `art/concept/gemini/sombras/tecela.png`
```
CHARACTER: the shade of a weaver woman, standing. Pale ghostly blue-gray solid opaque figure, the lower body dissolving into pixel wisps. Long dress, hands raised in front of her pulling taut a few thin pale threads between her fingers, hair tied back, patient waiting expression. Dominant colors: pale gray-blue (#8FB3C9) and bone.
```

### 4.3 O Padeiro (Rua dos Ofícios) — `art/concept/gemini/sombras/padeiro.png`
```
CHARACTER: the shade of a baker, stocky man, standing. Pale ghostly warm-gray solid opaque figure with faint ochre flour-dust tones, the lower body dissolving into pixel wisps. Apron, rolled sleeves, hands mid-kneading motion in front of his belly, a long wooden bread peel leaning on one shoulder, vague absent expression. Dominant colors: bone and pale ochre.
```

---

## 5. Pastas e comando para o Claude Code

```
art/concept/gemini/
├── companheiros/   aulo.png  ismena.png  dimas.png  septimia.png  calias.png
├── protagonista/   pescador_m.png  pescador_f.png  sacerdote.png  veterano.png  artesao.png  estrangeiro.png
└── sombras/        remador.png  tecela.png  padeiro.png
art/sprites/        (saída do pixelizar, por categoria)
```

Pedido pronto para colar no Claude Code:

```
Rode tools/pixelizar.py nas imagens novas de art/concept/gemini/<categoria>/ com alturas 32, 48 e 64, salvando em art/sprites/<categoria>/. Olhe os arquivos _comparacao.png e _na_tela.png de cada imagem e me diga: (1) se o fundo magenta saiu limpo, sem halo; (2) se há pontos soltos ou bordas sujas; (3) qual altura mantém melhor o rosto e os objetos-chave (elmo, véu, lira, lamparina). Se algo estiver ruim, ajuste --tolerancia, --erosao ou --cores e rode de novo. Não altere as imagens originais.
```

Para a **primeira rodada de teste** use só: `aulo.png` e `ismena.png`.
