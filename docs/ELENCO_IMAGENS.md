# ELENCO_IMAGENS — todas as imagens que o Gemini precisa gerar

> Versão 0.1 (23/out/2026). Registro mestre: **quem** precisa de **quais imagens**, com o estilo e os prompts.
> **Decisão (23/out/2026): estilo REALISTA, sprite de 96 px de altura** (Aulo realista aprovado pelo João). Pipeline: `pixelizar.py` / `folha_para_quadros.py` com `--cores 28`.
> Chamada no chat: o João diz **"kit gemini"** (ou "kit do <personagem>") e o Claude entrega, para o que for pedido, os prompts prontos para copiar, na ordem certa, em blocos de código.

---

## 1. Decisões que este arquivo assume

1. **Realista, 96 px** de altura final. Cabeça ≈ 1/7 a 1/8 do corpo (proporção humana), rosto com olhos, nariz e boca legíveis.
2. **Só 2 vistas desenhadas:** **frente** (sudeste, voltado para baixo/direita) e **costas** (nordeste, voltado para cima/direita). As outras duas saem por **espelho** no Godot. Sem vista lateral pura.
3. No combate: grupo **de costas** (embaixo à esquerda), sombra **de frente, espelhada** (em cima à direita).
4. **Cuidado com espelho:** objetos carregados trocam de lado (elmo do Aulo, lira do Cálias). Aceitável; evitar assimetria forte onde o lado importa (ex.: Marca na mão).
5. Gemini **não** devolve grade de pixels real (blocos ~2,5 a 4,7 px, não inteiros). Por isso sempre passa pelo pixelizador.
6. A decisão final de escala (96 px numa base 960×540 ampliada 2× ≈ 18% da altura da tela) se confirma no **M0**, com o sprite dentro do cenário 3D. Alternativa de reserva: 80 px.

## 2. Tipos de imagem (códigos)

| Código | Imagem | Fundo / formato | Vira |
|---|---|---|---|
| **F** | Frente, parado (conceito) | Uma figura, magenta liso | sprite parado de frente |
| **C** | Costas, parado | Uma figura, magenta liso | sprite parado de costas |
| **AF** | Folha de **andar de frente**, 6 quadros | Grade **3 colunas × 2 linhas**, margem larga | ciclo de andar (6) |
| **AC** | Folha de **andar de costas**, 6 quadros | Grade 3×2 | ciclo de andar (6) |
| **FL** | Folha de **flutuar/parado animado**, 6 quadros (sombras e chefes) | Grade 3×2 | ciclo idle |
| **AT** | Folha de **ação/ataque/gesto**, 6 quadros | Grade 3×2 | ação de combate |
| **DX** | Folha de **dano e derrota**, 6 quadros (3 hurt + 3 derrota) | Grade 3×2 | hurt/defeat (ou por código) |

**Ordem de trabalho sempre:** F → (aprovado) → C anexando F → AF anexando F → AC anexando C. Anexar a imagem aprovada em cada passo é o que mantém o rosto e as cores iguais.

## 3. Elenco e imagens necessárias

### 3.1 Companheiros (5) — F, C, AF, AC (4 imagens cada) + combate depois
| Personagem | Imagens agora | Depois (combate, M1+) |
|---|---|---|
| Aulo, o Soldado | F ✅ C ✅ (retratos aprovados) · andar frente ✅, costas ✅, esquerda ✅ e direita ✅ (sprite 56 px, 12 quadros, marionete 3D; braço parado; mesmo rosto nos dois perfis) → **Aulo fechado**. **Nova versão SIMÉTRICA (24/out): frente, costas, perfil direito aprovados; andar 56 px nas 4 direções + parado em `aulo_sim/`. Esta é a versão oficial e a única guardada (a antiga assimétrica foi apagada). Parado de lado a partir de `aulo_pp_parado_direita.png`; regra nova: 4 imagens por personagem (F, C, perfil andando, perfil parado). Regra de simetria para todos os personagens: ver `docs/PIPELINE_PERSONAGEM.md`.** | AT, DX |
| Ismena, a Carpideira | F, C, AF, AC | AT, DX |
| Dimas, o Saqueador | F, C, AF, AC | AT, DX |
| Septímia, a Vestal | F, C, AF, AC | AT, DX |
| Cálias, o Aedo | F, C, AF, AC | AT, DX |

### 3.2 Protagonista — 5 origens × (F, C, AF, AC)
Pescador, Sacerdote, Veterano, Artesão, Estrangeiro. **Rosto neutro, cabelo curto liso, sem cicatriz/tatuagem** (Veterano tem cicatriz). Tabuinha Rasurada no cinto sempre.
- **Teste de gênero (pendência 1):** gerar o **Pescador masculino e feminino** (só F) e comparar antes de decidir se o corpo muda.
- **Economia:** gerar completo (F, C, AF, AC) só do **Pescador**; as outras origens começam só com F e C, e as folhas de andar entram depois de validado o pipeline (a roupa pode virar camada).
- Variações (cabelo, pele, marcas) saem por troca de paleta/camadas, **sem nova geração**.

### 3.3 Sombras — F + FL (+ AT depois)
Pedir **opacas** (translucidez entra por shader). Sem vista de costas (no jogo aparecem de frente).
- Definidas: **Remador** (tutorial), **Tecelã** (Campos do Luto), **Padeiro** (Rua dos Ofícios).
- A definir: as outras ~12 a 17 nomeadas e os **6 corpos-base** das sombras comuns (ver `PENDENCIAS.md`).

### 3.4 Chefes (6 + 1 opcional) — F + FL + AT (3 fases → 1 AT por fase quando for a hora)
A Fome · Cérbero (3 cabeças, vista própria) · O Campeão · O Sonho · As Fúrias (Alecto, Megera, Tisífone) · A Sombra do Esquecido · *(opcional)* O Sem-Nome. Chefes são **maiores** (pedir figura maior, 128 a 160 px finais). Descrições em `HISTORIA.md` seção 6.

### 3.5 Outros (não são Gemini, ou vêm depois)
Retratos 64×64 (3–4 expressões por companheiro), ícones dos ~65 itens, UI, texturas de cenário, romã/Marca. Ver `ARTE.md` seção 8.

### 3.6 Contagem do primeiro lote
Companheiros 20 (3 já feitas) + Pescador 4 + 4 origens × 2 (F, C) + 2 teste de gênero + 3 sombras × 2 = **~44 imagens** de conceito/folha. Plano de uso do limite diário do Gemini: 4 a 6 por dia, conforme a cota real do plano (não verificada).

---

## 4. Prompts

Todos em inglês. Cada prompt = **BASE REALISTA** + **CHARACTER** (descrição, seção 5) + **sufixo do tipo** (seção 4.2). Na conversa do Gemini, **anexar a imagem aprovada** quando indicado.

### 4.1 BASE REALISTA (colar sempre primeiro)
```
Pixel art character for a 2D video game sprite, detailed realistic style (not chibi, not cartoon): natural adult human proportions, head about one seventh to one eighth of the body height, readable face with eyes, nose and mouth. Visible square pixels, hard edges, limited muted palette, flat shaded with a few tones per material, no anti-aliasing, no blur, no glow, no gradients, no dithering. Sober, worn Greco-Roman dark fantasy. Muted earthy palette like painted Greek ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent. Never use magenta on the character.

Background: one flat solid pure magenta (#FF00FF) filling the whole image. Nothing else: no ground, no cast shadow, no text, no frame, no grid lines, no color swatches, no extra props lying around.
```

### 4.2 Sufixos por tipo
**F — frente parado**
```
POSE: standing idle, full body from head to feet, centered, facing the viewer slightly turned toward the lower right (3/4 front view), relaxed natural stance, arms at the sides or holding the props described. Empty margin around the whole figure.
```
**C — costas parado** (anexar a F aprovada)
```
Use the attached image as the exact reference: same character, same outfit, same colors, same pixel density and proportions. POSE: the same character seen from BEHIND (back view), standing idle, full body, centered, head straight and facing away from the viewer (do not turn the head), slightly turned toward the upper right. Show the back details of the clothing, armor, hair, bag or weapon. Keep carried props on the same side of the body as in the reference. Empty margin around the figure.
```
**AF — andar de frente** (anexar F)
```
Use the attached image as the exact reference: same character, outfit, colors, pixel density and proportions in every frame. Create a WALK CYCLE sprite sheet of exactly 6 frames arranged in a strict grid of 3 columns and 2 rows (frames 1-3 on the top row, 4-6 on the bottom row, left to right). Every frame has the same figure size, the same scale, the character facing the viewer slightly turned toward the lower right, full body visible, feet on the same baseline in every frame. Large empty magenta margin around each figure so no figure touches another figure or the edge of its cell. Frames: 1 contact (left foot forward), 2 passing, 3 high point, 4 contact (right foot forward), 5 passing, 6 high point. Subtle natural arm swing, props stay attached and consistent. Same flat magenta background, no grid lines, no numbers, no text.
```
**AC — andar de costas** (anexar C aprovada)
```
Use the attached BACK-VIEW image as the exact reference: same character, outfit, colors, pixel density and proportions in every frame. Create a WALK CYCLE sprite sheet of exactly 6 frames in a strict grid of 3 columns and 2 rows (frames 1-3 top row, 4-6 bottom row, left to right). The character is seen from BEHIND walking away from the viewer, head straight (not turned), slightly turned toward the upper right, full body visible, same size and scale in every frame, feet on the same baseline. Large empty magenta margin around each figure so no figure touches another figure or the edge of its cell. Frames: 1 contact (left foot forward), 2 passing, 3 high point, 4 contact (right foot forward), 5 passing, 6 high point. Subtle arm swing, props stay on the same side as in the reference. Same flat magenta background, no grid lines, no numbers, no text.
```
**FL — flutuar / idle animado** (sombras e chefes; anexar F)
```
Use the attached image as the exact reference. Create an IDLE sprite sheet of exactly 6 frames in a strict grid of 3 columns and 2 rows, same figure size in every frame, large magenta margin around each figure. The figure hovers and sways very slightly (breathing, drifting wisps at the base, tiny shifts of the arms or props), small differences between frames, loop-friendly. No grid lines, no text.
```
**AT — ação / gesto de combate** (anexar F ou C; trocar [ACTION])
```
Use the attached image as the exact reference. Create an ACTION sprite sheet of exactly 6 frames in a strict grid of 3 columns and 2 rows: [ACTION]. Same character, outfit, colors and scale in every frame, feet on the same baseline, large magenta margin around each figure, no figure touching another. No grid lines, no text.
```
**DX — dano e derrota** (anexar F)
```
Use the attached image as the exact reference. Create a sprite sheet of exactly 6 frames in a strict grid of 3 columns and 2 rows: frames 1-3 take a hit (flinch, recoil, recover), frames 4-6 fall to defeat (stagger, kneel, collapse). Same character, outfit, colors and scale, large magenta margin around each figure. No grid lines, no text.
```


### 4.3 Folhas de ANDAR v2: 4 quadros com guia de poses (usar no lugar de AF/AC)
Problema das folhas 3×2 de 6 quadros: o Gemini repete uma perna só. Solução: **4 quadros (2×2)** + **imagem-guia** `art/guias/guia_poses_andar.png` (palitos: azul = perna esquerda, vermelho = direita) anexada junto com a imagem aprovada do personagem. Anexar 2 imagens: (1) F ou C aprovada, (2) o guia. Depois: `folha_para_quadros.py <folha> --colunas 2 --linhas 2 --alturas 96 --cores 28` e `montar_ciclo_pernas.py ... --originais 1,2,3,4` (ou preset). Plano B: 1 pose por geração (4 gerações) com o texto de cada pose abaixo.

**AF2 — de frente (anexar F + guia):**
```
Image 1 is the exact character reference: same character, outfit, colors, proportions, pixel density and props in every frame. Image 2 is a LEG POSE GUIDE made of stick figures seen from the side: blue = the character's LEFT leg, red = the character's RIGHT leg. Use image 2 ONLY to copy the leg positions; do not copy the stick figures, their colors or their style.

Create a WALK CYCLE sprite sheet in pixel art with exactly 4 frames in a strict 2 columns x 2 rows grid (frame 1 top-left, frame 2 top-right, frame 3 bottom-left, frame 4 bottom-right). The character faces the viewer turned 3/4 toward the lower right and walks toward the right. Same figure size and scale in every frame, feet touching the same ground line at the bottom, large empty magenta margin around each figure so no figure touches another or the edge of its cell.

FRAME 1 (contact): RIGHT leg stretched forward (toward the right), heel on the ground, nearly straight. LEFT leg stretched far back, only the toe touching the ground, heel lifted.
FRAME 2 (passing): RIGHT leg straight and planted under the body. LEFT leg lifted off the ground, knee bent about 70 degrees, the left foot hanging just behind the right knee, passing it.
FRAME 3 (contact, the OPPOSITE of frame 1): LEFT leg stretched forward (toward the right), heel on the ground. RIGHT leg stretched far back, only the toe touching the ground, heel lifted.
FRAME 4 (passing, the OPPOSITE of frame 2): LEFT leg straight and planted under the body. RIGHT leg lifted off the ground, knee bent about 70 degrees, passing the left knee.

Strict rules: both legs must have the same length in every frame (no shrinking); the two legs must be in clearly different positions in every frame; frame 3 must visibly swap the roles of the legs compared to frame 1; no two frames may have the same leg pose; the body bobs slightly (lowest in frames 1 and 3, highest in frames 2 and 4); the arms swing opposite to the legs; carried props stay in the same hand as in image 1. Flat magenta background (#FF00FF), no grid lines, no numbers, no text.
```

**AC2 — de costas (anexar C + guia):** o mesmo texto, trocando a 1ª frase de pose por: `The character is seen from BEHIND, walking away from the viewer toward the upper right, head straight (not turned).` e `Image 1 is the BACK-VIEW reference`. As pernas continuam sendo as do PRÓPRIO personagem (esquerda/direita dele, não da tela).

**Resultado do teste (Aulo de frente, 23/out/2026):** o Gemini fez só 2 poses e copiou (quadros 3 = 1 e 4 = 2). As 2 poses saíram boas (contato e passagem). **Continuação usada:** gerar as poses opostas uma a uma anexando a pose boa como referência (ver "Pose oposta" abaixo). Recortes das poses boas: `art/guias/aulo_pose1_contato.png` e `aulo_pose2_passagem.png`.

**Pose oposta (anexar a pose boa recortada + o guia; trocar [POSE] pelo texto):**
```
Image 1 is the exact reference: same character, same size, same camera angle (3/4 view toward the lower right), same pixel density, outfit and props. Create ONE new image of the same character with the LEGS SWAPPED, the OPPOSITE pose: [POSE]. Everything above the hips (head, torso, arms, helmet, sword) stays almost identical. The two legs must be in clearly different positions than in image 1. Image 2 is a stick-figure leg guide (blue = left leg, red = right leg): copy only the leg positions, not the drawing. Single figure, flat magenta background (#FF00FF), no text.
```
- Oposta do contato (usar a pose 1 recortada): `[POSE]` = `LEFT leg stretched forward (toward the right), heel on the ground; RIGHT leg stretched far back, only the toe touching the ground, heel lifted`.
- Oposta da passagem (usar a pose 2 recortada): `[POSE]` = `LEFT leg straight and planted under the body; RIGHT leg lifted off the ground, knee bent about 70 degrees, passing the left knee`.

**Plano B (1 pose por geração, anexar F + guia):** `Same character as image 1, one single figure, pixel art, flat magenta background. Pose from image 2: [colar o texto de UM dos 4 quadros acima]. Keep the same size and the same ground line.` Repetir 4 vezes e o Claude junta as imagens numa folha.

## 5. Linhas CHARACTER (colar depois do BASE REALISTA)

### Companheiros
- **Aulo:** `CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.`
- **Ismena:** `CHARACTER: Ismena, a professional mourner (ancient Greek lamenting woman). Long black veil covering her hair and falling over her shoulders, dark long dress with ice-blue (#8FB3C9) trim at the hem and sleeves. Drooping shoulders, head slightly bowed, both hands open and relaxed at her sides, a small white cloth in one hand. Pale face, tired sad eyes. Dominant colors: black and pale ice blue.`
- **Dimas:** `CHARACTER: Dimas, a grave robber, lean and wiry, slightly hunched, shifty but not villainous. A heavy sack slung over his back, a small oil lantern hanging from one hand, a hooked iron tool (grappling hook) at his belt. Patched, mismatched ragged clothes in brown, with dirty gold (#C8A24A, dulled) details: trim, a coin pouch. Wrapped cloth around his hands, a headband. Dominant colors: brown and dirty gold.`
- **Septímia:** `CHARACTER: Septímia, a former Vestal priestess, calm and dignified. Long white tunic and a pale veil (the Roman suffibulum) with a thin band at the head, simple and clean. She carries a small clay oil lamp with a lit flame in front of her in both hands; the flame is a flat orange pixel shape (#E0833A) with no glow. Straight posture, serene but guarded face. Dominant colors: off-white and flame orange.`
- **Cálias:** `CHARACTER: Cálias, a wandering Greek bard (aoidos), thin and expressive, middle-aged. A wooden lyre slung on his back. The only colorful garment in the whole cast: a faded olive-green (#7A7F45) mantle (himation) with muted purple (#6B4A7A) trim, worn and sun-bleached. A crown of wilted laurel leaves on his head. Gentle ironic smile, one hand raised slightly as if about to tell a story. Dominant colors: olive and muted purple.`

### Protagonista (trocar `[GENDER]` por `male` ou `female`)
Linha comum: `Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. A small wax writing tablet with scratched-out writing hangs from the belt.`
- **Pescador:** `CHARACTER: the player character, origin "fisherman of Cape Taenarum". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Short coarse tunic in faded teal-green (#4E7F86, desaturated), a coil of rope and a folded fishing net over one shoulder, barefoot, sun-weathered skin, rolled-up sleeves. A small wax writing tablet with scratched-out writing hangs from the belt.`
- **Sacerdote:** `CHARACTER: the player character, origin "priest of Demeter and Hecate". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Long ankle-length robe in bone (#D9CBB0) and ochre, a woolen ribbon band (fillet) tied around the head, a small woven basket of offerings held in one hand (bread, a small jar). A small wax writing tablet with scratched-out writing hangs from the belt.`
- **Veterano:** `CHARACTER: the player character, origin "war veteran". Ordinary face with one thin scar on the cheek, short plain dark-brown hair, [GENDER], sturdy build. Worn leather cuirass with shoulder guards, a short faded cape, a desaturated brick-red tunic (closer to terracotta than bright red), bronze buckles and bracers. No weapon drawn. A small wax writing tablet with scratched-out writing hangs from the belt.`
- **Artesão:** `CHARACTER: the player character, origin "potter and weaver craftsman". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Thick leather apron over a terracotta-brown tunic, sleeves rolled up, hands and forearms stained with clay, a small tool (wooden spatula) tucked in the belt. A small wax writing tablet with scratched-out writing hangs from the belt.`
- **Estrangeiro:** `CHARACTER: the player character, origin "foreigner". Ordinary neutral face, short plain dark-brown hair, [GENDER], average slim build. Long traveling cloak in sand and gray with the hood pulled back onto the shoulders, plain worn tunic, a leather shoulder bag, dusty boots, no insignia or decoration of any kind. A small wax writing tablet with scratched-out writing hangs from the belt.`

### Lia, a Retratista (protagonista jogável; **proposta, em geração de imagens**, ver `docs/LIA_PROPOSTA.md`)
Ordem: 1 batalha frente 3/4 → 2 batalha costas 3/4 (anexar a 1) → depois de aprovar: F e C simétricas, perfis e parado. Batalha (assimétrica): tábua com um retrato pela metade e pincel fino. Mapa (simétrico): sem itens, sem bolsa. Pernas visíveis do joelho para baixo (rig do Dimas, a confirmar).
- **Lia:** `CHARACTER: Lia, a portrait painter, adult woman. Soft rounded face, fair skin with a few freckles, light blue eyes, arched brown eyebrows, open warm smile. Long wavy dark-brown hair tied back with a linen band (taenia), one small gold hoop earring. Knee-length linen tunic in faded saffron (#C99A3B, desaturated) with rolled-up sleeves and paint stains (no pink or magenta stains). Leather-and-linen apron in lapis blue (#4A6A8A) with straps crossed on the back and tied in a bow. Leather pouch on her RIGHT hip, strap over her LEFT shoulder. Sandals. Dominant colors: faded saffron and lapis blue.`
  > Linha montada pelo Claude a partir da descrição de aparência em `LIA_PROPOSTA.md` (a `LIA_FICHA.md` citada nos prompts não veio no pacote). Os blocos de prompt da conversa com o Gemini continuam valendo; se divergirem, valem eles.

### Sombras (opacas; só F e FL)
- **Remador:** `CHARACTER: the shade (ghost) of a drowned oarsman, standing. Pale ghostly gray-teal body drawn as a solid opaque figure (no real transparency), the lower legs dissolving into pixelated wisps and fading to nothing at the bottom. Torn short tunic, a long wooden oar held in both hands like a staff, water-stained, hollow dark eyes, resigned posture. Dominant colors: pale Lethe teal and gray.`
- **Tecelã:** `CHARACTER: the shade of a weaver woman, standing. Pale ghostly blue-gray solid opaque figure, the lower body dissolving into pixel wisps. Long dress, hands raised in front of her pulling taut a few thin pale threads between her fingers, hair tied back, patient waiting expression. Dominant colors: pale gray-blue (#8FB3C9) and bone.`
- **Padeiro:** `CHARACTER: the shade of a baker, stocky man, standing. Pale ghostly warm-gray solid opaque figure with faint ochre flour-dust tones, the lower body dissolving into pixel wisps. Apron, rolled sleeves, hands mid-kneading motion in front of his belly, a long wooden bread peel leaning on one shoulder, vague absent expression. Dominant colors: bone and pale ochre.`

### Chefes (descrições a escrever antes de gerar; marcar "bigger than a normal human, imposing")
Pendente: A Fome, Cérbero, O Campeão, O Sonho, As Fúrias, A Sombra do Esquecido, O Sem-Nome. O Claude escreve a linha CHARACTER de cada um quando o João pedir o kit dos chefes (usar `HISTORIA.md` seção 6).

## 6. Depois de gerar (pixelizar)

```
python tools/pixelizar.py art/concept/gemini/<categoria>/<nome>.png --alturas 80 96 --cores 28 --saida art/sprites/<categoria>
python tools/folha_para_quadros.py <folha>.png --colunas 3 --linhas 2 --alturas 96 --cores 28 --fps 8 --saida art/sprites/<categoria>/<nome>_<acao>
```
Conferir: fundo sem halo; quadros sem sobreposição; rosto, armadura e cores iguais entre quadros; GIF com pé fixo no chão. Se vier 5 quadros ou quadros colados, pedir de novo com "exactly 6 frames, 3 columns × 2 rows, wide margins".

**Folhas de andar (AF/AC):** depois de cortar, **sempre** rodar `montar_ciclo_pernas.py` com um preset de `tools/ciclos/` (padrão em `ARTE.md` 7.6; modelo em `tools/ciclos/_modelo.json`, exemplo aprovado: `aulo_costas.json`), porque o Gemini não alterna as pernas direito. O prompt de AC/AF melhorado descreve cada quadro pela posição das DUAS pernas, mas o resultado ainda é irregular.

## 7. Pastas
```
art/concept/gemini/
├── companheiros/   <nome>_f.png  <nome>_c.png  <nome>_andar_f.png  <nome>_andar_c.png
├── protagonista/   <origem>_<m|f>_f.png ... (idem)
├── sombras/        <nome>_f.png  <nome>_flutuar.png
└── chefes/         <nome>_f.png  <nome>_flutuar.png  <nome>_ataque_<fase>.png
art/sprites/        (saída do pixelizador, mesma estrutura)
```

## 8. Progresso
- [x] Aulo F (realista), Aulo C, Aulo AF (testada em 48/64)
- [x] Aulo AC (gerado; ciclo de 8 quadros aprovado, preset `tools/ciclos/aulo_costas.json`)
- [~] Aulo AF: ciclo de 6 quadros inteiros do Gemini, sem corte (`tools/ciclos/aulo_frente.json`, modo `--originais`); aguardando aprovação
- [ ] Ismena F/C/AF/AC · Dimas · Septímia · Cálias
- [ ] Pescador m/f F (teste de gênero) → Pescador C/AF/AC
- [ ] Outras origens F/C
- [ ] Sombras definidas (Remador, Tecelã, Padeiro) F/FL
- [ ] Chefes (descrições a escrever)


## Status do andar do Aulo
- Costas: APROVADO (`tools/ciclos/aulo_costas.json`, 8 quadros).
- Frente: APROVADO (`tools/ciclos/aulo_frente_12.json`, 12 quadros; pernas trocadas por codigo, ver `docs/ARTE.md` 7.7). Saida em `art/sprites/companheiros/aulo/andar_frente_12/`.
- Ismena F e C: feitas e pixelizadas juntas (paleta compartilhada, `--cores 56`). Faltam AF, AC.
- Ismena AF e AC: ciclos de 6 quadros montados com --originais (presets ismena_frente.json / ismena_costas.json), aguardando aprovacao.
- Aulo marionete (sprite pintado do Gemini + esqueleto 3D): frente e costas em 12 quadros, teste em `art/sprites/companheiros/aulo/teste_3d_marionete/`; ferramentas em `tools/render3d/` (index.html = esqueleto/andar, joints.py, marionete.py). Perfis feitos (24/out): esquerda = `aulo_perfil_esquerda_rosto_novo.png`, direita = `aulo_perfil_direita_sem_capacete.png` (Gemini, sem capacete); saídas em `art/sprites/companheiros/aulo/andar_56px/{esquerda,direita}`; usar `sprite_andar.py --celula-fixa` e `marionete.py esquerda|direita` (desloca_x ajusta o nariz).


## Aulo: perfis e parado (prompts)
Anexar `aulo_f.png` nos dois perfis (o perfil NAO e espelho: capacete e espada ficam em lados diferentes). Parado: anexar F para o de frente e C para o de costas.

**PERFIL DIREITA (anexar aulo_f.png)**
```
Pixel art character for a 2D video game sprite, detailed realistic style (not chibi, not cartoon): natural adult human proportions, head about one seventh to one eighth of the body height, readable face with eyes, nose and mouth. Visible square pixels, hard edges, limited muted palette, flat shaded with a few tones per material, no anti-aliasing, no blur, no glow, no gradients, no dithering. Sober, worn Greco-Roman dark fantasy. Muted earthy palette like painted Greek ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent. Never use magenta on the character.

Background: one flat solid pure magenta (#FF00FF) filling the whole image. Nothing else: no ground, no cast shadow, no text, no frame, no grid lines, no color swatches, no extra props lying around.

CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.

Use the attached image as the exact reference: same character, same outfit, same colors, same pixel density and proportions. POSE: the same character in pure SIDE PROFILE (exactly 90 degrees), FACING THE RIGHT side of the image, standing idle, full body from head to feet, centered, feet on the same baseline. We see his RIGHT side: his right arm hangs in front of the body, the sword and scabbard hang at his right hip and are fully visible, the helmet under his left arm is only partly visible behind the torso. Show the profile of the face. Empty margin around the figure.
```

**PERFIL ESQUERDA (anexar aulo_f.png)**
```
Pixel art character for a 2D video game sprite, detailed realistic style (not chibi, not cartoon): natural adult human proportions, head about one seventh to one eighth of the body height, readable face with eyes, nose and mouth. Visible square pixels, hard edges, limited muted palette, flat shaded with a few tones per material, no anti-aliasing, no blur, no glow, no gradients, no dithering. Sober, worn Greco-Roman dark fantasy. Muted earthy palette like painted Greek ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent. Never use magenta on the character.

Background: one flat solid pure magenta (#FF00FF) filling the whole image. Nothing else: no ground, no cast shadow, no text, no frame, no grid lines, no color swatches, no extra props lying around.

CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.

Use the attached image as the exact reference: same character, same outfit, same colors, same pixel density and proportions. POSE: the same character in pure SIDE PROFILE (exactly 90 degrees), FACING THE LEFT side of the image, standing idle, full body from head to feet, centered, feet on the same baseline. We see his LEFT side: his left arm is bent holding the iron helmet under the arm, fully visible; the sword and scabbard are on his right hip, on the far side of the body, so only the tip of the scabbard shows behind his legs. Show the profile of the face. Do NOT mirror the right-facing version: the helmet side is the visible side. Empty margin around the figure.
```

**PARADO DE FRENTE (anexar aulo_f.png)**
```
Pixel art character for a 2D video game sprite, detailed realistic style (not chibi, not cartoon): natural adult human proportions, head about one seventh to one eighth of the body height, readable face with eyes, nose and mouth. Visible square pixels, hard edges, limited muted palette, flat shaded with a few tones per material, no anti-aliasing, no blur, no glow, no gradients, no dithering. Sober, worn Greco-Roman dark fantasy. Muted earthy palette like painted Greek ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent. Never use magenta on the character.

Background: one flat solid pure magenta (#FF00FF) filling the whole image. Nothing else: no ground, no cast shadow, no text, no frame, no grid lines, no color swatches, no extra props lying around.

CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.

Use the attached image as the exact reference: same character, outfit, colors, pixel density and proportions in every frame. Create an IDLE sprite sheet of exactly 4 frames in a strict grid of 2 columns and 2 rows (frames 1-2 top row, 3-4 bottom row, left to right). The character faces the viewer slightly turned toward the lower right (3/4 front view), like the reference. Same figure size and scale in every frame, feet on the same baseline and the feet do NOT move. Only tiny natural changes: chest rises and falls by a pixel or two, shoulders and the free hand move very slightly, the cloak or tunic hem sways a little. Frames: 1 neutral, 2 inhale (body one pixel higher), 3 neutral, 4 exhale (body one pixel lower). Large empty magenta margin around each figure so no figure touches another or the edge of its cell. No grid lines, no numbers, no text.
```

**PARADO DE COSTAS (anexar aulo_c.png)**
```
Pixel art character for a 2D video game sprite, detailed realistic style (not chibi, not cartoon): natural adult human proportions, head about one seventh to one eighth of the body height, readable face with eyes, nose and mouth. Visible square pixels, hard edges, limited muted palette, flat shaded with a few tones per material, no anti-aliasing, no blur, no glow, no gradients, no dithering. Sober, worn Greco-Roman dark fantasy. Muted earthy palette like painted Greek ceramics: bone #D9CBB0, terracotta #B5573B, charcoal #25222B, Lethe teal #4E7F86, antique gold #C8A24A. Deep pomegranate red #8C1F2E only as a tiny accent. Never use magenta on the character.

Background: one flat solid pure magenta (#FF00FF) filling the whole image. Nothing else: no ground, no cast shadow, no text, no frame, no grid lines, no color swatches, no extra props lying around.

CHARACTER: Aulo, a weathered middle-aged Roman legionary, tall and upright military posture. Old battered iron armor (lorica segmentata style, chipped and dull) over a faded brick-red tunic (desaturated, closer to terracotta than bright red). Iron helmet held under his left arm. Short graying hair, stern tired face, short stubble. A short sword (gladius) at the belt. Leather boots, dirty. Dominant colors: faded red and dull iron gray.

Use the attached image as the exact reference: same character, outfit, colors, pixel density and proportions in every frame. Create an IDLE sprite sheet of exactly 4 frames in a strict grid of 2 columns and 2 rows (frames 1-2 top row, 3-4 bottom row, left to right). The character is seen from BEHIND, head straight, slightly turned toward the upper right, like the reference. Same figure size and scale in every frame, feet on the same baseline and the feet do NOT move. Only tiny natural changes: chest rises and falls by a pixel or two, shoulders and the free hand move very slightly, the cloak or tunic hem sways a little. Frames: 1 neutral, 2 inhale (body one pixel higher), 3 neutral, 4 exhale (body one pixel lower). Large empty magenta margin around each figure so no figure touches another or the edge of its cell. No grid lines, no numbers, no text.
```


**Ismena (24/out, versão simétrica FECHADA):** imagens F, C e perfil direito em `art/concept/gemini/companheiros/ismena_sim/` (`sim_f.png`, `sim_c.png`, `sim_pd.png`); sprites 56 px (andar 12 quadros nas 4 direções + parado) em `art/sprites/companheiros/ismena_sim/`. Feitos com `tools/andar_vestido.py` (vestido longo: balanço da barra, salto de 1 px, passo alternando; `--pe-tras` mostra o pé de trás no perfil). **Só o perfil esquerdo é gerado; o direito é o espelho dele quadro a quadro** (gerar os dois separados deu pé de trás errado). As imagens antigas da Ismena (com pano na mão) estão superadas.

**Lia (25/out, batalha APROVADA, proposta de protagonista):** originais do Gemini em `art/concept/gemini/batalha/lia_34_original.png` e `lia_34_costas_original.png`; versões ESPELHADAS (frente olha para a esquerda, costas para a direita, como no layout de batalha) em `lia_34.png` e `lia_34_costas.png`. Sprites: `art/sprites/batalha/{liso,pixel_128,pixel_160}/lia_34*.png`. Depois do espelho: pincel na mão DIREITA, painel na ESQUERDA, bolsa no quadril direito (frente). Mapa simétrico (F/C/PD/PP) ainda não gerado.

**Lia (25/out, versão simétrica FECHADA para o mapa, proposta de protagonista):** imagens F, C e PD em `art/concept/gemini/companheiros/lia_sim/` (`sim_f/c/pd.png`; `pad_*.png` = cópias em 1024x1024; `sim_f_gemini.png` = frente original do Gemini). A frente teve a perna e o pé direitos refeitos por ESPELHO da metade esquerda (a partir de y=760), porque o Gemini devolvia pé torto. Sem itens nas mãos (painel/pincel/bolsa só na arte de batalha). Cabelo cobre nuca e costas. **As 3 vistas: `tools/render3d/marionete_capa.py lia_frente|lia_costas|lia_direita joints.json saida.png`** (saia na altura do joelho, só canela e pé animados; `lia_direita` modo "duas": duas pernas pintadas separadas por divisor inclinado, `amp=0.5` reduz o balanço para o joelho não aparecer fora da barra; `q_n=57`). Andar 56 px nas 4 direções + parado em `art/sprites/companheiros/lia_sim/` (esquerda = espelho da direita; parado = quadro 1, a passagem).
