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

Fase de **ideação e direção de arte** concluída parcialmente. **Ainda não há código.** O próximo passo é o **M0 (protótipo técnico)** descrito em `docs/BACKLOG.md`. Antes de escrever código, **confirme o plano do M0 com o João** e pergunte o que precisar (por exemplo, versão do Godot instalada, se o Python e o Aseprite estão disponíveis, onde o repositório ficará).

## Pipeline de sprites

`tools/pixelizar.py` converte imagens do Gemini (fundo magenta liso) em sprites de 32/48/64 px e gera folhas de comparação. Você consegue **ver as imagens geradas** (leia o PNG) e ajustar tolerância, cores e paleta até ficar bom. O acabamento pixel a pixel e a escolha final do tamanho são do João. Detalhes e prompts em `docs/ARTE.md`.

## Comandos

Ambiente: **Godot 4.7.2 .NET** (winget `GodotEngine.GodotEngine.Mono`, em `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*\`) e **.NET SDK 10** compilando para **net8.0** (alvo do GodotSharp 4.7).
Estrutura: `project.godot` e `Lete.Game.csproj` na raiz (o csproj exclui `src/Lete.Core*` e só referencia o Core); scripts do jogo em `src/Lete.Game/`. Pastas sem recursos do Godot levam `.gdignore`.

- Build: `dotnet build Lete.sln`
- Testes: `dotnet test src/Lete.Core.Tests`
- Abrir no editor: `Godot_v4.7.2-stable_mono_win64.exe --path . --editor`
- Exportar Windows: _a definir no M0_
- Sprites: `python tools/pixelizar.py <imagem.png> [--alturas 32 48 64] [--paleta "#..."]`
