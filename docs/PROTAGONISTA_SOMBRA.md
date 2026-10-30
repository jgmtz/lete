# Protagonista-sombra (decisão do João, 26/out/2026; arte pronta em 29/out)

> Fonte de verdade das mudanças do protagonista. Junta a proposta aceita (26/out) e o documento de alterações na programação (29/out), como vieram do João. Substitui `PROTAGONISTAS_FIXOS.md` e `LIA_PROPOSTA.md`. Pendências: `PENDENCIAS.md` 40 a 47.

## Parte 1: Protagonista-sombra, 4 origens e 6 companheiros (decisões do João, 26/out/2026)

**Substitui** `entregas/2026-10-25_protagonistas_fixos` (6 protagonistas fixos). Volta à ideia do GDD 2.3 (o protagonista não lembra do próprio rosto), com mudanças. Tudo marcado *(proposta)* foi sugerido pelo Claude e o João aceitou na conversa, mas os detalhes ainda podem mudar. O Claude Code **não deve inventar** o que não está aqui: registrar dúvidas em `docs/PENDENCIAS.md`.

### 1. Elenco

- **Protagonista:** uma **sombra sem aparência**. No jogo é um corpo de silhueta escura, com o rosto como uma mancha lisa, sem traços. Duas versões de corpo: **masculina** e **feminina** (o gênero se lê pela silhueta: ombros e cabelo). O jogador escolhe entre as duas no **reflexo do rio** (ver `CUTSCENE`, `entregas/2026-10-26_cutscene_abertura`, atualizada aqui).
- **Companheiros (6):** Aulo (soldado), Ismena (carpideira), Dimas (saqueador), Septímia (vestal), Cálias (aedo) e **Lia** (retratista, entra como sexta). 3 homens e 3 mulheres. **Um companheiro por partida**, como antes. Os 6 já têm arte de batalha e de mapa.
- **Lia como companheira:** precisa de capítulos, fixação, oferta final e vínculos, como os outros. *Decidir se entra completa (5 capítulos) ou reduzida* (pendência).

### 2. Origens: 5 viram 4

Ficam **Pescador, Sacerdote, Veterano e Artesão**. O **Estrangeiro sai** (nunca teve arte). A origem **não muda o sprite** do protagonista: ela aparece no texto, nas falas, nas alternativas e atritos (ver `HISTORIA.md` seção 4) e em um **símbolo** por origem (Pescador: vara de pesca; Sacerdote: cesto de oferendas com fita; Veterano: ombreira de bronze; Artesão: pote de barro com espátula). Decisão do João (26/out): na tela de escolha os 4 símbolos aparecem lado a lado, não há variação de roupa por origem. A escolha de origem vem **logo depois do reflexo**, antes do prólogo, para ficar definida desde o início.

### 3. Origens x companheiros (6 pares)

Com 4 origens há exatamente 6 pares possíveis. Cada companheiro liga-se a um par diferente: cada origem fica com 3 companheiros, cada companheiro com 2 origens. Nenhum companheiro é favorito de uma origem só; para qualquer origem, metade do elenco é "próximo".

| Companheiro | Origens ligadas | Tema do vínculo *(proposta, sem história escrita ainda)* |
|---|---|---|
| Aulo | Veterano + Sacerdote | guerra, dever, obediência |
| Ismena | Pescador + Sacerdote | afogados, luto, ritos |
| Cálias | Pescador + Veterano | canta o mar e a guerra |
| Lia | Pescador + Artesão | retratos, ofício |
| Dimas | Veterano + Artesão | saque de guerra, objetos |
| Septímia | Sacerdote + Artesão | culto, lamparina, tecido ritual |

**Regra:** companheiro nunca tem a "mesma origem" que o protagonista (não há origem igual). O vínculo só dá **história**, nunca poder (as origens continuam sem vantagem).

### 4. Fragmentos de origem (a mecânica nova)

Quando o jogador encontra, conversa, liberta ou viaja com **um espírito nomeado ou companheiro ligado à sua origem**, ganha **um fragmento extra de memória sobre si mesmo** (um pedaço da vida passada: o ofício, a ambição, um rosto, um lugar). É conteúdo narrativo, **não é poder**, e entra no Mosaico/Diário. Como cada origem tem a mesma quantidade de ligações (3 companheiros e 2 espíritos nomeados), o total é igual para todas. Integra-se à cadeia de 4 capítulos (o capítulo 1 "Eco" já é "uma sombra o reconhece pela origem"). Número de fragmentos por origem: **a definir** (pendência).

### 5. Espíritos nomeados: mínimo de 2 por origem

Existentes: **Remador** (Pescador), **Tecelã** e **Padeiro** (Artesão). Faltam 5 *(ideias do Claude, nome e papel a confirmar pelo João)*:

| Origem | Espírito 1 | Espírito 2 (novo) |
|---|---|---|
| Pescador | Remador (pronto) | **A Âncora**: marinheira afogada que carrega a âncora |
| Sacerdote | **O Sacristão**: cuidava do altar, esqueceu o rito | **A Oferente**: cuja oferenda nunca chegou |
| Veterano | **O Desertor** | **O Trombeteiro**: tocava a retirada |
| Artesão | Tecelã (pronta) | Padeiro (pronto) |

Cada um precisa de: gesto de ofício em vida, estados *vago/desperto*, fixação e um fragmento de origem (seção 4). Pipeline de arte: 4 imagens por sombra (batalha, mapa F, C, PD), como Remador, Tecelã e Padeiro.

### 6. Inventário e a Marca (romã)

O GDD já tem a **Bolsa**; esta é uma tela **nova de inventário/personagem** com a silhueta do protagonista (a mesma do reflexo que ele escolheu). A **romã evolui sobre a sombra** em etapas, e o **rosto é a última** parte a mudar.

| Etapa | Gatilho (já no GDD 2.3) | O que aparece na silhueta |
|---|---|---|
| 0 | início | silhueta limpa, rosto liso |
| 1 | primeiro Eco absorvido | romã rachada na mão |
| 2 | Marca média | manchas de romã nos dedos, mãos e antebraços; cor some um pouco |
| 3 | Marca alta | a mancha sobe pelo tronco; sem sombra projetada e sem pegadas (já no GDD) |
| Rosto | final / Marca máxima | **regra C**, ver seção 7 |

**Mudança no `ARTE.md`:** o texto "dedos e lábios manchados" passa a "dedos e mãos manchados". O **rosto (inclusive lábios) é o último a mudar**.

### 7. Regra C: o rosto no fim (escolha do João)

O rosto depende de como o jogador tratou as **Memórias Pesadas** (HISTORIA 1.2):
- **Encarou as seis (ou a maioria):** o rosto **volta**: aparecem os traços verdadeiros dele (o de quem fez a Venda do Nome), com a vergonha junto.
- **Deixou (a maioria):** a romã **cobre o rosto** (casca vermelha, rachada, com sementes), como o gesto do Gole.
- **Meio-termo:** *a definir* (pendência: limiar exato e se há uma versão mista).

A imagem aparece **no inventário** e na **cena final no reflexo da água** (fecha a abertura: ele começa sem rosto diante da água e termina com o rosto, ou sem ele, diante da água). Também deve ser coerente com os finais de HISTORIA 3 (por exemplo "Absorver + Marca alta: passa como sombra entre os vivos").

Arte: **2 imagens de rosto por corpo** (rosto devolvido, rosto coberto), mais as etapas 1 a 3 (ver `LISTA_SPRITES.md`).

### 8. Cutscene (atualiza `2026-10-26_cutscene_abertura`)

1. Novo jogo → primeira pessoa, desorientado, piscando, andando.
2. Vê o rio, ajoelha, reflexo **sem rosto**.
3. O jogador escolhe entre **dois reflexos** (homem ou mulher) movendo para os lados (era o carrossel de 6 bustos).
4. **Logo depois**, escolhe a **origem** (4 cartas lado a lado: símbolo, ofício, ambição, 2 a 3 linhas de "quem foi", sem revelar o ato).
5. Segue para o prólogo em Tênaro.

Fim do jogo: retorna ao reflexo (seção 7).

### 9. Impactos nos documentos

- `GDD.md` 2.3 (Marca, aparência), seção 9 (tela inicial, gênero), companheiros 5→6, origens 5→4.
- `HISTORIA.md` seção 4 (origens), 1.1 (tabela ambição/traído sem Estrangeiro), tabela de aprovação dos companheiros (nova coluna/linha para a Lia e as 4 origens), seção 3 (epílogo com o rosto).
- `ARTE.md` seção 4 (protagonista), seção 6 (sombras nomeadas, 6 corpos comuns).
- `ARQUITETURA.md` (variação de gênero: vem da escolha do reflexo; salvar `genero` + `origem`).
- Salvamento: guardar `genero` (m/f), `origem` (4), `rosto_final`, `marca`.

## Parte 2: Alterações na programação original (29/out/2026)

Este documento lista o que muda em relação ao que está no GDD, na HISTORIA, na ARTE e na ARQUITETURA, depois das decisões do João em 26/out/2026 e da arte do protagonista-sombra já aprovada. Complementa `../2026-10-26_protagonista_sombra_12-50/PROPOSTA_PROTAGONISTA_SOMBRA.md` (decisões e motivos). **Regra para o Claude Code: não inventar.** O que está marcado *(proposta)* foi sugerido pelo Claude e aceito pelo João, mas os detalhes ainda são abertos; o que está em "Pendências" não deve ser decidido sozinho.

### 1. Antes e depois

| Tema | Antes (documentos originais) | Depois |
|---|---|---|
| Protagonista | Personalizável: gênero, pele, cabelo, olhos, estilo, marca, escolhidos na poça de Tênaro (GDD 2.3 e seção 9) | **Sombra sem aparência**, rosto liso sem traços; só há **dois corpos** (masculino e feminino). Sem criador de personagem, sem cor de pele/cabelo/olhos, sem estilo de cabelo |
| Escolha de gênero | Opção no menu inicial | Escolha de **um de dois reflexos** (homem ou mulher) no rio, na cutscene de abertura |
| Origens | 5 (Pescador, Sacerdote, Veterano, Artesão, Estrangeiro) | **4**: Pescador, Sacerdote, Veterano, Artesão. **Estrangeiro removido** |
| Escolha de origem | Dentro da criação de personagem | Tela própria **logo depois do reflexo**: 4 cartas lado a lado, cada uma com o **símbolo** da origem |
| Companheiros | 5 (Aulo, Ismena, Dimas, Septímia, Cálias), independentes das origens | **6** (entra a **Lia**), 3 homens e 3 mulheres. Continua **um por partida**. Agora **ligados às origens** por pares (seção 4) |
| Ganhos da origem | Alternativa e atrito, sem vantagem | Igual, mais **fragmentos de origem** (seção 5) |
| Marca (romã) | Dedos e lábios manchados, sprite perde cor, sem sombra nas faixas altas | Mantém as 3 faixas; **muda a ordem do corpo**: dedos e mãos primeiro, **o rosto por último** (lábios saem da lista). Ganha tela de inventário que mostra a evolução (seção 6) |
| Rosto no fim | Não existia | **Regra C**: depende das Memórias Pesadas (seção 7) |
| Espíritos nomeados | Lista aberta (Remador, Tecelã, Padeiro prontos) | Mínimo de **2 por origem**; 5 novos a criar *(proposta)* (seção 8) |
| Sprite de mapa do protagonista | Corpo único, roupa por origem (5 roupas), camadas de cabelo e pele | **Um sprite por corpo** (M e F), sem roupa de origem, com tint/shader pela Marca |
| Perfil do mapa | Imagem de perfil andando | **Só o perfil parado**; o andar sai do rig (`marionete_capa.py`, modo "duas" com IK, ver `docs/PIPELINE_PERSONAGEM.md`) |

### 2. Dados e salvamento

Remover do save: `genero_opcao`, `pele`, `cabelo`, `olhos`, `estilo_cabelo`, marcas cosméticas.
Acrescentar:

```
corpo: "m" | "f"                      // escolhido no reflexo
origem: "pescador" | "sacerdote" | "veterano" | "artesao"
companheiro: "aulo"|"ismena"|"dimas"|"septimia"|"calias"|"lia"   // um por partida
marca: contador interno (nunca exibido como número); faixa derivada: baixa | media | alta
marca_etapa: 0..3                      // ver seção 6
rosto_final: null | "devolvido" | "coberto"   // calculado pela regra C (seção 7)
memorias_pesadas: lista das 6 com estado: encarou | deixou | pendente
fragmentos_origem: lista de ids já ganhos
```

O gênero para a **localização** (`ARQUITETURA.md`, variação de gênero em textos) vem de `corpo`. Falas dos companheiros que se dirigem ao protagonista usam essa variação.

### 3. Fluxo de início de jogo (substitui GDD seção 9, tela inicial)

1. Novo jogo → cutscene em primeira pessoa (ver `../2026-10-26_cutscene_abertura/`): andar desorientado, rio, ajoelha, reflexo **sem rosto**.
2. **Escolha do corpo:** dois reflexos (homem, mulher), jogador alterna para os lados e confirma. Imagens: `arte/protagonista_mapa/*_frente.png` (silhueta).
3. **Escolha da origem:** 4 cartas lado a lado com símbolo, ofício, ambição e 2 a 3 linhas de "quem foi" (sem revelar o ato). Símbolos: `arte/simbolos_origem/`.
4. **Dificuldade** (modos travados ao iniciar, como já definido).
5. Prólogo em Tênaro (a poça do GDD é substituída pelo rio da cutscene; ver pendências sobre a continuidade).

### 4. Origens x companheiros (dados, não código fixo) *(proposta)*

Com 4 origens existem 6 pares; cada companheiro liga-se a um par:

| Companheiro | Origens ligadas |
|---|---|
| Aulo | veterano, sacerdote |
| Ismena | pescador, sacerdote |
| Cálias | pescador, veterano |
| Lia | pescador, artesao |
| Dimas | veterano, artesao |
| Septímia | sacerdote, artesao |

Cada origem fica ligada a 3 companheiros e cada companheiro a 2 origens. O vínculo dá **história, nunca poder**. Guardar em um arquivo de dados (JSON/Resource), não em condicionais no código.

### 5. Fragmentos de origem (mecânica nova)

Quando o jogador **encontra, conversa, liberta ou viaja com** um espírito nomeado ou companheiro ligado à sua origem, ganha **um fragmento extra de memória sobre si mesmo** (ofício, ambição, um lugar, um rosto). É conteúdo narrativo no Diário/Mosaico, **não dá poder**. Integra-se à cadeia de 4 capítulos por origem (o capítulo "Eco" continua). Quantidade e eventos exatos: pendência.

### 6. Inventário e Marca (tela nova)

Tela com a silhueta do corpo escolhido, trocando a imagem conforme a etapa da Marca:

| Etapa | Quando | Arquivo (M / F) |
|---|---|---|
| 0 | início | `protagonista_{m,f}_frente.png` |
| 1 | primeiro Eco absorvido (romã rachada na mão, já no GDD 2.3) | `marca_{m,f}_etapa1_romaracha.png` |
| 2 | Marca média | `marca_{m,f}_etapa2_maos.png` |
| 3 | Marca alta (sem sombra e sem pegadas, já no GDD) | `marca_{m,f}_etapa3_tronco.png` |
| Rosto | final, regra C | `marca_{m,f}_rosto_devolvido.png` ou `marca_{m,f}_rosto_coberto.png` (as duas existem para os dois corpos) |

No **mapa e na batalha** o sprite do protagonista **não troca de imagem**; a Marca altera o sprite por **shader** (perda de cor por faixa) *(proposta)*. As imagens das etapas servem ao inventário e à cena final.

**Ajuste no `ARTE.md`:** "dedos e lábios manchados" → "dedos e mãos manchados"; o rosto (inclusive os lábios) é a última parte a mudar.

### 7. Regra C: o rosto no fim

- Se o jogador **encarou** as Memórias Pesadas (maioria): `rosto_final = "devolvido"` (os traços verdadeiros).
- Se **deixou** (maioria): `rosto_final = "coberto"` (romã cobre o rosto).
- Meio-termo: pendência (limiar e versão mista).

O resultado aparece no inventário (após a etapa 3) e na **cena final diante da água** (fecha a abertura). Deve ser coerente com os finais da HISTORIA seção 3 (por exemplo "Absorver + Marca alta: passa como sombra entre os vivos").

### 8. Conteúdo que cresce

- **Companheiros:** 5 → 6 (Lia precisa de capítulos, fixação, oferta final e vínculos; decidir se completa).
- **Espíritos nomeados:** mínimo de 2 por origem. Existentes: Remador (pescador), Tecelã e Padeiro (artesão). Novos *(proposta, nome e papel a confirmar)*: A Âncora (pescador), O Sacristão e A Oferente (sacerdote), O Desertor e O Trombeteiro (veterano).
- **Sombras comuns:** 6 corpos-base *(proposta)*: Soldado caído, Lavadeira, Pastor, Carpideira, Mercador, Camponesa.
- **Tabela de aprovação dos companheiros** (HISTORIA seção sobre vias): acrescentar a Lia.

### 9. Documentos a atualizar

`GDD.md` (2.3, seção 9, companheiros 5→6, origens 5→4), `HISTORIA.md` (seção 4, tabela 1.1 sem Estrangeiro, epílogo com o rosto, tabela de aprovação com a Lia), `ARTE.md` (seção 4 protagonista, seção 6), `ARQUITETURA.md` (variação de gênero, save), `PENDENCIAS.md`, `ELENCO_IMAGENS.md`.

### 10. Arte desta pasta (`arte/`)

| Pasta | Arquivos | Uso |
|---|---|---|
| `protagonista_mapa/` | M e F: frente, costas, perfil parado | Sprites de mapa 56 px pelo pipeline; frente também é a imagem da escolha de corpo |
| `protagonista_batalha/` | M e F: `protagonista_{m,f}_batalha_costas_34.png` | Batalha (o grupo aparece de costas); espelhar/pixelizar como os outros |
| `marca_inventario/` | M e F: 3 etapas + rosto devolvido + rosto coberto | Tela de inventário e cena final |
| `simbolos_origem/` | vara de pesca, cesto de oferendas, ombreira de bronze, pote de barro com espátula | Cartas da escolha de origem; selo pequeno no inventário e no Diário |
| `originais_antes_da_correcao_rosto/` | 4 imagens | Versões do Gemini antes de apagar os traços do rosto masculino |

**Correção feita pelo Claude:** a frente do corpo masculino e as etapas 1, 2 e 3 vieram com olhos, nariz e boca esboçados no rosto. Eles foram apagados por script (rosto liso, como o do corpo feminino e como no perfil e nas costas masculinos). Os originais estão na pasta de originais.

**Arte do protagonista completa** (8 de mapa/batalha, 10 da Marca, 4 símbolos). Ainda faltam, em outra frente: as sombras novas (5 espíritos nomeados e 6 corpos comuns, 4 imagens cada) e a cena da abertura.

**Observações técnicas da arte:**
- O fundo magenta do Gemini varia um pouco (por exemplo `(227,33,217)` nos símbolos). O removedor de fundo precisa tolerar isso (`tirar_fundo`).
- Nas etapas 3 e nos rostos há um **contorno claro** em volta do corpo; conferir se o removedor de fundo não o come ou deixa franja magenta.
- O ícone da ombreira do Veterano parece um elmo com tira. Se atrapalhar a leitura na tela de escolha, regerar com camadas de placas sobrepostas (ombreira de legionário).
- Na imagem feminina de perfil, o ombro tem uma mancha clara redonda (um brilho). Verificar no sprite de 56 px.

### 11. Pendências (não decidir sem o João)

1. Lia como sexta companheira: capítulos completos ou reduzidos?
2. Fragmentos de origem: quantos por origem e quais eventos disparam.
3. Nome e papel dos 5 espíritos nomeados novos; confirmar os 6 corpos comuns.
4. Regra C: limiar entre "encarou" e "deixou" e se há versão mista.
5. Rio da cutscene x poça de Tênaro: qual rio, antes ou dentro de Tênaro, caminhada jogável ou só cinemática.
6. Se a roupa de origem entra um dia no sprite do jogo (hoje: não).
7. Ligações origem x companheiro: os temas são ideias, as histórias ainda não existem.
