# PENDENCIAS — o que falta decidir ou produzir

> Atualizar este arquivo sempre que uma decisão for tomada (mover para o GDD) ou surgir uma dúvida nova.
> **Regra:** nenhum número de balanceamento deve ser tratado como definitivo antes da fase de números (M5). Usar placeholders marcados `_tbd`.

---

## 1. Decisões em aberto (precisam do João)

| # | Tema | Situação | Proposta (se houver) |
|---|---|---|---|
| 1 | **Gênero e silhueta** | O João escolheu **escolher o gênero no início**. Falta decidir se o gênero muda só o **tratamento nos textos** ou também o **corpo do sprite** | Recomendado: só texto (um corpo, aparência por cabelo e marcas). Dois corpos dobram os trajes por origem |
| 2 | **Tamanho do sprite** | A decidir por teste visual (32 / 48 / 64 px) | Recomendação inicial: 48 px |
| 3 | **Maré: granularidade** | O GDD define 5 degraus por marco, **mais** avanço "pequeno" por viagem entre Trívias **e** 1 ou 2 degraus por derrota. Os dois usos de "degrau" precisam de uma unidade comum | Proposta: Maré como valor numérico (`TideValue`); marcos somam 1,0; derrota soma 0/1/2 por modo; viagem soma uma fração (TBD). Tabelas por região reagem ao **degrau inteiro** |
| 4 | **Paleta final** | A "cerâmica dos mortos" é paleta de partida; o João quer **ver** os personagens antes de aprovar | — |
| 5 | **Origens, companheiros e sombras visuais** | O João quer ver imagens reais antes de aprovar | Primeiro teste: Aulo e Ismena |
| 6 | **Nomes** | Aulo, Ismena, Dimas, Septímia, Cálias são **provisórios**; o **título do jogo** também | — |
| 7 | **Plano de cortes** | Ordem proposta em `GDD.md` seção 11; falta o João confirmar | Começar com 2 origens e 1 companheiro |
| 8 | **Idiomas** | Português do BR primeiro; inglês é opcional | Textos já nascem traduzíveis |
| 9 | **Stat por tipo de Lembrança** | A tabela de stats por tipo (Fôlego, Força, Defesa, Lucidez, Precisão) é proposta antiga; também foi decidido que **todos começam com os mesmos números**. Revisar na fase de números | — |
| 10 | **Barca de Caronte** como segunda rede de viagem | Opcional; fica para depois | — |
| 11 | **Fim do Diário e da Tabuinha** | O destino de cada um por desfecho ainda precisa ser escrito | — |

## 2. Números e balanceamento (por último, como o João pediu)

Dano e Firmeza das sombras; custo de Lucidez por Memória; janelas de perfeito/bom por gesto e por modo; limiares de assistência; economia (óbolos, preços); capacidade das oferendas (rascunho: 5 por tipo); intervalo de apoio do companheiro (rascunho: 2 turnos) e como o vínculo o reduz; curva da Maré por região; valores da Marca por faixa e quanto cada ação soma; efeito de cada Culpa/Leveza; cura de Fogos do Lar e hospitalidade; custo de viagem em Maré; bônus de perfeito; penalidade de gesto automático. Ver `DADOS.md` (campos `_tbd`).

## 3. Conteúdo ainda por listar e escrever

- **Sombras** (~40 a 60; 15 a 20 nomeadas; 6 corpos-base de comuns): lista por região com gesto, fixação, nome e vias.
- **Lembranças** (~45): lista completa (6 comuns por tipo + 3 por origem + 6 Pesadas + incertas/falsas) e a Memória (habilidade) de cada uma.
- **Itens** (~65), com dono, frase de história, afinidade e conjuntos de dono.
- **Juramentos** (8 a 10), **julgamentos** (4 a 6), **missões** (15 a 20), **mercadores por região**.
- **Regras de combate das regiões** ainda indefinidas (só Luto, Tártaro e Elísios estão definidas).
- **Mapas** (layout de cada uma das ~15 regiões).
- **Todas as falas e cenas** (o roteiro em si ainda não existe, só a estrutura).
- **Tutorial** (como o prólogo ensina gestos, vias e oferendas, uma coisa por vez).
- **Lista de relíquias** com a lição (bênção + ônus) de cada uma.
- **Eco** de cada sombra absorvível.
- **Interface:** layout das telas listadas em `GDD.md` seção 10.
- **Áudio:** trilha por região, efeitos, ambiências, créditos.
- **Arte:** conceitos de todos os personagens, sombras e chefes; modelos 3D modulares; texturas.
- **Acessibilidade:** daltonismo e escala de fonte (detalhes).
- **Publicação:** nome, capa, página, trailer, demo, política de arte com IA.

## 4. Pontos de atenção de design (já conhecidos)

- O **hub Asfódelos** precisa mudar a cada retorno.
- **Veterano e Aulo** usam vermelho: manter dessaturado para a romã continuar sendo o único vermelho forte.
- **Custo de escrita** de 5 origens × 4 capítulos + 5 companheiros × 5 capítulos é de todos, embora o jogador veja um de cada por partida.
- **Combinações de estado** (Marca × Maré × Mosaico × Pesadas × juramentos) geram bugs: painel de debug e testes desde o início.
- **Sprites consistentes**: o Gemini oscila entre quadros; manter processo e tamanho padrão.
- **Mnemósine × Lete:** as duas fontes se parecem; o puzzle depende de pistas interpretáveis.
- **Perjúrio por provas falsas** liga Tribunal e Mosaico: garantir que a regra fique clara para o jogador.

## 5. Registro de decisões (resumo, 23/out/2026)

- Tom sóbrio e fantasioso; combate por turnos; ~10h; executável Windows (não web); itch.io.
- Protagonista desceu vivo; Marca irreversível (romã); sem ponte direta com os vivos (só NPCs e relíquias de heróis).
- Asfódelos primeiro; hub; oferendas com partilha; origem importa do começo ao fim; Hécate como guia fixa.
- Origens sem vantagem; 5 origens; 5 companheiros independentes das origens, nenhum extremo.
- Combate: toda ação é um gesto (5 gestos-base, 3 níveis); alternado; companheiro só apoia; dificuldade por regras; derrota com custo real; HUD diagonal/3D; 3 modos travados ao iniciar.
- Mosaico com Chave de Memória (3 de 5 tipos); confirmação cruzada; incertas/falsas; Pesadas definitivas.
- Itens únicos com história (sem loot genérico); ~65 itens; cura sem derrotar sombras; Presenciar renomeado; Juramentos (máx. 3); Testemunhos só no Tribunal.
- 6 chefes; cadeia de 4 capítulos por origem; companheiros com 5 capítulos e oferta final.
- Godot 4 + C# + cenário 3D low-poly com sprites; Windows com controle.
- História: ambição, Venda do Nome, sem vilão externo, só ele sofre; Trocar como final.
- Arte: aparência como "como você se lembra"; gênero escolhido no início; teste de 32/48/64 px; Gemini para conceitos.
