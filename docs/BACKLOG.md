# BACKLOG — marcos e tarefas

> Sem datas. Cada marco só termina quando o critério de "pronto" é atendido. Construir em **fatias verticais**, com todos os sistemas-base funcionando antes de expandir conteúdo. Referências: `GDD.md`, `ARQUITETURA.md`, `DADOS.md`.

---

## M0 — Protótipo técnico

**Objetivo:** validar o visual e a base do projeto. **Pronto quando** existe uma cena parecida com o visual desejado e o tamanho do sprite foi escolhido.

- [x] Criar o projeto **Godot 4 .NET** com a estrutura de `ARQUITETURA.md` (solução com `Lete.Core`, `Lete.Core.Tests`, `Lete.Game`); `.gitignore`; repositório Git. *(Godot 4.7.2 .NET, net8.0)*
- [x] Câmera **ortográfica diagonal** fixa; cenário 3D low-poly simples (chão, paredes, uma estrutura) com **texturas em pixel art**. *(ângulo aprovado no teste do andar)*
- [x] Sprite em pé (billboard) do protagonista placeholder; andar nas 4 direções (2 desenhadas + espelho).
- [x] **Teste do andar com Aulo e Ismena** (`docs/M0_PRIMEIRO_TESTE.md`): cena `src/Lete.Game/Sandbox/WalkTest.tscn`, sprites de 56 px nas 4 direções, 12 quadros a 8 fps, Tab troca de personagem. *(falta o João testar com teclado e controle)*
- [x] **Teste de tamanho: 32 / 48 / 64 px** com os sprites de teste (Aulo e Ismena, a partir de `tools/pixelizar.py`), lado a lado no cenário e com a densidade de pixel do chão. Registrar a decisão em `docs/ARTE.md`. *(superado: sprite de mapa de 56 px, retrato de 96 px; Aulo e Ismena já andam na sala com névoa, escala 3× aprovada)*
- [x] Teste de **densidade de pixel**: render direto vs. viewport de baixa resolução ampliado (vizinho mais próximo). Escolher e registrar. *(decidido: baixa resolução)*
- [x] Luz e **névoa** básicas (a névoa do Lete é parte do tema).
- [x] Andar e **interagir** (um objeto examinável que escreve uma frase num Diário placeholder) com **teclado e controle** (Input Map com ações). *(controle mapeado, ainda não testado num controle físico)*
- [x] **Painel de debug** inicial (teleporte, flags).

## M1 — Combate

**Objetivo:** uma sombra jogável com os 5 gestos, HUD e as vias. **Pronto quando** dá para **vencer, libertar e absorver** uma sombra, e a sensação dos gestos foi avaliada jogando.

- [ ] Interface `IGesture` e **5 cenas de gesto**: Anel, Traço, Segurar, Arrastar, Esquiva (valor 0–1; limiares perfeito/bom/falho; assistência). *(`IGesture` e **Anel** prontos na cena de teste `src/Lete.Game/Sandbox/GestureTest.tscn`; sensação aprovada pelo João; Anel e **Esquiva** funcionando dentro da batalha em `src/Lete.Game/Sandbox/BattleAttackTest.tscn`; faltam Traço, Segurar e Arrastar)* *(25/out: **Segurar** e **Arrastar** prontos e usados na batalha (Escutar e Libertar); falta **Traço**)*
- [x] **Teste de estilo da batalha** (`BATALHA_TESTE.md`): cena `BattleTest` com liso/128/160 e animações por código. *(decidido: **pixel_160**, ver `ARTE.md` 2.2; versão com ataques em `src/Lete.Game/Sandbox/BattleAttackTest.tscn`, com HUD em pixel art, fundo escuro e indicadores de turno; layout normal escolhido)*
- [x] **Máquina de estados** do combate (jogador → apoio → sombra/Esquiva → checagens), turnos alternados. *(`BattleFlow` no Core, com testes; conduz `BattleAttackTest`: Ismena no papel do protagonista, Aulo com Postura a cada 2 rodadas, sombra alternando golpe direto (aparar com o Anel) e de padrão (Esquiva); derrota recomeça a luta)*
- [ ] **HUD** em layout diagonal/3D: personagens embaixo à esquerda, sombra no alto à direita, caixa de ações embaixo à direita, indicadores nos cantos superiores; menu que **some** quando o gesto começa; legenda do gesto da sombra; barra de Firmeza sem número; 3 pontos de Compreensão.
- [ ] Botões: Atacar, Memória, Aproximar (Escutar, Oferenda, Presenciar, Poupar), Eco, Bolsa, Fugir. *(25/out: Atacar; Aproximar com Escutar e Libertar funcionando; Oferenda, Presenciar e Poupar ainda respondem "ainda não"; submenus voltam com `cancel` ou "Voltar")*
- [ ] **Sombra-tutorial (Remador)** como dado em JSON (gesto, fixação, nome, vias abertas, vozes por Firmeza). *(25/out: rascunho em `content/shades/remador.json` para o João revisar, `PENDENCIAS.md` 21)*
- [ ] Vias: **Combater, Libertar, Absorver**, Presenciar, Poupar, Fugir, Deixar-se vencer, Misturar (avaliação por turno). Imunidades por sombra. *(25/out: **Combater**, **Libertar** (escolha da fixação, fúria no erro, barra de paz) e **Absorver** (Segurar, Eco em 3 espaços, Marca no debug) funcionando: as três vitórias do M1; faltam Presenciar, Poupar, Fugir e Deixar-se vencer)*
- [ ] Recursos Fôlego, Lucidez (estado *esquecida*), Firmeza, Compreensão. *(Fôlego, Firmeza e Compreensão funcionando no teste; Lucidez só aparece, falta o que a gasta e o estado esquecida)*
- [ ] **Derrota com custo** (volta à Trívia, perde oferendas/óbolos, Maré por modo) — pode usar Trívia placeholder.
- [ ] Esquema de controles de gesto para **teclado e controle**.
- [ ] Testes unitários das vias e da máquina de estados.

## M2 — Sistemas

**Objetivo:** base de sistemas e dados. **Pronto quando** salva e carrega o estado completo e o conteúdo nasce por JSON.

- [ ] **Carregador de dados** JSON com validação (ver `DADOS.md` seção 9) e **avaliador de condições** (DSL) com `NearMiss`.
- [ ] **EventBus** e eventos tipados.
- [ ] **WorldState** + **Save/Load** em JSON versionado, migrações, round-trip testado; salvar em Trívias e ao trocar de região.
- [ ] **Mosaico** (tipos, estágios, confirmação cruzada, incerta/falsa, Chave de Memória) + UI do Mosaico sem moldura.
- [ ] **Memórias Pesadas** (fila fixa, Encarar/Deixar definitivos, Culpa/Leveza).
- [ ] **Marca** (contador, faixas, efeitos visuais e condicionais), **Maré** (degraus, tabelas por região), **Perfil**, **Caminho**.
- [ ] **Diário** (sem marcadores) e **Diálogo** (grafo, posturas, estados de lucidez, opções cinzas, companheiro comenta uma vez).
- [ ] **Oferendas** (dar/partilhar, tipos, limite por tipo), **cura** (Trívias, Fogos do Lar, hospitalidade, provisões vivos/Além), **itens** (equipamento 3 espaços + 2 ex-votos, evolução Gasto/Reconhecido/Desperto, conjuntos de dono, relíquias).
- [ ] **Mercadores** que leem o Perfil, **juramentos** (máx. 3, perjúrio), **testemunhos** (Tribunal), **viagem por Trívias** (3 tochas, Asfódelos exceção, atalhos físicos).
- [ ] **Companheiro** (apoio a cada 2 turnos, aprovação por via, vínculo).
- [ ] **Localização** completa (chaves) com marcação de gênero `{m|f}`.
- [ ] **Criador de aparência** em camadas (gênero, paleta, cabelo, marca).
- [ ] **Painel de debug** completo (Marca, Maré, flags, Lembranças, juramentos).
- [ ] Testes automáticos do estado (Mosaico, Marca, Maré, juramentos, save).

## M3 — Fatia vertical

**Objetivo:** 1h a 1h30 jogáveis. **Pronto quando** é possível jogar do prólogo até parte de Asfódelos com origem e companheiro funcionando de ponta a ponta.

- [ ] Prólogo em **Cabo Tênaro** (criação, descida, Tabuinha, Remador-tutorial).
- [ ] **Aqueronte** (Caronte, óbolo e alternativas de travessia) e **parte de Asfódelos** (Trívia, Rua dos Ofícios, Altar).
- [ ] **1 a 2 origens** completas até o capítulo cabível + **1 companheiro** (Aulo sugerido).
- [ ] Primeira **Memória Pesada** (O Convite); hospitalidade e uma missão curta; um mercador (O Coveiro).
- [ ] Arte real do conjunto (protagonista, companheiro, algumas sombras, uma região) no tamanho de sprite decidido no M0.
- [ ] Rodada de teste do jogo com outras pessoas (sensação dos gestos, clareza das vias, ritmo).

## M4 — Expansão

**Objetivo:** uma região por vez, **cada região fechada antes da seguinte**, na ordem do mapa (Ato I → IV). A cada região: layout, sombras (nomeadas + comuns), regra de combate da região, Lembranças, itens, mercador, Trívia, missões, chefe/encontros, trilha e efeitos.

- [ ] Ato I: Tênaro, Vestíbulo (A Fome), Aqueronte.
- [ ] Ato II: Asfódelos completo (+ Poço das Sem-Nome), Cérbero, Campos do Luto, Tribunal, Campo dos Guerreiros (O Campeão); escolha de companheiro (Maré 2).
- [ ] Ato III: Estige (juramento obrigatório), Palácio, Hipno e Tânato (O Sonho), Mnemósine (secreta).
- [ ] Ato IV: Tártaro (As Fúrias), Elísios, Vale do Lete (A Sombra do Esquecido), Portões e finais compostos, epílogo.
- [ ] Origens restantes e companheiros restantes (seguindo o **plano de cortes**).

## M5 — Acabamento

- [ ] Os **3 modos de dificuldade** (Estige primeiro; Tártaro por último), balanceamento de números (só agora).
- [ ] Tutorial refinado, acessibilidade, remapeamento, opções de idioma.
- [ ] Áudio completo, créditos, polimento visual.
- [ ] Nome definitivo, capa, página do **itch.io**, trailer, demo; checar regras de arte gerada por IA.
- [ ] Versão pública.

---

## Para o Claude Code: ordem de trabalho inicial

1. Ler `CLAUDE.md` e todos os documentos de `docs/`.
2. Antes de implementar, **fazer as perguntas de esclarecimento necessárias** (preferência do João) e confirmar o plano do M0.
3. Montar a estrutura do M0 e entregar em pequenos passos verificáveis.
