# DADOS — esquema dos arquivos de conteúdo

> Rascunho de esquema (versão 0.1). Os campos podem mudar na implementação; a **ideia** é fixa: conteúdo em JSON tipado, validado no carregamento, com condições declarativas. Todos os **números** abaixo são **placeholders** (`"_tbd": true` quando relevantes) e não representam balanceamento.
> Os comentários `//` nos exemplos são só ilustrativos: **JSON real não aceita comentários**, então não os inclua nos arquivos.
> Convenções: IDs em `snake_case` ASCII em português; textos sempre por **chave de localização** (`"text_key"`), nunca texto direto.

---

## 1. Condições (DSL)

Usadas em diálogos, revelação de névoa, estoque de mercadores, missões, eventos e finais.

```json
{
  "all": [
    { "origin": "pescador" },
    { "recollection": { "id": "a_agua_parada", "min_stage": "recalled" } },
    { "mark_band": { "min": "mid" } },
    { "flag": "conheceu_aulo" }
  ]
}
```

- **Combinadores:** `all`, `any`, `not`.
- **Predicados previstos:** `origin`, `gender` (só para texto), `recollection` (`id`, `min_stage`, `state`: certain/uncertain/false), `recollection_kind_full` (tipo com Memória plena, para a evolução dos itens), `recollection_kind_full_count` (nº de tipos com Memória plena), `heavy_memory` (`id`, `state`: face/leave/pending), `mark_band`, `tide_min`, `flag`, `path_key`, `oath_active`, `oath_broken`, `offering_shared` (`shade`, `kind`), `shade_state` (freed/destroyed/spared/seen/absorbed/fled/yielded), `companion`, `bond_min`, `difficulty`, `profile` (via mais usada, tipo predominante), `item_owned`, `trivia_lit`, `region_visited`, `quest_stage`.
- O avaliador devolve `Pass`, `Fail` ou `NearMiss` (falha em exatamente um predicado marcado `soft`). **Opções de diálogo bloqueadas** ficam visíveis em cinza com `hint_if_blocked` (chave de texto com dica vaga).
- **Implementado (29/out/2026, `Lete.Core/Conditions`):** um predicado é um objeto com **uma chave só** (o nome) e, se for o caso, `"soft": true` (ex.: `{ "flag": "x", "soft": true }`; não vale dentro de `not`). Formatos:
  - texto (id): `origin`, `gender` (`m`/`f`), `flag`, `path_key`, `oath_active`, `oath_broken`, `companion`, `difficulty`, `item_owned`, `trivia_lit`, `region_visited`, `shade_lucidity` (só em diálogo);
  - número inteiro: `tide_min`, `bond_min`;
  - objeto: `recollection` {`id`, `min_stage`?: fragment/recalled/full, `state`?: certain/uncertain/false}, `recollection_kind_full_count` {`min`}, `heavy_memory` {`id`, `state`: face/leave/pending}, `mark_band` {`min`? e/ou `max`?: low/mid/high}, `offering_shared` {`shade`, `kind`?}, `shade_state` {`id`, `state`: freed/destroyed/spared/seen/absorbed/fled/yielded}, `profile` {`top_via`?: fight/free/absorb/witness/spare/flee/yield e/ou `top_kind`?: tipo de Lembrança; lê o Perfil **recente** e falha quando ele está neutro}, `quest_stage` {`id`, `min`: índice em `stages`}.
  - `NearMiss` = a condição falha, mas passaria se um único predicado `soft` que falhou fosse cumprido. O resultado diz qual foi.
  - Campos que guardam condição: `requires`, `when`, `gate`, `hook`, `condition`.

---

## 2. Sombra (`content/shades/<id>.json`)

```json
{
  "id": "tecela_do_luto",
  "kind": "named",                       // named | common | personification | guardian | boss
  "region": "campos_do_luto",
  "gesture_cycle": {                     // o que ela repete em vida, também usado na Esquiva
    "text_key": "shade.tecela.gesture",
    "pattern_id": "fios_cruzados",
    "cycle_length": 3
  },
  "fixation": { "id": "espera", "clue_keys": ["shade.tecela.clue1", "shade.tecela.clue2"],
    "options": [                         // o que o jogador pode acreditar ao Libertar (pistas nunca são exatas)
      { "id": "espera", "label_key": "shade.tecela.fix.espera" },
      { "id": "luto", "label_key": "shade.tecela.fix.luto", "wrong_key": "shade.tecela.corrige.luto" }  // fala que corrige a pista
    ] },
  "murmur_key": "shade.tecela.murmurio", // o que se ouve num Escutar falho ou num golpe sem fala
  "listen": [                            // Escutar: o que ela murmura em cada golpe do ciclo (contando do 0)
    { "cycle_step": 1, "reveals": "name", "text_key": "shade.tecela.nome_ouvido", "partial_key": "shade.tecela.nome_metade" }
  ],                                     // reveals: clue | name | fixation (exatamente um name e um fixation)
  "voices": [ { "resolve_fraction_below": "_tbd", "text_key": "shade.tecela.voz1" } ],   // falas conforme a Firmeza cai
  "phases": [ { "resolve_fraction_below": "_tbd", "caption_key": "shade.tecela.fase2", "reveals": "fixation",
                "attacks": [ /* ciclo novo, no formato de "attacks" */ ] } ],
  "story_keys": [ "shade.tecela.historia1" ],  // Presenciar: a história inteira
  "flee_clue_key": "shade.tecela.fuga",        // o que se ouve ao fugir
  "yield": { "lucidity_cost": "_tbd", "text_keys": [ "shade.tecela.ceder1" ] },  // Deixar-se vencer
  "defeat_clue_keys": [ "shade.tecela.hecate1" ],  // pistas de Hécate, uma por derrota
  "spare_key": "shade.tecela.poupar",
  "offering": { "accepts": "agua", "accept_key": "shade.tecela.oferenda.aceita", "refuse_key": "shade.tecela.oferenda.recusa" },
  "liberation": { "peace": "_tbd", "peace_gain": { "perfect": "_tbd", "good": "_tbd", "failed": "_tbd" },
    "lucidity_cost": "_tbd", "enraged_damage_multiplier": "_tbd",
    "progress_key": "shade.tecela.libertar.progresso", "freed_key": "shade.tecela.libertar.parte" },
  "true_name": { "id": "nome_tecela", "revealed_by": ["escutar_ciclo2", "oferenda_agua"] },
  "ways_open": { "fight": true, "free": true, "absorb": true, "witness": true, "spare": true, "flee": true },
  "ways_immune": [],
  "resolve": { "_tbd": true, "value": 100 },
  "phases": [
    { "at_resolve_pct": 100, "gesture": "fios_cruzados" },
    { "at_resolve_pct": 50,  "gesture": "fios_tensos", "reveals": "fixation" }
  ],
  "voices": [
    { "when": { "resolve_pct_below": 40 }, "text_key": "shade.tecela.voice1" }
  ],
  "attacks": [                           // ciclo de golpes no turno da sombra (repete)
    { "kind": "direct",  "gesture": "aparo_basico",  "caption_key": "shade.tecela.golpe",
      "breath_damage": { "perfect": 0, "good": "_tbd", "failed": "_tbd" } },   // aparar com o Anel
    { "kind": "pattern", "gesture": "esquiva_ondas", "caption_key": "shade.tecela.fios",
      "breath_damage": { "perfect": 0, "good": "_tbd", "failed": "_tbd" } }    // quadro da Esquiva
  ],
  "absorb": { "resolve_fraction_max": "_tbd", "weak_fill_factor": "_tbd",       // Absorver: só com Firmeza baixa
    "mark_gain": { "perfect": "_tbd", "good": "_tbd", "failed": "_tbd" } },
  "echo": { "id": "eco_fio_que_prende", "name_key": "shade.tecela.eco.nome", "description_key": "shade.tecela.eco.descricao",
    "gesture": "fios_cruzados",                // gesto em content/gestures (um Anel pode ter "repeats")
    "resolve_damage": { "perfect": "_tbd", "good": "_tbd", "failed": "_tbd" }, "mark_on_use": "_tbd" },
  "rewards": {
    "free": { "recollection_fragment": "o_fio_da_espera", "hospitality": true },
    "fight": { "obols": 0, "items": [] }
  },
  "knows_player": false,
  "dialogue_id": "dlg_tecela",
  "sprite": "shades/tecela",
  "tide_scaling": { "per_step": "table:tecela" }
}
```

Sombras **comuns** referenciam um `base_body` (um dos 6 corpos-base) e um conjunto reciclado de 4 a 5 gestos genéricos.

---

## 3. Lembrança (`content/recollections/<id>.json`)

```json
{
  "id": "a_agua_parada",
  "kind": "mourning",                  // childhood | feast | family | mourning | craft
  "scope": "origin:pescador",          // common | origin:<id> | heavy
  "title_key": "rec.a_agua_parada.title",
  "stages": {
    "fragment":  { "text_key": "rec.a_agua_parada.s1", "skill": "mem_agua_parada_1" },
    "recalled":  { "text_key": "rec.a_agua_parada.s2", "skill": "mem_agua_parada_2" },
    "full":      { "text_key": "rec.a_agua_parada.s3", "skill": "mem_agua_parada_3" }
  },
  "sources": [
    { "id": "sombra_afogado", "type": "free_shade", "shade": "afogado_do_cais" },
    { "id": "ancora_cais",    "type": "anchor", "where": "aqueronte_margem", "needs": { "item_or_offering": "agua" } }
  ],
  "upgrade_rules": {
    "fragment_to_recalled": { "needs_distinct_sources": 2 },
    "recalled_to_full": { "needs": "anchor_act" }
  },
  "uncertain_if_single_source": true,
  "contradicted_by": ["fonte_x"],      // se confirmada por esta, vira false
  "mosaic_position": { "x": 0, "y": 0 }
}
```

**Implementado (29/out/2026, `Lete.Core/Recollections`):** as regras de subida são fixas (GDD 2.1), então no lugar de `upgrade_rules`, `uncertain_if_single_source` e `contradicted_by` cada fonte diz o seu papel: `sources[]` = {`id`, `type`: free_shade | shared_offering | anchor | dream | mnemosyne | dialogue, `shade` (obrigatório nos dois primeiros), `kind` (oferenda, opcional), `reliable` (padrão true), `contradicts` (padrão false)}. `full_by`: ids dos atos que levam a Recordação à Memória plena (âncora com item/oferenda, ou missão). `skill` (na raiz, no lugar de `stages.*.skill`): o id da Memória de combate (`content/memories`) que a Lembrança dá, uma só com 3 níveis (GDD 2.1); a força é multiplicada por `stage_power` de `content/rules/mosaico.json` conforme o estágio. Libertar uma sombra e partilhar oferenda chegam sozinhos pelos eventos; as outras fontes pelo código da cena. Uma fonte que contradiz só age sobre uma Lembrança incerta. Cores das pedras e a Chave (3 tipos) em `content/rules/mosaico.json`. No save: `lembrancas` {id: {`fontes`, `ato_pleno`, `falsa`}}.

**Memória pesada:** `scope: "heavy"`, ordem fixa (`order`), `gate`: condição para aflorar, `choice`: `{ "face": { "skill": "culpa_x", "profile_effect": {...} }, "leave": { "lightness": {...} } }`. Decisão **definitiva** (sem campo de reversão).

**Implementado (30/out/2026, `Lete.Core/Systems/HeavyMemories.cs`):** no lugar de `choice`, a Pesada traz `order` (1, 2, 3... sem buracos), `gate` (opcional), `warning_key` (aviso de Hécate ou do companheiro), `surface_key` (o aflorar), `stages.full.text_key` (a verdade, mostrada ao Encarar), `skill` na raiz (a **Culpa**, dada só ao Encarar; Memória normal de `content/memories`, forte e cara) e `leave` {`text_key` (a fala ao Deixar), `lucidity_max`, `protections`} (a **Leveza**). Sem `sources` nem `full_by`: encarada, é plena; deixada, a pedra fica escura e coberta. `profile_effect` fica para o Perfil. O lugar onde aflora é um nó `HeavyMemoryPlace` (Area3D, `MemoryId`) na cena da região.

**Batalha (`content/battle/<id>.json`), participante:** além de `id`, `art`, `height`, `flip_h`, `tint`, `portrait` e `name_key`, pode ter `idle_frames` (`frames`, `fps`, `by_style`: id da versão de arte → caminho dos quadros sem o número, ex. `.../remador_flutuar_` + `01.png`); versões sem quadros usam a imagem única com o balanço por código.

**Bolsa (30/out/2026):** a Bolsa está no save (`bolsa` {`obolos`, `oferendas`, `provisoes`}), com Fôlego e Lucidez gastos (`vitais` {`folego_gasto`, `lucidez_gasta`, `curas`: fonte → degrau da Maré}). Regras: `content/rules/oferendas.json` (`kinds`, `capacity` por tipo, `blood`), `content/rules/cura.json` (`hearth_breath`, `hospitality_breath`); a Marca de cada oferenda em `marca.json` (`offering_mark`). Provisões definidas em `content/items/provisoes.json`; `bolsa_teste.json` agora só tem quantidades (`obols`, `offerings`, `provisions` id → quantidade) e enche a Bolsa no teste de batalha e no F1. Conversa com sombra: `lucidity.share_effects` (efeitos de partilhar a oferenda certa) e o efeito de nó `hospitality: true`.

**Bolsa de teste (antiga, até 30/out/2026):** `obols`, `offerings` (tipo → quantidade: mel, vinho, leite, agua, farinha) e `provisions` (`id`, `name_key`, `description_key`, `breath_restore`, `mark_gain`, `count`). **Modo de dificuldade** também traz `assist` (`window_widening`, `auto_score`, que precisa dar "bom") e `defeat_tide_steps` (0 a 2). **Fuga:** gesto de Esquiva com `exit` (`x`, `y`, `w`, `h` em frações do quadro).

**Memórias de combate (`content/memories/<id>.json`, campo `memories`):** `id`, `kind` (childhood | feast | family | mourning | craft), `name_key`, `description_key`, `stroke` (`steps`: 2 a 4 de up, up_right, right, down_right, down, down_left, left, up_left; `beat_seconds`, `zero_score_offset_seconds`, `lead_in_seconds`), `lucidity_cost`, `failed_cost_multiplier`, `effect` (heal | strike | guard | slow | focus) e `power` (perfect/good/failed). Todos os números `_tbd`.

**Memória (habilidade) `MemorySkill`:** `kind` herdado do tipo, `stroke_pattern` (conjunto de direções), `lucidity_cost` (`_tbd`), `effect`.

---

## 4. Item (`content/items/<id>.json`)

```json
{
  "id": "remo_do_afogado",
  "category": "instrument",             // instrument | garment | amulet | votive | utensil | relic
  "owner_story_key": "item.remo.story",
  "affinity": "craft",
  "set_id": "conjunto_do_afogado",
  "stages": [
    { "id": "worn",       "modifiers": [{ "attack_ring": "double_slow" }] },
    { "id": "recognized", "modifiers": [{ "attack_ring": "double_slow", "ring_forgiveness": "+_tbd" }],
      "requires": { "recollection_kind_full": "craft", "quest": "remo_reconhecido" } },
    { "id": "awakened",   "modifiers": [{ "effect": "evoca_agua" }],
      "requires": { "quest": "remo_desperto" } }
  ],
  "merchant_tags": ["combater", "oficio"],
  "icon": "items/remo"
}
```

**Chave do Caminho (`content/path/<id>.json`, implementado em 30/out/2026, `Lete.Core/Systems/PathKeys.cs`):** `id` (uma por passagem: `aqueronte`, `vale_do_lete`...), `journal_key` (frase do Diário ao obter) e `ways` [{`id`, `condition`}]: a primeira forma cuja condição vale dá a chave, conferida a cada evento. A condição `path_key` cita o `id`. Uma forma que usa um sistema que ainda não existe é pulada (aparece no F1). No save: `caminho` {chave: forma}.

**Relíquia:** `category: "relic"`, `path_key` (a chave que abre), `hint_key`, `lesson` (`blessing` + `burden`); não equipável; no alforje.
**Modificadores** são declarativos (ex.: `dodge_frame_size`, `parry_window`, `mist_resistance`, `rule_passive`) e aplicados pelo sistema de equipamento, nunca por código específico do item.

**Implementado (30/out/2026, `Lete.Core/Items`):** item = arquivo de `content/items/` com `category` (a pasta também guarda a Bolsa de teste e as provisões): `id`, `category`, `name_key`, `story_key`, `affinity`, `set` (id em `content/item_sets/`), `unlocks_path` (relíquia: a chave do Caminho que ela abre; no lugar de `path_key`, que o validador leria como chave de texto) e `stages` [{`id`: worn | recognized | awakened, `when` (condição; o primeiro não tem), `story_key` (linha de história que vai para o Diário ao evoluir), `modifiers`}]. O item evolui sozinho quando a condição vale (decisão do João). Modificadores por enquanto: `parry_window` (alarga a janela do aparo), `dodge_box` (aumenta o quadro da Esquiva, fração), `breath_max`, `lucidity_max`. Conjunto (`content/item_sets/<id>.json`): `id`, `name_key`, `story_key`, `pieces`, `modifiers` (valem quando o jogador tem todas as peças). Valem os modificadores do equipado, dos conjuntos completos e das relíquias do alforje. Condição nova: `recollection_kind_full` (tipo com Memória plena). No save: `itens` {`tem` (id → estágio), `equipado` (espaço → id), `conjuntos`}.

---

## 5. Diálogo (`content/dialogue/<id>.json`)

```json
{
  "id": "dlg_padeiro",
  "lucidity_states": ["apagada", "vaga", "desperta"],
  "nodes": [
    {
      "id": "vago_inicio",
      "when": { "shade_lucidity": "vaga" },
      "line_key": "dlg.padeiro.vago.linha1",
      "options": [
        { "posture": "escutar",  "text_key": "dlg.padeiro.vago.escutar",  "next": "vago_escutar" },
        { "posture": "perguntar","text_key": "dlg.padeiro.vago.perguntar","next": "vago_nao_entende" },
        { "posture": "calar",    "text_key": "dlg.padeiro.vago.calar",    "next": "fim" },
        { "posture": "escutar",  "text_key": "dlg.padeiro.vago.cheiro_forno",
          "requires": { "recollection": { "id": "as_maos_de_barro", "min_stage": "fragment" } },
          "hint_if_blocked": "dlg.padeiro.hint.cheiro_forno", "show_blocked": true, "next": "vago_reconhece" }
      ]
    }
  ]
}
```

**Implementado (30/out/2026, `Lete.Core/Dialogue`):** o arquivo tem `id` (igual ao nome do arquivo), `mode` (shade | fixed | scene), `speaker_key`, `shade` (id da sombra, para a oferenda), `lucidity` (só e sempre no modo shade: `base`, `raise` [{`to`, `when`}], `offering`: o tipo que a desperta; sem ele, qualquer oferenda), `start` (nós de entrada, na ordem: começa pelo primeiro cujo `when` vale) e `nodes`. Nó: `id`, `when`, `line_key`, `options`, `next` (nó sem opções segue para ele; `fim` ou nada encerra), `effects` (objetos de uma chave: `journal_key`, `flag`, `origin_fragment`, `hear` {`recollection`, `source`}, `swear_oath`, `break_oath`) e `comment` {`id`, `lines` [{`companion`, `line_key`}]} (o companheiro comenta uma vez por partida; o jogador concorda, discorda ou ignora, evento `CompanionCommented`). Opção: `posture`, `text_key`, `next`, `requires`, `hint_key` (no lugar de `hint_if_blocked`, para o validador conferir a chave) e `show_blocked` (exige `hint_key`); uma opção que falha só num predicado `soft` também aparece em cinza. Se o nó não tiver Calar disponível, a conversa ganha um que encerra. A oferenda certa (`OfferingGiven`/`OfferingShared`) desperta a sombra até o fim da próxima conversa. No save: `dialogo` {`escolhidas`, `comentarios`, `despertas`}. Na sala de teste, o Padeiro (`content/dialogue/padeiro.json`).

Posturas: `escutar`, `perguntar`, `pressionar`, `consolar`, `calar` (`calar` sempre disponível). Rótulos entre colchetes (`[Artesão]`, `[Memória de Ofício]`) vêm de `requires`. Efeitos de nó: `effects` (lista de eventos/alterações: ajustar vínculo, adicionar Diário, conceder Fragmento). **Estados de lucidez** (apagada/vaga/desperta) dependem de oferendas e do Mosaico.

---

## 6. Região (`content/regions/<id>.json`)

```json
{
  "id": "campos_do_luto",
  "act": 2,
  "areas": ["entrada", "jardim_de_mirto", "campo_aberto"],
  "connections": [{ "to": "asfodelos", "via": "trivia_luto" }],
  "combat_rule": { "id": "lagrimas", "text_key": "rule.lagrimas", "effect": "slow_all" },
  "tide_table": [
    { "step": 1, "effects": [] },
    { "step": 3, "effects": [{ "unresolved_shades_difficulty": "_tbd" }, { "spawn_new": ["sombra_x"] }] }
  ],
  "mist_default": "dense",
  "trivia": "trivia_luto",
  "merchant": "viuva_do_cocito",
  "mood": { "accent_color": "#8FB3C9", "music": "mus_luto" }
}
```

---

## 7. Modo de dificuldade (`content/difficulty/<modo>.json`)

```json
{
  "id": "estige",
  "gesture_windows": { "perfect": "_tbd", "good": "_tbd" },
  "defeat": { "lose_offerings_pct": 100, "lose_obols_pct": 100, "tide_steps": 1 },
  "boss_extra_phase": false,
  "extra_immunities": false,
  "resource_scarcity": { "offerings_multiplier": "_tbd", "trivia_spacing": "normal" },
  "hecate_hints": "qualitative"
}
```
Modos: `asfodelo` (janelas largas; derrota perde parte das oferendas, Maré +0; dicas claras), `estige` (padrão), `tartaro` (janelas estreitas; Maré +2; fases extras e mais imunidades; recursos escassos; dicas quase nenhuma). A **assistência de gestos** fica em `settings`, fora do modo.

---

## 8. Origem, companheiro, juramento, julgamento, missão

- **Origem** (`origins/`): `id`, `ambition_key`, `betrayed_key`, `alternative` (efeito mecânico poupado), `friction` (efeito mecânico cobrado), `outfit`, `exclusive_recollections` (3 ids), `chain` (4 capítulos com `act`, `quest_id`).
- **Companheiro** (`companions/`): `id`, `meet_region`, `support` (`id`, `effect`, `interval_turns`, `window_widening` na Postura), `approval` (mapa via→ aprova/tolera/reprova), `chapters` (0 a 4 com `quest_id`), `gate_offer` (`id`, `scene_id`), `portrait` (3 a 4 expressões).
- **Juramento** (`oaths/`): `id`, `blessing`, `heel`, `condition` (como condição DSL avaliável ao longo do tempo), `giver`, `conflicts_with`. Perjúrio publica `OathBroken` (Marca sobe, bênção some).
  - **Implementado (30/out/2026, `Lete.Core/Systems/Oaths.cs`):** `name_key`, `promise_key`, `blessing_key`, `heel_key` (textos), `breaks_on` (vias que quebram: fight/free/absorb/witness/spare/flee/yield), `kept_when` (condição que cumpre; opcional) e `expires_when` (condição que vence o prazo antes de cumprir = perjúrio; opcional), `blessing`/`heel` {`lucidity_max`, `echoes_blocked`} (só os efeitos que a batalha já lê). Atos que não são vias (revelar um nome) chegam pela cena (`Oaths.Break`). Máximo ativo em `content/rules/juramentos.json`; Marca do perjúrio em `marca.json` (`perjury`). `giver` e `conflicts_with` ainda não existem. No save: `juramentos` {id: ativo | quebrado | cumprido}.
- **Julgamento** (`trials/`): `id`, `judge`, `shade`, `evidence_slots`, `verdicts` (destinos e consequências), `perjury_if` (Lembrança incerta/falsa como prova).
  - **Implementado (30/out/2026, `Lete.Core/World/Tribunal.cs`):** `id`, `judge_key`, `shade`, `intro_key`, `slots`, `evidence` [{`journal` | `recollection`, `weight`: positivo a favor da sombra}], `perjury_if_uncertain` (Lembranças cuja versão incerta é perjúrio; a falsa sempre é) e `verdicts` [{`destination`: asfodelos | elisios | tartaro, `min`, `text_key`}] do maior mínimo ao menor, o último sem `min`. Nota = pesos das provas + bônus da postura (`content/rules/tribunal.json`); calar não apresenta prova. No save: `julgamentos` {id: destino}, `perjurios`, e o Perfil guarda as posturas (`perfil.tribunal`).
- **Mercador** (`merchants/`, implementado em 30/out/2026, `Lete.Core/World/Merchants.cs`): `id`, `name_key`, `greeting_key`, `stock` [{`item` | `offering` | `provision`, `price`, `count` (não no item), `tags`: vias e tipos de Lembrança; sem etiqueta = neutro}]. O que não combina com o Perfil recente custa `off_path_price_factor` a mais (`content/rules/mercadores.json`). Oferendas e provisões repõem a cada degrau da Maré. No save: `mercadores` {mercador/entrada: [vendidas, degrau]}.
- **Trívia** (`trivias/`, implementado em 30/out/2026, `Lete.Core/World/Trivias.cs`): `id`, `region`, `name_key`, `torches` (até 3 Trívias) e `hub` (a de Asfódelos liga a todas as acesas). A condição `trivia_lit` cita o `id`. No save: `trivias_acesas`.
- **Missão** (`missions/`): `id`, `type` (region | offering | origin | companion), `hook` (condição de aparecer), `stages` (lista com condição de avanço), `consequences`, `closed_by_fight` (bool). Sem campo de marcador de mapa.

---

## 9. Validação no carregamento

O carregador deve falhar de forma clara (arquivo, campo, mensagem) quando:
- um ID referenciado não existe (sombra → Lembrança, item → missão, etc.);
- uma chave de localização não existe no idioma padrão;
- uma condição usa predicado desconhecido;
- um chefe não tem pelo menos uma saída sem dano total;
- uma sombra comum não tem `base_body`;
- um valor `_tbd` é usado em build de release (apenas aviso em desenvolvimento).

**Implementado (29/out/2026):** `Lete.Core/Data/ContentValidator.cs`, chamado pelo `GameRoot` ao abrir o jogo (erros no console do Godot, com arquivo → campo: mensagem) e pelo teste `ContentTests.Content_PassesLoadValidation`. Confere JSON, chaves de texto (`*_key`, `*_keys`), condições (formato e ids citados) e as referências `battle.shade`, `battle.companion` e `shades.*.gesture`. As regras de chefe e de `base_body` entram quando esses dados existirem.
