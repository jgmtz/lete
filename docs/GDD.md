# GDD — Projeto Lete (título provisório)

> Documento de design de jogo, versão 0.1 (23/out/2026).
> Fonte: decisões tomadas por João Guilherme Matzenbacher em conversa com Claude.
> **Números de balanceamento ficam para o fim** (ver `PENDENCIAS.md`). Onde este documento diz "TBD", o valor não foi decidido e não deve ser inventado como definitivo.
> Termos do jogo estão em português. O mapeamento para nomes de código em inglês está em `ARQUITETURA.md`, seção Glossário.

Documentos relacionados: `HISTORIA.md` (enredo, atos, origens, companheiros, chefes, regiões), `ARQUITETURA.md`, `DADOS.md`, `ARTE.md`, `BACKLOG.md`, `PENDENCIAS.md`.

---

## 1. Visão geral

**Gênero:** RPG de exploração e escolhas, com combate por turnos em que toda ação é um pequeno gesto de atenção.
**Tema:** mitologia greco-romana, fantasia sombria e sóbria. Sucessor do jogo Damnatio (HTML/JS), mais complexo e completo.
**Premissa:** o protagonista **desceu vivo** ao mundo dos mortos. Ele não lembra por quê. Explorar o Além é recuperar memórias; as escolhas dele decidem o que leva consigo, e se consegue sair.
**Duração alvo:** cerca de 10 horas no caminho principal, 15 horas ou mais para quem explora tudo.
**Plataforma:** executável para Windows, com suporte a teclado, mouse e controle desde o início. Publicação prevista no itch.io. Não é um jogo web.
**Visual:** câmera diagonal estilo Hades, em 3D low-poly com texturas em pixel art e sprites em pé (HD-2D / 2.5D), mais pixelizado que o Hades.
**Referências de função (não de cópia):** Planescape: Torment (protagonista com passado sombrio), Undertale (combate como escolha moral, gestos de defesa), Link's Awakening (mundo fechado que é o próprio tema), Hades (câmera e tema), Octopath Traveler (layout diagonal de combate), Sea of Stars e Paper Mario (ação com tempo certo em combate por turnos), Dark Souls (mundo que se lê pelos objetos).

### 1.1 Pilares

1. **Explorar é lembrar.** O mapa, a névoa e a progressão vêm de recuperar memórias, não de XP.
2. **Escolher é virar alguém.** O Perfil do jogador (como ele joga) altera mercadores, companheiro, NPCs, a luta final e o final.
3. **O que é irreversível tem peso.** Marca dos Mortos, Memórias Pesadas, perjúrio e escolha do companheiro não voltam atrás.
4. **Cada mecânica serve ao tema.** Teste para qualquer sistema: "o que isso me faz sentir que reforça a ideia central?" Se não houver resposta, é enfeite e deve sair.

### 1.2 Regras gerais de design

- **Nada de porcentagem de progresso.** O jogador nunca vê "37/60". O progresso aparece no mundo.
- **Nenhum caminho é obrigatório nem fecha o jogo.** Quem perdeu algo nunca fica travado; sempre pode voltar a explorar (sem relógio) ou escolher *Ficar*.
- **Sem relógio.** Nenhum prazo em tempo real. A passagem de tempo é a Maré, medida por progresso na história.
- **A origem dá alternativa, nunca vantagem.** Todos começam com os mesmos números.
- **Dificuldade sobe por regras mais complexas**, não por mais inimigos por luta.
- **Conteúdo é dado, não código** (ver `ARQUITETURA.md`).
- **Textos nascem traduzíveis** (chaves de localização, nunca texto fixo no código).

### 1.3 Loop central

Explorar uma região → encontrar sombras, objetos e oferendas → resolver cada encontro pela luta ou por outras vias → ganhar Lembranças, itens e relíquias → voltar a um hub ou Trívia → perceber que o mundo mudou (Maré, Perfil) → seguir.

---

## 2. Sistemas centrais

### 2.1 Mosaico (memória)

O Mosaico reúne as **Lembranças** do protagonista, episódios com nome ("O dia do juramento", "A voz no cais"). No menu é uma imagem de mosaico romano **sem moldura**, que cresce conforme ele lembra. O jogador nunca vê quantas pedras faltam.

**Tipos de Lembrança (cinco).** Cada tipo tem uma cor de pedra, uma oferenda, um padrão de gesto (Traço) e uma percepção na exploração.

| Tipo | Oferenda | Efeito das habilidades | Gesto Traço (rascunho) | Percepção na exploração | Stat que cresce (proposta, revisar) |
|---|---|---|---|---|---|
| Infância | Mel | Cura, proteção | Curva suave | Mostra lugares seguros (descanso, passagens fáceis) | Fôlego |
| Festa | Vinho | Ataque, coragem | Zigue-zague rápido | Atrai e acorda sombras próximas | Força |
| Família | Leite | Defesa, aliados | Dois traços que se encontram no meio | Mostra ligações entre sombras (quem pertence a quem) | Defesa |
| Luto | Água | Lentidão, drenar | Linha descendente lenta | Revela sombras e passagens escondidas | Lucidez |
| Ofício | Farinha | Técnica, precisão | Três toques em ritmo | Revela objetos, mecanismos e jeitos de usar o cenário | Precisão |

Cores de pedra: ocre para Infância, vermelho para Festa; as demais são TBD (a romã continua sendo o único vermelho forte do jogo, ver `ARTE.md`).

**Estágios de cada Lembrança** (a progressão vem de lembrar, sem XP):

| Estágio | O que o jogador tem | Efeito |
|---|---|---|
| Fragmento | Uma sensação solta | Versão fraca da Memória (habilidade); um trecho do mosaico acende |
| Recordação | Viu a cena | Habilidade média; desbloqueia falas e percepção |
| Memória plena | Entende o que aconteceu | Habilidade máxima; evolui itens; abre opções de diálogo |

**Como obter:** a fonte é sempre alguém ou algum lugar que conheceu o protagonista.
- Libertar sombras que o conheciam dá um Fragmento.
- Partilhar oferendas acorda sombras que lembram dele; o Diário guarda o que dizem.
- Lugares-âncora: objetos e cenários do passado, que só aparecem quando a névoa recua.
- Sonhos, na morada de Hipno e Tânato.
- A fonte de Mnemósine (região secreta).

**Confirmação cruzada:**
- Fragmento → Recordação: uma **segunda fonte, diferente da primeira**, que conte o mesmo episódio.
- Recordação → Memória plena: um ato no lugar-âncora, com o item ou a oferenda certa, ou a missão ligada a ela.

**Lembranças incertas e falsas.** Algumas fontes não são confiáveis (sombras com fixação, a paz de Elísios, sonhos). Uma Lembrança que vem de **uma só fonte** aparece com uma rachadura no mosaico (*incerta*): funciona como Fragmento, mas **não pode virar plena** até ser confirmada. Se a confirmação a contradiz, a pedra se parte e some (*falsa*). Apresentar uma incerta ou falsa como prova no Tribunal pode virar **perjúrio**.

**Quantidade por partida:** cerca de 30 Lembranças comuns (6 por tipo), 3 da origem escolhida, 6 Memórias Pesadas, mais algumas incertas ou falsas; total em torno de 40 a 45. Cada Lembrança tem uma Memória (habilidade) com 3 níveis, reaproveitando os 5 gestos-base.

**Chave de Memória.** Para entrar no Vale do Lete é preciso **Memória plena em pelo menos 3 dos 5 tipos** (qualquer combinação). O jogador não vê o número; o portão aparece "mudo" ou "desperto". Há mais Lembranças do que o mínimo. Se faltar, o jogador nunca fica preso: pode voltar a explorar ou beber do Lete (final *Ficar*). A Chave só abre a porta; o final ainda depende do Caminho, da Marca, das Lembranças e das Pesadas.

**O retrato do protagonista é um mosaico**: as pedras do rosto se preenchem conforme as Lembranças crescem (mesmo retrato, com efeito de shader).

### 2.2 Memórias Pesadas

Seis pedras escuras de borda partida no Mosaico, que guardam o que o protagonista fez. Seguem **fila fixa** (a seguinte só aflora depois da anterior). Afloram ao chegar a um lugar ou sombra ligados a elas, **sempre com aviso prévio** (Hécate ou o companheiro). A escolha é **definitiva**:

- **Encarar:** a Pesada vira Memória plena e dá uma habilidade **Culpa** (muito forte, custo alto de Lucidez). O protagonista muda: falas mais duras, sombras que o conheciam reagem.
- **Deixar (sob o Lete):** recebe **Leveza** (mais Lucidez máxima e resistência a ser "esquecido" ao zerar a Lucidez). Perde a habilidade e a verdade. Repete o gesto do "Gole" original, sem sermão do jogo, apenas pelas falas de Hécate e do companheiro.

Não existe medidor de "vontade de voltar": o jogo infere isso de quantas Pesadas foram encaradas ou deixadas, o que muda as falas de Hécate e de Anquises e dá peso ao final *Ficar*. Há também a opção de diálogo "prefiro não saber" e NPCs que notam quando o jogador foge do próprio passado.

As seis Pesadas e onde afloram estão em `HISTORIA.md`.

### 2.3 Marca dos Mortos (romã)

Representa o quanto o protagonista passou a "pertencer" ao Além. **Irreversível.**

- Internamente é um contador. **Nunca aparece como número.** O jogador vê uma romã que se abre: a romã aparece rachada na mão do protagonista no **primeiro Eco absorvido**, e cada absorção seguinte revela mais grãos.
- **O corpo mostra o efeito:** dedos e lábios manchados de vermelho, o sprite perde cor e, nas faixas altas, o protagonista deixa de projetar sombra e de deixar pegadas.
- **Três faixas:** baixa (nada acontece), média (NPCs vivos e algumas almas reagem diferente), alta (certas portas, travessias e a saída final deixam de reconhecê-lo).
- **Os mortos confiam mais em quem tem a Marca; os seres ligados aos vivos desconfiam.**
- **Cada faixa tem ganhos próprios**, não só perdas: Marca alta fecha o portão dos vivos, mas abre portas que só os mortos abrem (sombras de confiança, travessias que o reconhecem).
- **Regras de justiça:** Hécate avisa da romã **antes** do primeiro Eco e depois nunca repreende; ninguém precisa absorver para terminar o jogo.
- **Fontes de Marca:** Absorver sombras (Ecos), usar sangue como oferenda, consumir comida e bebida nativas do Além, perjúrio, pactos com Perséfone (custo irreversível), Absorver na luta final.
- **Regra "o que vem de cima é seguro, o que nasce no Além prende":** oferendas dos vivos e provisões trazidas não sobem a Marca; comida e bebida do Além curam barato mas sobem a Marca um pouco.

### 2.4 Caminho

Mede se o jogador sabe **como sair**: itens, nomes, promessas e aliados. Cada rio pede um tipo de chave, e cada chave pode ser obtida por mais de um caminho (ninguém trava por ter perdido uma só coisa):

| Rio / passagem | Chave | Formas de obter (exemplos) |
|---|---|---|
| Aqueronte | Óbolo | Moeda; favor de Hermes; ajudar insepultos; origem Pescador |
| Estige | Juramento (o primeiro é obrigatório) | Vários juramentos possíveis (ver 2.9) |
| Lete (entrada do Vale) | Nome ou Memória (Chave de Memória) | Memória plena em 3 dos 5 tipos |

Relíquias de heróis (ver 2.8) são chaves de pontos do Caminho. O Caminho decide **se existe saída**.

### 2.5 Perfil

Variável central que agrega **como o jogador jogou**: vias usadas em combate, tipo de Lembrança predominante, Marca, Pesadas encaradas ou deixadas e o comportamento **recente** (o jogador pode mudar de caminho). Quem lê o Perfil: mercadores, companheiro, reação de NPCs, a Sombra do Esquecido e o final composto. Com um único conjunto de variáveis, as mecânicas se explicam entre si.

### 2.6 Maré do Lete

O Lete é um rio e o esquecimento dele **sobe** conforme o jogador avança. Narrativamente, **a Maré é o juro da dívida do protagonista**; o mundo em si não está em perigo. É um valor global que sobe em degraus, **por progresso na história e nunca por relógio real**.

**Marcos de degrau:** (1) cruzar o Aqueronte, (2) escolher o companheiro, (3) cruzar o Estige, (4) passar pela bifurcação (Tártaro/Elísios), (5) entrar no Vale do Lete.

**Outras fontes de avanço:** viajar entre Trívias (avanço pequeno, ver 3.6) e a derrota em combate (avança por modo de dificuldade, ver 4.9). Ver `PENDENCIAS.md` item "Maré: granularidade".

**Efeito em regiões deixadas para trás:**
- Sombras **libertadas** continuam livres.
- As **não resolvidas** voltam mais teimosas (mais difíceis).
- Detalhes ainda não descobertos voltam a ficar sob a névoa (menos visíveis), **sem se perder**.
- Chegam sombras novas, com recompensa melhor.
- Há um **piso** de dificuldade na revisita, não um teto infinito. Quem explorou tudo cedo sente pouco a Maré (isso premia o foco em exploração).

**Apagamento:** quanto mais a Maré sobe, mais as sombras que o jogador ajudou o esquecem (ver `HISTORIA.md`, "Como mostrar que só ele sofreu").

---

## 3. Exploração

**Princípio:** é o foco do jogo. Explorar deve ser prazeroso por si só.

### 3.1 Estrutura do mundo

- Cada região é um conjunto de **áreas conectadas** (como rotas de Pokémon). **Asfódelos é o hub**: do ponto em que se atravessa o Aqueronte, três regiões ficam abertas ao mesmo tempo, em qualquer ordem.
- **Sem muros, com névoa.** Qualquer região pode ser visitada cedo, mas a maior parte do conteúdo só se revela com a lembrança certa. A progressão principal exige Memória, e a Memória só vem de explorar. Quem corre pela história chega ao fim sem material.
- **Os rios são o esqueleto do mapa.** Cada travessia pede uma chave (ver 2.4). As regiões se separam sem paredes artificiais.
- **Cada região tem três camadas:** caminho principal, ramos opcionais e um segredo. O tipo de espaço varia (aberto, labirinto, cidade, travessia) para não cansar em 10 horas.
- **O hub precisa mudar a cada retorno** (NPCs novos, falas novas, áreas que aparecem), senão cansa. Eventos por progresso, não por relógio (chegada de levas de mortos, dias de oferenda inspirados nas festas romanas dos mortos).
- **Dificuldade como sinal, não como barreira:** regiões profundas têm inimigos mais fortes; o jogador pode ir lá e ser derrotado, o que também ensina.

### 3.2 Sombras no mapa

Não há encontro aleatório. As sombras estão **visíveis no mapa**, vagando e repetindo o gesto que tinham em vida (o padeiro amassa, a tecelã puxa fio). O jogador lê o comportamento **antes** da luta e pode evitá-la. A luta começa quando a sombra o nota. Evitar poupa recursos, mas perde a recompensa.

### 3.3 Névoa e percepções

O mapa começa quase vazio e se preenche conforme o jogador lembra. Uma **névoa** esconde conteúdo (sombras, objetos, passagens) e recua conforme o Mosaico cresce. Os cinco tipos de Lembrança viram percepções (tabela em 2.1), o que faz do Mosaico também uma ferramenta de exploração (como os itens de Zelda, mas de memória).
- **Zonas de névoa densa** (opcionais) drenam Lucidez aos poucos.
- O **Utensílio** lamparina afasta a névoa.

### 3.4 Mundo para ler

Estilo Souls: bens funerários, tabuinhas, altares e oferendas deixadas. Examinar um objeto dá uma frase curta que vai para o Diário, sem texto longo.

### 3.5 Diário

Registra o que o jogador viu e ouviu (gestos, falas, objetos) em frases soltas. **Sem marcadores de objetivo, sem seta no mapa, sem lista de missões.** As sombras esquecem; o Diário não. O Diário é o único testemunho da jornada. Ele serve como fonte de provas no Tribunal. O que acontece com ele no final depende do desfecho.

### 3.6 Salvamento e viagem rápida

**Salvamento** só nas Trívias de Hécate e ao trocar de região. Nunca antes de cada luta (sem recarregar de graça).

**Viagem rápida só por estruturas no mundo** (para forçar a exploração):
- **Acender a Trívia** (chegar a pé e ativá-la) a liga à rede. A primeira vez em cada região é sempre a pé.
- **Cada Trívia liga a até três outras já acesas**, representadas por três tochas na própria estrutura. O jogador escolhe uma tocha (sem menu de mapa). Para ir longe, passa por várias Trívias.
- **A grande Trívia de Asfódelos é a exceção**: liga a todas as regiões já abertas.
- **Cada viagem faz a Maré avançar um pouco** (valor TBD).
- **Atalhos físicos** (portões, cordas, escadas abertos pelo lado de dentro) também são estruturas. Não custam Maré e recompensam quem explora bem.
- **Opcional, depois:** Barca de Caronte como segunda rede, ligando margens de rio.

### 3.7 Trívias de Hécate

Encruzilhadas de três caminhos, uma por região grande. Servem para **salvar, descansar (cura total de Fôlego e Lucidez) e receber dicas**. As dicas de Hécate são **qualitativas** e nunca decidem pelo jogador. Hécate é a guia fixa. Também são os pontos onde o jogador volta ao ser derrotado.

---

## 4. Combate

### 4.1 Visão geral

Combate **por turnos alternados** (o jogador age, o companheiro apoia quando é a vez dele, a sombra age). **Toda ação do jogador é um pequeno gesto de atenção.** Cada luta é uma pequena história que o jogador decifra; os caminhos de resolução são desfechos, não só botões. O jogo é de ritmo "lento e atento", e o gesto **nunca deve ser mais difícil que a decisão**.

**Risco real:** as lutas precisam ser lutadas e podem ser perdidas de verdade (ver 4.9).

### 4.2 Recursos

| Recurso | Quem | O que é |
|---|---|---|
| Fôlego | Protagonista | Vida. Volta com calor, comida e descanso, **nunca com violência** (ver 4.8) |
| Lucidez | Protagonista | Gasta nas habilidades de Memória e em outras ações. Zerada, a habilidade mais usada fica *esquecida* (indisponível) até a próxima Trívia |
| Firmeza | Sombra | Vida da sombra (barra sem valor numérico visível) |
| Compreensão | Sombra | O quanto o jogador sabe dela: **nome** e **fixação**. Aparece como 3 pontos que acendem (nome, fixação, ambos) |

Fôlego e Lucidez só voltam nas Trívias (ou por itens raros e fontes de 4.8). Cada luta cobra algo do caminho inteiro, não só dela mesma.

### 4.3 Como cada sombra é feita

- **Gesto:** o ciclo de ataques da sombra são ecos do que ela fazia em vida. Ler o padrão é ler a vida dela, sem texto.
- **Fixação:** o que a prende. Fica oculta; as pistas **nunca são exatas** ("ela chora quando ouve o mar" pode ser luto, medo ou saudade).
- **Nome:** o dado mais difícil de descobrir.
- **Vozes durante a luta:** conforme perde Firmeza, a sombra recupera clareza e fala mais. Lutar revela, mas destrói. Mesmo quem só ataca ouve parte da história.
- **Erro ensina:** tentar Libertar com a fixação errada enfurece a sombra, mas ela mesma corrige a pista na fala seguinte.
- **Sombras comuns** (preenchimento) usam um conjunto pequeno e reciclado de 4 a 5 gestos genéricos e bancos curtos de falas. **Sombras nomeadas** carregam a história e têm gesto e fixação únicos.
- **O lugar também luta:** cada região tem uma regra própria de combate que ensina o tema dela. Já definidas: Campos do Luto (lágrimas reduzem a velocidade), Tártaro (o fogo do Flegetonte fere todos), Elísios (qualquer dano cura devagar, o que faz o jogador hesitar em acabar a luta). As demais são TBD.
- **Dificuldade por complexidade de regra:** a sombra muda de gesto na metade da Firmeza, fica imune a uma via até certa condição, etc. **Não** por mais inimigos por luta (decisão explícita do João).

**Tipos de encontro:**

| Tipo | Como funciona |
|---|---|
| Sombra | Leitura e as vias de 4.5 |
| Personificação (Medo, Fome) | Sem nome: *suportar* até passar, ou combater. Ensina que há problemas que não se libertam |
| Guardião (Cérbero, Caronte) | Quebra-cabeça de regra: descobrir o que ele aceita |
| Julgamento | Sem luta: depor, argumentar, jurar (ver 2.10) |
| Cena | Sem combate; o jogador só acompanha um acontecimento |

### 4.4 Gestos-base (cinco)

Cada ação pede um gesto com a cara do que representa. São cinco gestos reaproveitados no jogo inteiro, com variações de velocidade e desenho:

| Gesto | Como é | Usado em |
|---|---|---|
| Anel | Um anel fecha sobre um círculo; aperta-se quando coincidem | Atacar; aparar golpes diretos |
| Traço | Desenhar uma sequência curta de 2 a 4 direções | Memória (padrão por tipo de Lembrança, ver 2.1) |
| Segurar | Segurar o botão e soltar no ponto certo | Escutar; Presenciar; Suportar |
| Arrastar | Conduzir um ponto devagar por um caminho, sem pressa | Oferenda; Libertar |
| Esquiva | Mover um marcador dentro de um quadro para fugir de padrões | Defesa contra ataques de padrão |

- **Esquiva e leitura são a mesma coisa:** os projéteis do quadro seguem o gesto da sombra (o padeiro lança golpes redondos em onda, a tecelã cruza fios).
- **Eco** usa o gesto da sombra de quem foi absorvida (quem "amassava pão" tem gesto de amassar).
- **O Instrumento equipado varia o gesto de ataque** (Anel duplo e lento, giratório, único e pesado) sem gesto novo.
- **Entrada:** direcionais ou analógico para gestos de padrão e trilha; **um único botão de confirmação** (espaço ou botão do controle) para os de tempo. Teclado/mouse e controle compartilham o mesmo sistema de gestos.

**Níveis de resultado (iguais para todos os gestos):** perfeito, bom ou falho, medidos pela **distância ao ponto ideal**.
- Perfeito dá bônus real (mais dano, mais chance de revelar a fixação).
- Bom é o resultado neutro esperado.
- Falho **ainda cumpre a ação**, mais fraca ou com pequena consequência temática (por exemplo, uma Memória que falha custa mais Lucidez, porque o jogador "forçou a lembrança"). O jogo nunca trava por erro puro.

**Dificuldade do gesto é separada da dificuldade da luta**; o modo de dificuldade só ajusta os limiares (janelas). **Assistência de gestos** (gesto automático com leve penalidade, janelas maiores) é opção de **acessibilidade ajustável a qualquer momento**, mesmo com o modo travado.

Ao escolher uma ação, **o menu some** e o gesto aparece por cima da cena (sombra e protagonista visíveis ao fundo). Na vez da sombra, o menu também some e entra o quadro da Esquiva no centro.

### 4.5 Vias de resolução

Todas começam igual; o que muda é a janela que o jogador persegue. **Misturar** é permitido: as vias não são mutuamente exclusivas dentro da mesma luta (ataca um pouco, descobre a fixação, muda para Libertar).

| Via | Janela / requisito | Duração-alvo (rascunho) | Custo | Ganho |
|---|---|---|---|---|
| Combater | A qualquer momento | 3 a 4 turnos | A sombra é destruída: some a informação dela; algumas facções e NPCs reagem mal; **fecha a missão** da sombra | Bens funerários (óbolos e itens). **Não cura** |
| Libertar | Compreensão completa (nome e fixação) | 5 a 6 turnos | Lucidez e turnos sob pressão | Fragmento de Mosaico, pista, às vezes aliado/hospitalidade futura; um pouco de Lucidez |
| Absorver | Firmeza baixa | 4 a 5 turnos | Sobe a Marca | Um **Eco** (o ataque da sombra, uso único por batalha; 3 slots) |
| Presenciar | Ficar até o fim do ciclo dela, sem lutar | Variável | Risco real (ela pode atacar) | A história inteira da sombra. Não dá poder |
| Poupar | Opção de menu "deixar ir" | Imediato | A sombra segue vagando, marcada no Diário como "ainda por resolver"; pode reaparecer mais forte ou em outro lugar | Poupa recursos |
| Fugir | Ação que tenta encerrar o combate, com chance de falhar | Imediato | Falha possível | Sai sabendo algo |
| Deixar-se vencer | **Ação escolhida** (não a derrota involuntária) | — | Custa Lucidez | Cena curta da sombra e uma lembrança *dela* |

Nenhum caminho é estritamente melhor que outro. Alguns inimigos são **imunes** a uma via (personificações do Vestíbulo não têm nome, então Libertar falha neles; outros resistem à força; outros são fortes demais para absorver). Cada inimigo **mostra quais vias estão abertas**.

### 4.6 Aproximar, oferenda e Eco em combate

- **Oferenda:** a oferenda certa revela nome e fixação antes da luta (facilita Libertar). Para não ficar forte demais, oferendas são escassas e a errada não funciona. Detalhes em 5.1.
- **Eco:** uso único por batalha; 3 slots; usa o gesto da sombra de origem; sobe a Marca.
- **Escutar:** gesto Segurar; revela pista de nome ou fixação.

### 4.7 HUD de combate

**Layout em diagonal / 3D** (pedido explícito): personagens embaixo à esquerda (protagonista e companheiro; **o protagonista fica sempre na posição mais à direita do grupo**), a sombra em destaque no canto superior direito, caixa de opções no canto inferior direito. Indicadores do protagonista no canto superior esquerdo; os da sombra no superior direito, em cima dela. O gesto da sombra aparece numa faixa no meio, como legenda de texto ("ela puxa o fio... e repete") que é a principal fonte de informação.

**Botões principais (menu em caixa):**
- **Atacar:** gesto Anel.
- **Memória:** abre os cinco tipos de Lembrança, cada um com seu Traço.
- **Aproximar:** agrupa Escutar, Oferenda, Presenciar e Poupar (para o jogador ver de cara que existem outros caminhos).
- **Eco:** usa um dos 3 slots.
- **Bolsa:** provisões (ver 4.8).
- **Fugir:** pequeno, fora do grupo.

**Sem números crus:** Firmeza é uma barra sem valor; Compreensão são três pontos. O companheiro aparece ao lado do protagonista com um selo do apoio ativo e um contador ("Apoio em 1 turno"). Quando a sombra fala, a caixa de diálogo aparece por cima do HUD.

### 4.8 Cura (nunca por derrotar sombras)

O Fôlego é o calor de quem ainda está vivo; volta com calor, comida e descanso.

| Fonte | Quanto cura | Limite |
|---|---|---|
| Trívias de Hécate | Tudo (Fôlego e Lucidez) | Só nelas |
| Fogos do Lar | Parcial | Pequenos altares domésticos (Lares romanos), acesos com uma oferenda; **uma vez por avanço da Maré** |
| Hospitalidade | Leve | Sombras **libertadas** com ofício (padeiro, tecelã) oferecem pão ou leito; uma vez por avanço da Maré |
| Provisões dos vivos | Boa | Raras; **não sobem a Marca** |
| Provisões do Além | Boa e barata | **Sobem a Marca** |
| Memória de Infância | Cura em combate | Gasta Lucidez |

A Lucidez volta nas Trívias e, em menor parte, ao libertar uma sombra. Não existe cura em loop. Nos modos mais difíceis as fontes ficam mais escassas.

### 4.9 Derrota

A derrota tem custo real, nunca é um "game over" seco:
- O jogador acorda na **última Trívia** (Hécate o recolhe).
- **Perde as oferendas e os óbolos** que carregava; ficam com a sombra e só voltam se ela for vencida depois.
- **A Maré sobe**: 0 degraus (Asfódelo), 1 degrau (Estige), 2 degraus (Tártaro).
- Recebe uma **pista nova** sobre a sombra (via Hécate); o Diário mantém o que viu.
- Salvamento só em Trívias e troca de região.

### 4.10 Companheiro em combate

- **Um por partida, definitivo.** Só **apoia** (buffs), **não causa dano** e não força o protagonista a agir diferente ("o mundo tem vários lados").
- Age **a cada 2 turnos**; quanto maior o vínculo, mais curto o intervalo.
- Se o jogador usa uma via que ele reprova, o apoio fica mais fraco naquele turno e ele comenta (versão *relutante*). Nada além disso. **Discordar e conversar aprofunda o vínculo.**
- Segunda leitura da luta: comenta a sombra à sua maneira (um diz "ele só quer descansar", outro "ele tem nome"). Ambos podem estar certos ou errados.
- **Apoios:** Postura (Aulo), Lamento (Ismena), Surrupio (Dimas), Brasa (Septímia), Canto (Cálias). Ver `HISTORIA.md`.
- O vínculo cresce ao partilhar oferendas com ele, lutar juntos e conversar nas Trívias.

### 4.11 Chefes

**Planta de todo chefe:** usa as mesmas regras do jogo e muda só as regras **dentro da luta**, em fases:
1. **Leitura:** gesto básico; o jogador aprende o padrão.
2. **Virada:** a regra muda; a fixação (e às vezes o nome) aparece.
3. **Desfecho:** abre-se a janela da via final.

Princípios: **nenhum chefe é só barra de vida** (todos têm ao menos uma saída sem dano total: um nome, uma oferenda, uma relíquia, uma prova). No modo Tártaro cada chefe ganha uma fase extra. Depois do chefe: a derrota rende pista; libertado, o chefe ajuda; combatido, deixa marca visível no mundo (um lugar que muda, um NPC que some). Recompensas por via: Libertar dá Fragmentos e aliados, Absorver dá Eco e Marca, Combater dá bens funerários.

Lista de chefes e exemplos detalhados (Cérbero, Fúrias, Sombra do Esquecido) em `HISTORIA.md`.

---

## 5. Oferendas, diálogo e missões

### 5.1 Oferendas

Vêm do mundo dos vivos (caem no Altar das Oferendas) e das provisões. Tipos: mel, vinho, leite, água, farinha e sangue (ver tabela em 2.1). O **sangue** acorda qualquer sombra, com crueza, mas sobe a Marca.

- **Dar** desperta a sombra. **Partilhar** é o protagonista provar também (hospitalidade): gasta a mesma oferenda, mas a sombra lembra com mais calor e o jogador ganha uma pista de Caminho ou um fragmento *dela*.
- A sombra **esquece depois**; o Diário guarda. Sombras já despertadas ficam um pouco mais nítidas e fazem um gesto ao ver o protagonista passar.
- **Dilema:** algumas sombras pedem para não lembrar; despertá-las mesmo assim tem custo.
- **Capacidade:** limite pequeno por tipo (rascunho: 5 de cada), para forçar escolhas.
- Gesto da oferenda: **Arrastar** (suave, sem pressão de tempo).

### 5.2 Diálogo

**Princípio:** conversar é explorar. Cada conversa revela algo do mundo ou do protagonista.

- **Três modos:** *Sombras* (a maioria dos mortos; estados de lucidez **apagada / vaga / desperta** que dependem de oferendas e do Mosaico), *Seres fixos* (Hécate, Hades, Perséfone, Caronte, Anquises e outros; falam normalmente, reagem ao Perfil) e *Cenas* (sem escolha, só contam algo, inclusive durante luta).
- **As opções são posturas**, não "bom ou mau": **Escutar, Perguntar, Pressionar, Consolar, Calar** (Calar sempre disponível). O jogador nunca sabe de antemão qual funciona com quem. É leitura, como no combate.
- **Opções que dependem do que o jogador é**, entre colchetes: `[Origem]` (ex.: `[Artesão] Mostrar as mãos`), `[Memória de Ofício]`, **Marca** (mortos confiam, vivos desconfiam), **Oferenda** (partilhar abre linhas que a sombra apagada não teria).
- **Opções bloqueadas ficam visíveis, em cinza, com dica vaga** ("você sente que já ouviu esse cheiro de forno..."). Isso mostra que existe mais e incentiva a revisita.
- **Companheiro na conversa:** em cenas importantes comenta **uma vez**; o jogador pode concordar, discordar ou ignorar, o que alimenta o vínculo.
- **Escopo:** falas reativas (origem, Marca, memória) só nas sombras nomeadas e nos seres fixos. Sombras de preenchimento usam bancos curtos.
- **Exemplo (Padeiro, Rua dos Ofícios):** estado *vago* (sem oferenda): Escutar → repete o gesto de amassar e murmura "a fornada atrasou..."; Perguntar → não entende; Calar → nada; `[cinza]` reconhece o cheiro do forno. Depois de partilhar **farinha**, o estado vira *desperto*: abrem-se "Quem era a mulher que esperava o pão?", `[Artesão] Mostrar as mãos` (ele o trata como colega) e `[Memória de Ofício]` (completa a frase dele). Quando o efeito passa, volta a *vago* e o Diário guarda o que contou.

### 5.3 Missões

Nenhuma é "busque e entregue". Cada uma é uma pequena história com **gancho, leitura e escolha**, e a consequência aparece depois. **Sem lista de objetivos e sem marcador.**

Três tipos: **de região** (ligadas à sombra nomeada local), **de oferenda ou vínculo** (a sombra só se abre depois de algo específico) e **de origem** (cadeia de 4 capítulos, ver `HISTORIA.md`). Mais as **missões dos companheiros** (5 capítulos cada).

- Exemplos: *Tecelã (Campos do Luto)*: esperava alguém voltar; se o jogador descobre o nome do esperado e o encontra em Elísios, pode levar a notícia, e ela pode se libertar ou piorar ao saber que ele a esqueceu. *Padeiro (Rua dos Ofícios)*: só abre se partilhar farinha; pede que leve pão à sombra da esposa, em outra região.
- **Consequências:** resolver ajusta o Mosaico, o Perfil ou a Marca; quase nunca dá XP. Destruir a sombra no combate **fecha** a missão (parte da escolha). Sem prazo; só a Maré pode endurecer a sombra, mas ela nunca desaparece.
- **Escopo:** 15 a 20 missões nomeadas, de três tamanhos (curtas de uma sombra, médias que cruzam regiões, longas do companheiro e da origem). Cada região tem 1 ou 2 curtas.

---

## 6. Itens, relíquias e economia

**Não há loot genérico nem venda de itens. Todo item é único, tem dono e tem uma frase de história.** Total: cerca de **65 itens únicos**.

### 6.1 Categorias (quantidades-rascunho)

| Categoria | O que é | Qtd. |
|---|---|---|
| Instrumentos | Variam o gesto de ataque (remo de afogado, fuso, martelo de oleiro, lira) | 15 |
| Vestes | Defesa: tamanho do quadro da Esquiva, janela do aparo, resistência à névoa | 12 |
| Amuletos | Regra passiva ligada a um tipo de Lembrança ou a uma via | 15 |
| Ex-votos | Pequenas oferendas deixadas em templos, efeito situacional; **2 espaços extras** | 12 |
| Utensílios | Ferramentas de exploração (lamparina afasta a névoa; fio de Ariadne marca o caminho em labirintos) | 6 a 8 |
| Relíquias | Chaves de Caminho; não equipáveis; ficam num alforje | 6 a 7 |

**Equipamento:** 3 espaços (instrumento, veste, amuleto) + 2 espaços de ex-voto.

### 6.2 Evolução

Cada item tem uma **Afinidade** (um dos cinco tipos de Lembrança) e três estágios: **Gasto, Reconhecido, Desperto**. Para evoluir é preciso **Memória plena do tipo certo** *e* uma pequena missão ligada ao dono do item (levar o objeto ao lugar onde viveu, ou encontrar quem o conheceu). Cada estágio acrescenta um efeito e uma linha de história.
Exemplo, **Remo do afogado** (Afinidade: Ofício): *Gasto* — ataque lento com anel duplo; *Reconhecido* — anel perdoa mais, revela quem era o dono; *Desperto* — "lembra" a água e ganha efeito próprio, depois de devolver a história dele à margem do Aqueronte.

**Conjuntos de dono:** peças que pertenceram à mesma sombra (remo, manto e moeda do afogado) formam um conjunto; reuni-las conta a história inteira, desbloqueia um efeito e uma pequena missão.

### 6.3 Relíquias de heróis

Cada uma conta como um herói desceu e saiu, ou falhou. Cerca de 6 a 7, **inventadas a partir dos mitos**: Eneias (ramo dourado seco), Orfeu (corda de lira), Héracles (algo da captura de Cérbero), Teseu e Pirítoo (fragmento da Cadeira do Esquecimento), Odisseu (a vala e o sangue da necromancia), Psiquê (registro do que levou: moedas e bolos de mel para Caronte e Cérbero). Cada relíquia é **chave** de um ponto do Caminho, dá uma **pista** da história daquele herói e traz uma **lição** (uma bênção com ônus, nunca só poder).

### 6.4 Economia e mercadores

- **Moeda:** o **óbolo**, escasso. Vem de sombras libertadas, objetos examinados, missões e bens funerários de sombras combatidas. Alguns mercadores aceitam **trabalho ou um nome** no lugar de óbolos (bom para Artesão e Estrangeiro).
- **Mercadores** são sombras comerciantes que negociam bens funerários. **O estoque segue o Perfil** (comportamento recente): no começo, itens fracos e neutros; conforme o jogador define o caminho, oferecem o que combina com ele, e itens e atributos mudam. Cada região tem o seu, com personalidade própria.
- **Travas contra beco sem saída:** sempre há um mercador neutro com o básico; o estoque segue o comportamento **recente** (dá para mudar de caminho); itens de outros caminhos existem, mais caros, como tentação.
- **Exemplos (rascunho):** O Coveiro (Asfódelos; básico, neutro, sempre disponível), A Viúva do Cocito (Campos do Luto; itens de luto e Compreensão: nomes, tabuinhas), O Intendente dos Guerreiros (Campo dos Guerreiros; instrumentos e vestes pesadas, itens de Combater).

---

## 7. Juramentos do Estige e Testemunhos do Tribunal

### 7.1 Juramentos

Cada juramento é um pacto com três partes: **bênção** (benefício real enquanto cumpre), **calcanhar** (ônus que a própria bênção traz, como Aquiles) e **condição** (o que promete ou deixa de fazer).

- Máximo **3 ao mesmo tempo**; cada um aparece como **um anel de água escura no braço**.
- **Perjúrio** (quebrar): a bênção some para sempre, a Marca sobe e os deuses reagem. **Irreversível.**
- **O primeiro juramento é obrigatório** para cruzar o Estige. Total de 8 a 10, vindos de deuses, guardiões e sombras.
- **Juramentos conflitantes** são o drama: a promessa a Hades e a promessa a Perséfone podem pedir coisas opostas. O jogo explica as condições com clareza, sem pegadinha escondida.
- **Exemplos (rascunho):**

| Juramento | Condição | Bênção | Calcanhar |
|---|---|---|---|
| Da Mão Aberta | Não absorver sombra alguma até o Lete | Lucidez máxima maior, resiste à névoa | Ecos ficam indisponíveis |
| Do Silêncio | Não revelar um nome confiado | Sombras confiam mais; opções de diálogo extras | Libertar fica mais difícil |
| Do Lar | Voltar a Asfódelos antes de descer ao Tártaro | Os Fogos do Lar curam mais | Não pode atravessar o Flegetonte antes de cumprir |

### 7.2 Testemunhos (Tribunal)

(O ato de ficar assistindo a uma sombra em combate chama-se **Presenciar**; "testemunho" é só do Tribunal.)

Minos, Radamanto e Éaco julgam sombras que **o jogador conheceu**. Cada julgamento é uma cena de diálogo com **provas**: o jogador escolhe quais entradas do Diário (falas, objetos, Lembranças) apresentar e pode **defender, acusar ou calar**.
- O veredito leva a sombra a um destino (Asfódelos, Elísios ou Tártaro); o jogador a reencontra lá, com outra atitude.
- Ele **depõe sob juramento**: apresentar Lembrança *incerta* ou *falsa* como prova pode virar **perjúrio** (liga os dois sistemas).
- De 4 a 6 julgamentos, todos de sombras nomeadas. Alimentam o Perfil (juiz severo ou clemente).

---

## 8. Dificuldade, acessibilidade e controles

### 8.1 Três modos, travados ao começar o jogo

História e finais iguais nos três.

| | **Asfódelo** | **Estige (padrão)** | **Tártaro** |
|---|---|---|---|
| Janela dos gestos | Larga | Normal | Estreita |
| Derrota | Perde parte das oferendas; Maré não sobe | Perde as oferendas; Maré sobe 1 degrau | Perde mais; Maré sobe 2 degraus |
| Sombras e chefes | Regras básicas | Regras básicas e mudanças de fase | Fases extras e mais imunidades a vias |
| Recursos | Mais oferendas, Trívias frequentes | Equilibrado | Escassos, Trívias mais espaçadas |
| Dicas de Hécate | Mais claras | Qualitativas | Quase nenhuma |

Implementação: **um arquivo de parâmetros por modo** (ver `DADOS.md`). Construir e balancear o Estige primeiro; o Tártaro só depois de o Estige estar sólido.

### 8.2 Acessibilidade

Assistência de gestos (separada do modo, ajustável a qualquer momento), legendas, tamanho de fonte, opções para daltonismo (TBD), remapeamento de controles.

### 8.3 Controles

Teclado e mouse **e** controle desde o M0. Um esquema único: direcionais/analógico + um botão de confirmação para gestos; menu navegável por direcionais. Remapeamento nas opções.

---

## 9. Criação do personagem

A escolha de aparência é **diegética**: o protagonista não lembra do próprio rosto. No prólogo, em Tênaro, uma poça de água escura mostra um reflexo borrado e o jogador **escolhe "como se lembra"**.

**Opções (versão 1):**

| Opção | Como é feita |
|---|---|
| **Gênero** (escolhido no início) | Tratamento do texto; ver `ARQUITETURA.md` (localização com variação de gênero). Se também altera a silhueta é **decisão pendente** (ver `PENDENCIAS.md`) |
| Tom de pele, cabelo e olhos | Troca de paleta por shader |
| Estilo de cabelo | 3 ou 4, como camada |
| Marca visível (cicatriz, tatuagem, nenhuma) | 3, camada pequena |
| Roupa | Definida pela **origem** |
| Corpo | Uma silhueta na primeira versão (a segunda pode entrar depois sem refazer nada) |

A aparência é só estética. Quem muda diálogos e atritos é a **origem**. A Marca altera a aparência (ver 2.3). Montar o criador **em camadas**, de forma que acrescentar uma opção seja adicionar dados.

Na tela de início o jogador escolhe: **origem, gênero, aparência, modo de dificuldade** (travado depois).

---

## 10. Interface e conteúdo a produzir

**Telas:** menu principal, tela de início (origem, gênero, aparência, modo), Diário, Mosaico, Bolsa/Alforje, equipamento, mapa (que se preenche), opções (controles, acessibilidade, idioma), tela de combate (4.7), tela de julgamento, tela de despedida no portão.

**Identidade visual da interface: barro e ouro (pedido do João, 25/out/2026).** Vale para todas as telas, atuais e futuras, por um tema central (`src/Lete.Game/UI/UiStyle.cs` + paleta `ui_paleta.tres`; arte gerada por `tools/ui_arte.py` em `art/ui/`, provisória):
- **Paleta:** barro escuro `#7A3E2B` (bordas, molduras), terracota clara `#B8664A` (destaques e **perigo**, nunca vermelho forte: a romã continua o único), ouro envelhecido `#C9A24B` (filetes, ícones, títulos), ouro escuro `#8C6B2A` (sombra do filete), azul-noite `#1E2430` translúcido (fundo dos painéis), marfim `#E9DFC7` (texto), cinza-barro `#6E5E58` (desativado), azul-lápis só para Lucidez e água.
- **Painéis:** moldura dupla (barro por fora, filete de ouro por dentro), quadrado de meandro em cada canto, fundo azul-noite translúcido; nada de caixa preta lisa.
- **Menu de ação:** tabuinha de barro com friso de meandro em cima e embaixo; cursor de folha de louro/ponta de lança dourada; desativado em cinza-barro; filete de ouro antes de Fugir. Ao abrir, os frisos se desenham da esquerda para a direita (0,3 s).
- **Barras:** trilho escuro, preenchimento colorido, tampas de meandro douradas e um ícone na mesma linha (sopro = Fôlego, gota = Lucidez, vulto = Firmeza). Fôlego baixo pulsa em terracota.
- **Retrato:** medalhão dourado. **Quadros de gesto:** filete de ouro com cantos de meandro; o ponto do jogador em marfim com brilho. **Anel de alvo no chão:** aro de ouro envelhecido com desgaste e quatro marcadores de meandro.
- **Texto:** fonte pixel (Pixelify Sans) com contorno escuro e sombra de 1 pixel da arte; títulos em ouro, corpo em marfim.
- **Transições:** fade curto na cor do fundo (azul-noite).
- **Proporções:** a base é 1920×1080; em telas mais estreitas (16:10, 4:3) a tela lógica cresce na vertical (`stretch/aspect = expand`) e o HUD se posiciona por frações da tela (dados em `content/battle/hud_layouts.json` e `battle_hud_estilo.tres`).
- Propostas do Claude que ainda dependem de aprovação: ver `PENDENCIAS.md` 24 a 26.

**Conteúdo total estimado (por partida e no jogo):** sombras ~40 a 60 (nomeadas 15 a 20, comuns reutilizando 6 corpos-base), Lembranças ~45, itens ~65, juramentos 8 a 10, julgamentos 4 a 6, missões 15 a 20, 6 chefes (+1 opcional), 5 origens, 5 companheiros, ~15 regiões. Ordem de grandeza de arte: 1.500 a 2.000 quadros (ver `ARTE.md`).

**Idioma:** português do Brasil primeiro; **textos nascem traduzíveis** (inglês opcional depois). **Áudio:** trilha por região, efeitos, ambiências (silêncio quando alguém "esquece" o protagonista), créditos.

**Tutorial:** o prólogo ensina gestos, vias e oferendas, uma coisa por vez, com o Remador (ver `HISTORIA.md`).

---

## 11. Escopo e plano de cortes

O jogo é grande para uma pessoa só. Ordem de construção: **fatias verticais** (ver `BACKLOG.md`). **Ordem de cortes, se faltar tempo** (a confirmar): (1) O Sem-Nome opcional, (2) a Barca de Caronte, (3) o modo Tártaro, (4) duas das cinco origens, (5) companheiros do meio (Dimas e Septímia), (6) segunda silhueta de corpo. Sugestão do plano: começar com **duas origens e um companheiro** e acrescentar o resto depois.

---

## 12. Publicação

Nome definitivo, capa, página do itch.io, trailer e demo ficam para perto do fim. **Conferir as regras de divulgação de arte gerada por IA** na plataforma onde publicar, já que conceitos são gerados com Gemini.
