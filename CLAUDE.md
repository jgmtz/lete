# CLAUDE.md — Projeto Lete (título provisório)

Jogo indie de **exploração e escolhas** com **combate por turnos baseado em gestos**, tema **greco-romano sombrio**: o protagonista **desceu vivo ao mundo dos mortos**, não lembra por quê, e recupera memórias explorando o Além. Windows, Godot 4 + C#. Sucessor do Damnatio.

## Sobre o desenvolvedor

João Guilherme Matzenbacher, estudante de Engenharia de Software (PUCPR, 6º período), desenvolvedor solo. Gosta de C# e de orientação a objetos. **Prefere que você faça perguntas de esclarecimento antes de implementar algo, quando necessário, para que o resultado saia certo da primeira vez.** Converse em **português do Brasil**. Explique escolhas técnicas de forma curta e clara, sem presumir que ele já conhece Godot (a maioria dos tutoriais é em GDScript; aqui é C#).

## Leia primeiro (nesta ordem)

1. `docs/GDD.md` — mecânicas e sistemas (fonte de verdade do design).
2. `docs/ARQUITETURA.md` — stack, camadas, estado, eventos, glossário PT→código.
3. `docs/DADOS.md` — esquema dos arquivos de conteúdo e a DSL de condições.
4. `docs/HISTORIA.md` — enredo, atos, origens, companheiros, chefes, regiões.
5. `docs/ARTE.md` — direção de arte e pipeline de sprites.
6. `docs/BACKLOG.md` — marcos (M0 a M5) e tarefas.
7. `docs/PENDENCIAS.md` — decisões em aberto, números por definir, conteúdo faltante.
8. `docs/PIPELINE_PERSONAGEM.md` — **receita completa de cada personagem** (Gemini → marionete 3D → 56 px).
9. `docs/ELENCO_IMAGENS.md` — **registro mestre** das imagens por personagem (estilo realista 96 px) e prompts do Gemini.

## Regras de trabalho

- **O GDD é a fonte de verdade.** Se algo não estiver nos documentos, **não invente uma decisão de design**: pergunte ao João ou registre em `docs/PENDENCIAS.md`.
- **Nunca trate números de balanceamento como definitivos.** Dano, custos, janelas, economia e curvas da Maré ficam por último (M5). Use placeholders `_tbd` em arquivos de dados e **nunca espalhe números mágicos pelo código**.
- **Conteúdo é dado, não código.** Sombras, falas, itens, Lembranças, juramentos, missões e regiões ficam em `content/` (JSON), com condições declarativas (ver `docs/DADOS.md`). O código implementa regras.
- **Um único `WorldState` serializável; sistemas conversam por eventos (`EventBus`).** Sem dependências diretas entre Diário, Perfil, missões e mercadores.
- **Lógica pura (sem Godot) em `Lete.Core`**, testada com xUnit; a camada Godot (`Lete.Game`) só faz cena, UI, entrada e áudio.
- **Nenhum texto fixo no código:** tudo por chave de localização (português do BR primeiro); gênero do protagonista via marcação `{m|f}` ou formas neutras.
- **Entrada por ações do Input Map**, nunca teclas fixas. Teclado/mouse e controle compartilham as mesmas ações. Remapeamento previsto.
- **Salvar só em Trívias e ao trocar de região**, em JSON legível e versionado, com migração.
- **Mantenha o painel de debug** atualizado (Marca, Maré, flags, Lembranças, juramentos, teleporte).
- **Entregue em passos pequenos e verificáveis**, com testes onde houver regra de estado. Ao terminar um passo, diga o que mudou e como conferir.
- Convenções de código e nomes em `docs/ARQUITETURA.md` (seções 8 e 10). Identificadores de código em **inglês**; IDs de conteúdo em `snake_case` ASCII, em português.

## Princípios de design que não podem ser quebrados

1. **Explorar é lembrar.** Progresso aparece no mundo, **nunca como porcentagem ou contagem visível** (nem Mosaico, nem Marca, nem Memórias).
2. **O que é irreversível tem peso:** Marca dos Mortos, Memórias Pesadas (Encarar/Deixar), perjúrio e escolha do companheiro **não têm botão de desfazer**.
3. **A origem dá alternativa, nunca vantagem.**
4. **Dificuldade sobe por regras mais complexas**, não por mais inimigos por luta.
5. **Nenhum caminho fecha o jogo.** O jogador nunca fica preso sem poder voltar a explorar ou escolher *Ficar*.
6. **Cura nunca vem de derrotar sombras.**
7. **Sem marcadores de missão, sem seta no mapa:** o Diário é o único guia.
8. **A romã é o único vermelho forte** da paleta (ver `docs/ARTE.md`).
9. **O protagonista é o único afetado:** ninguém o lembra no final.

## Estado atual do projeto

**M0 (protótipo técnico) quase pronto** (ver `docs/BACKLOG.md`): solução montada, sala de teste com câmera diagonal, névoa, jogador, objetos examináveis, Diário placeholder, painel de debug e o teste de tamanho/densidade. Sprites de mapa dos cinco companheiros (Aulo, Ismena, Dimas, Septímia, Cálias) e da Lia (proposta de protagonista, `docs/LIA_PROPOSTA.md`) prontos (56 px, 4 direções, 12 quadros) e jogáveis no teste do andar e na sala (Tab cicla) (`src/Lete.Game/Sandbox/WalkTest.tscn`, ver `docs/M0_PRIMEIRO_TESTE.md`). **Identidade visual barro e ouro** em todas as telas por um tema central (`src/Lete.Game/UI/UiStyle.cs`, paleta `ui_paleta.tres`, arte gerada por `python tools/ui_arte.py`; GDD 10); proporção `expand` (16:9, 16:10 e 4:3 com o HUD por frações da tela). Render em baixa resolução e escala 3× decididos (`docs/PENDENCIAS.md` 12–13). **M1 em andamento:** máquina de turnos (`BattleFlow`), os 5 gestos (Anel, Esquiva, Segurar, Arrastar e Traço), Memória e Lucidez (estado esquecida), vias Combater, Libertar (Escutar revela nome e fixação) e Absorver (com Eco), HUD em pixel art (layout normal) e arte de batalha 3/4 dos cinco companheiros; a luta de teste é `BattleAttackTest` (ver `docs/BACKLOG.md`). **Pixelorama** instalado para o acabamento dos sprites; prompts do Gemini em `docs/ELENCO_IMAGENS.md`.

## Pipeline de sprites

`tools/pixelizar.py` converte imagens do Gemini (fundo magenta liso) em sprites (decidido: realista 96 px, `--alturas 80 96 --cores 28`) e gera folhas de comparação. Você consegue **ver as imagens geradas** (leia o PNG) e ajustar tolerância, cores e paleta até ficar bom. O acabamento pixel a pixel e a escolha final do tamanho são do João. `tools/folha_para_quadros.py` faz o mesmo para folhas de animação (corta, alinha e gera GIF). `tools/montar_ciclo_pernas.py` conserta o ciclo de andar (pernas alternando; padrão para todos os personagens, presets por personagem em `tools/ciclos/`, ver `docs/ARTE.md` 7.6); `tools/refazer_pernas_opostas.py` troca as pernas em personagens de frente (ver 7.7). Detalhes e prompts em `docs/ARTE.md` e `docs/ELENCO_IMAGENS.md`.

## Comandos

Ambiente: **Godot 4.7.2 .NET** (winget `GodotEngine.GodotEngine.Mono`, em `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*\`) e **.NET SDK 10** compilando para **net8.0** (alvo do GodotSharp 4.7).
Estrutura: `project.godot` e `Lete.Game.csproj` na raiz (o csproj exclui `src/Lete.Core*` e só referencia o Core); scripts do jogo em `src/Lete.Game/`. Pastas sem recursos do Godot levam `.gdignore`.
Organização (26/out): **o elenco tem um registro único** em `content/characters/elenco.json` (altura, arte de batalha de frente e costas, retrato, pasta de mapa, tinta e flutuar de cada pessoa; as batalhas só citam o id e a pose). Arte de batalha só em `art/sprites/batalha/pixel_160/` (a escolhida); sprites de mapa em `art/sprites/companheiros/` e `art/sprites/sombras/`; prévias (GIFs, tiras) em `art/concept/conferencia/`, originais do Gemini em `art/concept/gemini/` (o Godot não importa `art/concept`). **O que o jogo não usa vai para `../sobras/`** (fora do projeto, listado em `sobras/SOBRAS.md`); ao fim de cada marco, perguntar ao João se apaga.

- Build: `dotnet build Lete.sln`
- Testes: `dotnet test src/Lete.Core.Tests`
- Abrir no editor: `Godot_v4.7.2-stable_mono_win64.exe --path . --editor`
- Batalha com turnos na arte escolhida (pixel_160, render em baixa resolução; Lia como protagonista (pedido do João, 25/out/2026), Aulo apoia com Postura; a sombra é o Remador (textos em rascunho; arte própria com o flutuar de 6 quadros) e alterna aparo com o Anel e Esquiva; Memória (Traço: setas no ritmo) usa 5 Lembranças de teste e gasta Lucidez (zerada, a mais usada fica esquecida); Aproximar → Escutar (Segurar) revela pista, nome e fixação conforme o golpe do ciclo, Libertar (escolher a fixação + Arrastar) a faz partir; com a Firmeza baixa, Absorver (Segurar) guarda o Eco dela, usável uma vez por luta (Anel duplo); Fôlego zerado recomeça; `-- --demo` liberta, na luta seguinte usa as 5 Memórias e ataca até absorver (a fase 2 do Remador aparece aos 50%), e na luta seguinte usa o Eco; `-- --demo=vias` joga uma luta por via nova (oferenda e Presenciar, provisão e Poupar, Fugir, Deixar-se vencer); `-- --folego=5` começa quase sem Fôlego para ver a derrota com custo; `-- --demo=absorver --firmeza=25` começa a sombra fraca e absorve na hora; `-- --shot=a.png --shot-at=1.5,20` salva a janela real nesses segundos e fecha): `Godot_v4.7.2-stable_mono_win64.exe --path . res://src/Lete.Game/Sandbox/BattleAttackTest.tscn`
- Teste de elenco da batalha (Tab passa por todos do elenco: de costas no grupo e de frente no lugar da sombra; H testa avanço/dano/derrota; `-- --demo`): `Godot_v4.7.2-stable_mono_win64.exe --path . res://src/Lete.Game/Sandbox/BattleTest.tscn`
- Teste dos gestos (Anel, repetindo sem parar): `Godot_v4.7.2-stable_mono_win64.exe --path . res://src/Lete.Game/Sandbox/GestureTest.tscn`
- Teste do andar (Aulo/Ismena/Dimas/Septímia/Cálias/Lia, Tab cicla os seis, sem Diário nem debug): `Godot_v4.7.2-stable_mono_win64.exe --path . res://src/Lete.Game/Sandbox/WalkTest.tscn` (ou abrir a cena no editor e apertar F6)
- Rodar o jogo: `Godot_v4.7.2-stable_mono_win64.exe --path .` (compilar antes com `dotnet build Lete.Game.csproj`). Começa pela **cutscene de abertura** (`src/Lete.Game/Opening/`): caminhada, rio, reflexo negro, escolha do personagem só pelo reflexo no rio (←/→ trocam, turvo mas com as cores) e então a exploração com o escolhido. Pulável da 2ª vez em diante; `-- --demo=<id>` escolhe sozinho. **Direto para a exploração:** `-- --sem-abertura` (ou qualquer argumento de conferência abaixo)
- Conferir sem jogar à mão (argumentos depois de `--`, ver `Main.cs`): `--render=low|direct --gender=m|f --teleport=<Marker> --examine --ui=journal|debug --shot=<arquivo.png> --frames=N`. Ex.: `... --path . -- --teleport=Poca --examine --shot=C:/tmp/a.png` (precisa de janela; não funciona com `--headless`)
- Importar recursos novos sem abrir o editor: `Godot_..._console.exe --headless --path . --import`
- Exportar Windows: `Godot_..._console.exe --headless --path . --export-release "Windows Desktop" export/windows/Lete.exe` (gera `Lete.exe`, `Lete.pck` e a pasta `data_Lete.Game_windows_x86_64`, que precisam ir juntos; o processo às vezes não fecha depois de exportar, então confira os arquivos e encerre)
- Arte placeholder: `python tools/placeholders.py` e depois o `pixelizar.py` nos conceitos de `art/concept/placeholder/`

Controles (Input Map): mover WASD/setas/analógico/direcional; `interact` E/Espaço/Enter/A; `journal` J/Y; `cancel` Esc/B; `debug_toggle` F1/Back; `switch_character` Tab/X; `run` Shift/RB; `gesture_confirm` Espaço/A (gestos de combate); `debug_zoom_in`/`debug_zoom_out` +/- (escala 2×–4× no teste do andar); `switch_art` Tab e `debug_hit` H (teste de elenco da batalha); `reduce_motion_toggle` F3 (abertura: reduzir movimento); na batalha com turnos: `cancel` Esc/B volta dos submenus; `assist_toggle` F2 (assistência de gestos: desligada, janelas maiores, automática); `debug_turn` T (passa a vez do jogador sem agir), `debug_reveal` N (revela nome, depois fixação), `debug_gesture_area` G (mostra a área central dos gestos).
- Sprites: `python tools/pixelizar.py <imagem.png> [--alturas 32 48 64] [--paleta "#..."]`
