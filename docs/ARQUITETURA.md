# ARQUITETURA — Projeto Lete (título provisório)

> Escopo técnico decidido em 23/out/2026. Versão 0.1.
> Itens marcados **(recomendação)** não foram decididos pelo João; seguir por padrão, mas explicar antes de divergir. Itens **(decidido)** só mudam com aprovação dele.

---

## 1. Stack

| Item | Decisão |
|---|---|
| Engine | **Godot 4 com C# (.NET)** (decidido). Fixar a versão estável mais recente do Godot 4 .NET no repositório e na documentação |
| Linguagem | **C#**, com orientação a objetos (preferência do João: foi a primeira linguagem que aprendeu) (decidido) |
| Plataforma | **Windows**, com **suporte a controle desde o início** (decidido). Exportar para Windows desktop; não exportar para web |
| Cenário | **3D low-poly com texturas em pixel art, câmera ortográfica diagonal, sprites em pé (billboards)** (decidido, "opção A"). Validar no marco M0 |
| Dados | Arquivos **JSON** em `content/`, carregados em classes C# tipadas **(recomendação)** |
| Controle de versão | Git |

Observação: a maioria dos tutoriais de Godot é em GDScript. Aqui, tudo em C#; ao seguir tutoriais, traduzir.

---

## 2. Três regras de arquitetura (decididas)

1. **O conteúdo vira dado, não código.** Sombras, falas, itens, Lembranças, juramentos, missões e regiões ficam em arquivos de dados com **condições simples** ("só aparece se origem = Pescador e a Lembrança de Luto estiver em Recordação"). O código implementa **regras**, não conteúdo. Como o maior esforço do jogo é conteúdo, acrescentar uma sombra ou uma fala não deve exigir programação.
2. **Um único estado do mundo, serializável** (`WorldState`). Salvar é gravar esse estado; carregar é restaurá-lo.
3. **Os sistemas conversam por eventos.** Quando algo acontece ("sombra libertada", "Lembrança obtida"), um evento é publicado. Diário, Perfil, missões, mercadores e Maré apenas **escutam**, sem um conhecer o outro.

---

## 3. Camadas e estrutura de pastas (recomendação)

Separar a **lógica pura em C# (sem tipos do Godot)** da camada que usa o Godot. Assim, os sistemas podem ser testados com testes unitários comuns, sem abrir o editor. Isso importa porque as combinações de estado (Marca × Maré × Mosaico × Pesadas × juramentos) geram bugs fáceis de esconder.

```
/ (raiz do repositório)
├── CLAUDE.md
├── docs/                      # GDD, HISTORIA, ARQUITETURA, DADOS, ARTE, BACKLOG, PENDENCIAS
├── tools/
│   └── pixelizar.py           # Gemini -> sprites 32/48/64 px (ver ARTE.md)
├── src/
│   ├── Lete.Core/             # C# puro, SEM referência ao Godot
│   │   ├── Events/            # EventBus e tipos de evento
│   │   ├── State/             # WorldState e sub-estados, Save/Load, migrações
│   │   ├── Conditions/        # avaliador de condições (DSL do DADOS.md)
│   │   ├── Systems/           # Mosaic, Mark, Tide, Profile, Journal, Offerings,
│   │   │                      # Oaths, Testimony, Merchants, Travel, Companion, Path
│   │   ├── Combat/            # máquina de estados, vias, fases, resultado de gesto
│   │   ├── Dialogue/          # grafo, estados de lucidez, opções cinzas
│   │   └── Data/              # classes de definição + carregador JSON + validação
│   ├── Lete.Core.Tests/       # xUnit
│   └── Lete.Game/             # camada Godot: cenas, nós, UI, entrada, áudio
│       ├── World/             # câmera, jogador, sombras no mapa, interativos, névoa
│       ├── Combat/            # HUD, Gestures/ (5 cenas), quadro da Esquiva
│       ├── Dialogue/ UI/ Debug/
├── content/                   # JSON do jogo (ver DADOS.md)
│   ├── regions/ shades/ recollections/ items/ oaths/ trials/
│   ├── dialogue/ missions/ companions/ origins/ difficulty/ locale/
├── art/
│   ├── concept/               # imagens do Gemini (brutas)
│   ├── sprites/ tiles/ portraits/ ui/ icons/
└── project.godot
```

Cuidados do Godot: arquivos de `content/` precisam estar dentro de `res://` e incluídos nos filtros de exportação (`*.json`) para existirem no executável. Se a solução de duas camadas atrapalhar o fluxo do João, a alternativa é uma só camada com as mesmas pastas; manter, de qualquer forma, **Systems e Combat sem dependência de nós do Godot**.

---

## 4. Estado do mundo, eventos e save

### 4.1 WorldState (único, serializável)
Campos mínimos: dados do protagonista (`corpo` m/f, escolhido no reflexo e fonte do gênero dos textos; `origem`: pescador, sacerdote, veterano ou artesao; modo de dificuldade travado; **sem aparência**), fragmentos de origem ganhos, etapa da Marca (0 a 3) e `rosto_final` (nenhum, devolvido ou coberto, calculado pela regra C), recursos (Fôlego, Lucidez atual e máximo), Marca (contador interno), Maré (valor), Mosaico (lista de Lembranças com estágio, fontes confirmadas e estado incerta/falsa; Pesadas encaradas/deixadas), Perfil (contadores de vias e janela de comportamento recente), Caminho (chaves obtidas), inventário (oferendas por tipo, provisões, equipamento, ex-votos, utensílios, relíquias, óbolos), juramentos ativos e quebrados, testemunhos dados, missões (estado por capítulo), sombras (resolvida/ poupada/ destruída/ vista), Trívias acesas, atalhos abertos, regiões visitadas, companheiro escolhido (um dos 6) e vínculo, Diário (lista de entradas), flags de cena, posição.

### 4.2 EventBus
Eventos tipados (structs ou classes). Lista inicial: `ShadeFreed`, `ShadeAbsorbed`, `ShadeDestroyed`, `ShadeWitnessed` (Presenciar), `ShadeSpared`, `ShadeFled`, `ShadeYielded` (Deixar-se vencer), `PlayerDefeated`, `MemoryUsed` (Memória usada em combate, para o Perfil), `RecollectionGained`, `RecollectionAdvanced`, `RecollectionFalsified`, `HeavyMemoryChosen` (Face | Leave), `MarkIncreased`, `TideAdvanced` (com causa: marco, viagem, derrota), `OfferingGiven`, `OfferingShared`, `OathSworn`, `OathBroken`, `OathKept`, `DialogueEnded`, `CompanionCommented`, `Rested`, `Healed`, `ItemSetCompleted`, `TestimonyGiven`, `TriviaLit`, `FastTravelUsed`, `ItemAcquired`, `ItemEvolved`, `CompanionBondChanged`, `CompanionChosen`, `OriginChosen`, `OriginFragmentGained`, `JournalEntryAdded`, `RegionEntered`, `BossPhaseChanged`, `DialogueChoiceMade`, `GateReached`.

### 4.3 Save
- Salvar **nas Trívias e ao trocar de região** (decidido), nunca antes de uma luta.
- **JSON legível**, com **número de versão** e **migração** para atualizações. Um slot por perfil de jogo mais autosave de região (recomendação).
- Testes automáticos: salvar → carregar → estado idêntico.
- **Implementado (29/out/2026):** `Lete.Core/State/SaveGame.cs`, versão 1, chaves em português (`versao`, `corpo` = id do elenco `sombra_m`/`sombra_f`, `genero`, `origem`, `companheiro`, `marca`, `marca_etapa`, `rosto_final`, `mare`, `mare_marcos`, `lembrancas`, `memorias_pesadas` {id: encarou|deixou}, `fragmentos_origem`, `diario`, `regiao`, `posicao` [x, y, z], `itens` {`tem`, `equipado`, `conjuntos`}, `bolsa` {`obolos`, `oferendas`, `provisoes`}, `vitais` {`folego_gasto`, `lucidez_gasta`, `curas`}, `dialogo` {`escolhidas`, `comentarios`, `despertas`}, `juramentos` {id: ativo|quebrado|cumprido}, `caminho` {chave: forma}, `perfil` {`vias`, `tipos` (contagens), `recentes` [{`via`, `tipos`}]}, `flags`). Migração: uma função por versão em `SaveGame.Migrations` (N → N+1).

### 4.4 Debug
Incluir desde o início um **painel de debug** (tecla dedicada) para editar Marca, Maré, flags, Lembranças, juramentos, teleporte entre regiões e forçar eventos. Combinações de estado são a maior fonte de bugs.

---

## 5. Como cada mecânica vira código

| Mecânica | Implementação |
|---|---|
| Os cinco gestos | **Cinco cenas independentes com a mesma interface** (`IGesture`): recebem parâmetros (velocidade, padrão, janela) e devolvem um valor de **0 a 1** (proximidade do ideal). Limiares viram **perfeito / bom / falho**; o **modo de dificuldade e a assistência** só ajustam os limiares |
| Combate por turnos | **Máquina de estados**: menu do jogador → gesto → resolução → apoio do companheiro (a cada 2 turnos) → turno da sombra (quadro de Esquiva) → resolução → checagem de fim/fase. Cada sombra é um **dado** com gesto, fixação, nome, vias abertas, fases e falas por Firmeza |
| Vias de resolução | Cada via é uma estratégia com **janela de abertura** (condição) e **resolução** (efeitos publicados como eventos). Misturar = vias avaliadas a cada turno, não escolhidas no início |
| Névoa e percepções | Cada objeto/sombra/passagem tem uma **condição de revelação** ligada ao Mosaico (tipo e estágio). A névoa é só uma **camada visual** por cima. Percepção ativa = função do tipo de Lembrança e do estágio |
| Maré | Um número global. **Tabela por região** diz o que muda a cada degrau (dificuldade, sombras novas, névoa) |
| Diálogo | **Grafo de nós com condições**. Opções cinzas = as que falham na condição e têm `hint_if_blocked` |
| Mosaico | Lista de Lembranças com estágio e **fontes confirmadas**. A contagem e o conteúdo das fontes decidem entre *incerta*, *confirmada* e *falsa* |
| Viagem | **Grafo de Trívias acesas**, com até 3 ligações cada; Asfódelos é exceção (liga a todas as abertas) |
| Dificuldade | Um **arquivo de parâmetros por modo**, travado ao iniciar. Assistência de gestos fica separada e editável a qualquer hora |
| Equipamento | Cada item define modificadores de forma declarativa (ver `DADOS.md`); o jogo aplica a pilha de modificadores |
| Marca | Contador interno → faixa (baixa/média/alta) → efeitos visuais (shader de cor, sombra, pegadas) e condições de diálogo/travessia |
| Câmera e visual | Câmera ortográfica diagonal; sprites 2D em `Sprite3D` com billboard; texturas do cenário na **mesma densidade de pixel** dos sprites. **Teste no M0:** renderizar a cena 3D num viewport de baixa resolução e ampliar com filtro de vizinho mais próximo, para unificar a densidade de pixel (técnica comum), e comparar com render direto |

---

## 6. Entrada e controles

- Usar o **Input Map** do Godot com **ações** (`confirm`, `cancel`, `move_*`, `gesture_*`), nunca teclas fixas no código.
- Teclado/mouse e controle compartilham o mesmo conjunto de ações. Gestos: direcionais/analógico + um botão de confirmação.
- **Remapeamento** nas opções.

---

## 7. Localização e gênero

- **Nenhum texto fixo no código.** Toda fala e rótulo usa **chave de localização** (`dlg.padeiro.vago.escutar`). Português do Brasil primeiro; inglês opcional depois.
- **Gênero do protagonista** (vem do **corpo** escolhido no reflexo do rio, 26/out/2026): preferir **formas neutras** na escrita; onde o gênero for inevitável, usar a marcação `{m|f}` no texto, por exemplo `cansad{o|a}`, resolvida na hora de exibir pelo gênero escolhido. Variar manualmente só as **falas-chave**. O corpo muda a silhueta (dois sprites); as falas dos companheiros que se dirigem a ele usam a mesma variação.
- Entradas de Diário também passam pelo mesmo mecanismo.

---

## 8. Glossário (termo do jogo → nome em código)

**Convenção (recomendação):** identificadores de código em **inglês**; **IDs de conteúdo** (nos JSON) em `snake_case` ASCII, em português, sem acento (ex.: `tecela_do_luto`, `a_agua_parada`).

| Jogo | Código | Jogo | Código |
|---|---|---|---|
| Mosaico | `Mosaic` | Maré | `Tide` |
| Lembrança | `Recollection` | Perfil | `Profile` |
| Tipo de Lembrança | `RecollectionKind` {`Childhood`, `Feast`, `Family`, `Mourning`, `Craft`} | Caminho | `Path` |
| Estágio | `RecollectionStage` {`Fragment`, `Recalled`, `Full`} | Diário | `Journal` |
| Incerta / Falsa | `Uncertain` / `False` | Trívia (de Hécate) | `Crossroads` (ou `Trivia`) |
| Memória (habilidade) | `MemorySkill` | Fogo do Lar | `Hearth` |
| Memória Pesada | `HeavyMemory` (Encarar `Face`, Deixar `Leave`) | Juramento / Perjúrio | `Oath` / `Perjury` |
| Culpa / Leveza | `Guilt` / `Lightness` | Testemunho | `Testimony` |
| Marca (dos Mortos) | `Mark` (faixas `Low`/`Mid`/`High`) | Sombra | `Shade` |
| Eco | `Echo` | Fôlego / Lucidez | `Breath` / `Lucidity` |
| Oferenda | `Offering` | Firmeza / Compreensão | `Resolve` / `Understanding` |
| Combater / Libertar / Absorver | `Fight` / `Free` / `Absorb` | Presenciar / Poupar / Fugir | `Witness` / `Spare` / `Flee` |
| Aproximar | `Approach` | Bolsa / Alforje | `Satchel` / `Pouch` |
| Relíquia / Ex-voto / Utensílio | `Relic` / `Votive` / `Utensil` | Óbolo | `Obol` |
| Origem / Companheiro | `Origin` / `Companion` | Tabuinha Rasurada | `ErasedTablet` |
| Gestos: Anel, Traço, Segurar, Arrastar, Esquiva | `Ring`, `Stroke`, `Hold`, `Drag`, `Dodge` | Fase (de chefe) | `Phase` |
| Névoa | `Mist` | Chave de Memória | `MemoryKey` |

---

## 9. Riscos técnicos e cuidados

- **Conteúdo pesa mais que código** (escrita, arte, sprites): por isso fatias verticais (`BACKLOG.md`).
- **A sensação dos gestos só se descobre jogando**: protótipo cedo (M1).
- **Sprites consistentes**: o Gemini oscila de um quadro para outro; processo e tamanho padrão em `ARTE.md`.
- **Estado combinatório**: painel de debug e testes automáticos do estado desde o início.
- **Densidade de pixel 2D+3D**: validar no M0 antes de produzir arte em volume.
- **Performance**: low-poly com sprites é leve; vigiar apenas névoa volumétrica e iluminação.
- **Tutoriais em GDScript**: traduzir para C#.

---

## 10. Convenções de código (recomendação)

- C# idiomático, nulabilidade habilitada, `PascalCase` para tipos e métodos, `camelCase` para locais. Uma classe por arquivo.
- Preferir **composição e interfaces** a heranças profundas; dados imutáveis (`record`) para definições carregadas do JSON.
- **Nada de números mágicos** de balanceamento no código: tudo em `content/difficulty/` ou nas definições de dados (ver `PENDENCIAS.md`).
- Testes unitários para: avaliador de condições, Mosaico (incerta/falsa/confirmação), Maré, Marca, juramentos (perjúrio), save/load (round-trip e migração), vias de combate.
- Commits pequenos, com mensagem descritiva **em português** (decidido em 23/out/2026).
