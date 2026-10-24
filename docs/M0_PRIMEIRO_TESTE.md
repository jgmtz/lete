# M0 — Primeiro teste: andar com WASD (tarefa para o Claude Code)

Objetivo: abrir no Godot 4 .NET uma cena com **chão branco**, **câmera ortográfica diagonal** e um personagem em pixel art **andando nas 4 direções com WASD** (e analógico). É a primeira fatia do M0 (`docs/BACKLOG.md`). Decisões do João (24/out/2026): câmera 3D diagonal; personagens de teste **Aulo e Ismena**; Godot 4 (.NET) e SDK do .NET já instalados no PC dele.

## O que construir
1. **Projeto Godot 4 .NET** na estrutura de `docs/ARQUITETURA.md`: `src/Lete.Core` (C# puro), `src/Lete.Core.Tests` (xUnit), `src/Lete.Game` (Godot). `.gitignore` do Godot/.NET e `git init`. Pergunte ao João a versão exata do Godot (`godot --version`) e do `dotnet --version` se precisar.
2. **Cena de teste** (`Teste_Andar.tscn`): chão branco grande (plano), uma luz direcional, 4 a 6 cubos cinza espalhados só para dar referência de movimento, `WorldEnvironment` simples (sem névoa ainda).
3. **Câmera**: `Camera3D` ortográfica, rotação diagonal fixa (yaw 45°, inclinação ~35°), seguindo o personagem com suavização leve. Valores em campos exportados, não espalhados no código.
4. **Personagem**: `CharacterBody3D` com `AnimatedSprite3D` (ou `Sprite3D` + troca de textura) em **billboard no eixo Y**, `texture_filter = Nearest`, sem mipmaps, `pixel_size` de modo que 56 px de altura = ~1,7 m. Sombra projetada simples (blob) opcional.
5. **Entrada por ações do Input Map** (nunca teclas fixas): `move_up`, `move_down`, `move_left`, `move_right` ligadas a **W/A/S/D, setas e analógico esquerdo**; `switch_character` (Tab / botão do controle). Movimento **relativo à tela** (W = para cima na tela, que é o sprite de costas; S = frente; A = esquerda; D = direita). Em diagonal, normalizar a velocidade e manter a animação da direção horizontal (ou a última tecla pressionada). Velocidade em campo exportado (placeholder, ver regra dos números mágicos).
6. **Animação**: andar 12 quadros a 8 fps por direção; parado = imagem de `parado/`. Ao soltar as teclas volta ao parado da última direção. Trocar de personagem (Aulo ↔ Ismena) com `switch_character`.
7. **Lógica de movimento testável**: um pequeno `MovementInput`/`Facing` em `Lete.Core` (direção a partir do vetor de entrada, normalização, última direção) com testes xUnit. A camada Godot só lê a entrada e aplica.
8. Atualizar `CLAUDE.md` (seção "Comandos": como abrir, rodar e testar) e marcar no `docs/BACKLOG.md` o que foi feito.

## Sprites (já prontos, copiar para `src/Lete.Game/assets/sprites/<personagem>/`)
- Aulo: `art/sprites/companheiros/aulo_sim/andar_56px/{frente,costas,esquerda,direita}/aulo_<dir>_01..12.png` e `.../aulo_sim/parado/aulo_parado_<dir>.png`.
- Ismena: `art/sprites/companheiros/ismena_sim/andar_56px/{frente,costas,esquerda,direita}/ismena_<dir>_01..12.png` e `.../ismena_sim/parado/ismena_parado_<dir>.png`.
- Cada quadro é um PNG RGBA com a mesma altura (56 px). **Importar sem filtro (Nearest), sem compressão com perda, sem mipmaps.** Os quadros de uma mesma direção têm todos o mesmo tamanho; os pés ficam na mesma linha de base (a base do quadro é o chão). Aulo: 12 quadros por direção, Ismena também.
- As imagens grandes em `art/concept/` não vão para o jogo.

## Pronto quando
- Abre no Godot, roda e o personagem anda nas 4 direções com WASD e analógico, trocando de sprite e animando; Tab troca Aulo/Ismena.
- `dotnet test` passa nos testes de direção/normalização.
- Nenhum número mágico ou tecla fixa no código; nenhum texto de interface fixo (ainda não há UI).

## Fora do escopo agora
Colisão com paredes, interação, diário, combate, save. Só o movimento e a animação.
