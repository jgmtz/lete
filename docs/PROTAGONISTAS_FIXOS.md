> **SUBSTITUÍDO em 26/out/2026** por `docs/PROTAGONISTA_SOMBRA.md` (protagonista-sombra, 2 corpos, 4 origens, 6 companheiros). Mantido só como histórico.

# Protagonistas fixos: gênero definido pelo personagem (PROPOSTA de 25/out/2026)

> Decisão do João nesta conversa: **cada origem é um personagem fixo**, com o gênero definido pelo sprite. O jogador escolhe a origem vendo os sprites (retratos) e o gênero vem junto. Não há mais escolha de gênero nem criador de personagem na versão 1. Isto resolve a pendência nº 1 ("Gênero e silhueta"). Tudo que está marcado *(proposta)* ainda precisa de confirmação do João; o Claude Code **não** deve inventar o que não está aqui.

## 1. O que muda

| Antes (GDD seção 9) | Agora |
|---|---|
| O jogador escolhe origem, **gênero**, aparência (pele, cabelo, olhos, estilo, marca) e dificuldade | O jogador escolhe **um dos 6 personagens** (cada um = uma origem) e a dificuldade |
| Gênero mudava o tratamento do texto | O gênero é **do personagem**; o texto usa o gênero dele (a variação de gênero na localização continua, mas fixa por personagem) |
| Aparência no prólogo: "como se lembra" (escolhida na poça) | O jogador **já viu** o personagem na tela inicial. A poça do prólogo mostra o reflexo **borrado do personagem escolhido** *(proposta)*; o rosto "volta" aos poucos conforme a história avança |
| Corpo único + camadas de cabelo/pele | **Um sprite próprio por personagem** (mapa e batalha), desenhado à mão no pipeline do Gemini |
| Teste de gênero do Pescador (m/f) | **Cancelado** |

A **Marca** continua alterando a aparência (GDD 2.3), como sobreposição/paleta sobre o sprite de cada personagem. Se opções de cor (cabelo, pele) voltarem como cosméticos depois, é uma pendência futura, não da versão 1.

## 2. O elenco jogável (6)

João quer 3 homens e 3 mulheres. A Lia já é mulher. Proposta de gêneros *(a confirmar)*:

| # | Origem | Personagem | Gênero | Arte hoje |
|---|---|---|---|---|
| 1 | Pescador | *(nome pendente)* | Masculino | a fazer |
| 2 | Sacerdote | *(nome pendente)* | Feminino | a fazer |
| 3 | Veterano | *(nome pendente)* | Masculino | a fazer |
| 4 | Artesão | *(nome pendente)* | Masculino | a fazer |
| 5 | Estrangeiro | *(nome pendente)* | Feminino | a fazer |
| 6 | Retratista | **Lia** | Feminino | batalha + mapa prontos |

Os **nomes** dos protagonistas não existem no GDD/HISTORIA (os nomes dos traídos foram apagados de propósito; os dos protagonistas não foram definidos). Registrar como pendência; **não inventar**.

## 3. Tela inicial (proposta)
1. Fila de 6 retratos (arte de batalha de frente, recortada), com o **nome**, o **ofício** e a **ambição** de cada um embaixo.
2. Ao focar um personagem: sprite animado (flutuar/idle) e 2 a 3 linhas de "quem foi", **sem revelar o ato**.
3. Confirmar → escolher dificuldade (travada depois) → prólogo em Tênaro.
Nada de criador de personagem.

## 4. Impacto na arte (ELENCO_IMAGENS)
- 6 protagonistas × (batalha frente + costas, assimétricas) + (mapa simétrico F, C, PD; PE = espelho do PD), no pipeline atual.
- Lia: pronta. Os outros 5: 5 × 5 imagens do Gemini (2 de batalha + 3 de mapa) = **25 imagens**.
- Cancelar as 2 imagens do teste de gênero do Pescador. Remover o 4+4×2 do cálculo antigo (ver seção "Quantidade" do `ELENCO_IMAGENS.md`) e usar a conta acima.
- Cada protagonista com ofício diferente tem sua roupa de época e ferramentas de trabalho (o prompt de cada um usa a origem como guia).

## 5. Impacto no código e nos textos
- `ARQUITETURA.md` (localização com variação de gênero): o gênero vem do **personagem escolhido**, não de uma opção. A estrutura de texto com variação continua igual.
- Falas dos 5 companheiros que se dirigem ao protagonista devem ter variação de gênero por personagem (já previsto).
- Salvamento: guardar o `id` do personagem (não mais gênero + aparência).
- Lia: ver `2026-10-25_lia_protagonista` (já descreve a origem Retratista).

## 6. Pendências a abrir
1. Confirmar os gêneros da tabela da seção 2 (3 homens e 3 mulheres).
2. Definir os **nomes** dos 5 protagonistas restantes.
3. A poça do prólogo: confirmar a versão "reflexo borrado do personagem escolhido".
4. Opções cosméticas (cor de cabelo/pele) futuras: sim ou não.
5. O Retratista (Lia) e o Artesão são ofícios parecidos (obra, aprendiz/família que paga): confirmar que ficam distintos (Artesão = oficina, barro e obra paga com um nome; Retratista = rosto e retrato).
