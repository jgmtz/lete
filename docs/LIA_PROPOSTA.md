> **SUBSTITUÍDO em 26/out/2026:** a Lia não é mais origem nem protagonista; é a **sexta companheira** (`docs/PROTAGONISTA_SOMBRA.md`). Capítulos, fixação, apoio e oferta final dela estão pendentes. A ficha abaixo fica como histórico e fonte de ideias.

# Lia, a Retratista — protagonista jogável (PROPOSTA de 25/out/2026)

> Rascunho para aprovação do João. Nada aqui altera HISTORIA/GDD/código até ele confirmar. Substitui a ficha de "sexta companheira" (`2026-10-25_lia_personagem`, descartada).

## Como ela entra no design (proposta)
**Sexta origem, "Retratista"**, jogável como as outras cinco (Pescador, Sacerdote, Veterano, Artesão, Estrangeiro). Mesma estrutura: ambição, traído, cadeia de 4 capítulos, 3 lembranças exclusivas, missão de origem. Tudo que já existe (5 vias, 1 companheiro por run, Tabuinha Rasurada, Venda do Nome, "o protagonista é o único afetado") continua valendo sem mudança. Os 5 companheiros continuam e **nenhum é a Lia**.

Diferença: as outras origens são "rostos escolhidos" (gênero e aparência pelo jogador, "como se lembra"). Lia tem **rosto e nome próprios**, e a escolha diegética vira o tema dela: ela pinta o próprio rosto no prólogo. (Decisão pendente: Lia é origem fixa, com gênero feminino e aparência da referência, ou o jogador pode escolher a aparência dela também.)

## Ficha de origem (mesmas colunas de `HISTORIA.md` 1.1)

| Origem | Ambição | Quem foi traído |
|---|---|---|
| Retratista | Ser lembrada como a melhor pintora de rostos | A menina pobre que posou e a quem ela negou o retrato |

**Ato original:** a família pobre da menina encomendou o retrato do pai morto e não pôde pagar. Um patrono rico ofereceu duas moedas e a promessa de apresentá-la aos ricos se ela raspasse aquele rosto e pintasse o filho dele no lugar. Lia raspou. O pai ficou sem retrato, ninguém o reconheceu no Além, e a menina virou uma sombra sem rosto (Sem-Nome). **Venda do Nome:** o nome que ela entregou foi o do pai, apagado da tábua; a Tabuinha Rasurada dela é a própria tábua raspada. Ela morre com uma tábua por terminar e sem lembrar do próprio rosto (espelho do protagonista que esqueceu um nome).

## Cadeia de 4 capítulos (rascunho, `HISTORIA.md` 4.1)

| Cap. | Ato | Rascunho |
|---|---|---|
| 1. Eco | I | Uma sombra sem rosto reconhece as mãos manchadas dela e pede para ser pintada |
| 2. Peso | II | O muro de rostos do Além (os que ela pintou) a acusa de ter embelezado: ninguém se reconhece |
| 3. Prova | III | Aflora a Memória Pesada: a oficina, a tábua raspada, as duas moedas |
| 4. Acerto | IV | Encontra a menina sem rosto: pintar o rosto do pai, entregar/destruir a tábua do patrono, ou deixar a menina sem imagem (libertar / combater / absorver) |

## Lembranças exclusivas (3, tipos diferentes)
- **O ateliê na estrada** (Ofício)
- **O dia de pintar a mãe** (Infância/Família)
- **A festa do patrono** (Festa)

## Missão de origem (exemplo)
Sombras sem rosto pedem para ser pintadas; o que ela pinta fica no muro e define se são reconhecidas depois.

## Mecânica própria (proposta, a validar com o João)
- **Retrato** (habilidade de origem, como os apoios dos companheiros): revela a fixação da sombra um passo antes. Não causa dano, não cura (princípio 6).
- O **Perfil final** pinta o rosto dela: o retrato no portão muda conforme as vias usadas. Fecha o arco "sem embelezar".
- Companheiros: o Retrato dela soma com o apoio do companheiro; a Ismena (nome) e a Lia (rosto) formam a dupla mais forte de leitura. Balancear depois.

## Aparência (referência do João, não copiada para o projeto)
Mulher adulta, rosto suave e arredondado, pele clara com poucas sardas, olhos azuis claros, sobrancelhas castanhas arqueadas, sorriso aberto, cabelo castanho-escuro longo e ondulado preso por taenia de linho; argola de ouro pequena. Túnica de linho até o joelho açafrão desbotado (#C99A3B dessat.), mangas arregaçadas, manchas de pigmento; avental couro e linho azul-lápis (#4A6A8A) com alças cruzadas nas costas e laço; bolsa de couro no quadril DIREITO, correia do ombro ESQUERDO; sandálias. Sem rosa/magenta nas manchas.
- Batalha (assimétrica): tábua com retrato pela metade e pincel fino. Mapa (simétrico): sem itens, sem bolsa. Prompts em `PROMPTS_GEMINI_LIA.md` continuam válidos.
- Pipeline do mapa: pernas visíveis do joelho para baixo; `marionete.py` (rig do Dimas), a confirmar após ver as imagens.

## Decisões do João (para `PENDENCIAS.md`)
1. Lia como **sexta origem** (proposta) ou como **protagonista à parte** (história e prólogo próprios, sem escolha de origem).
2. Rosto/gênero **fixos** (aparência da referência) ou **escolhíveis** como nas outras origens.
3. Impacto de escrita: +1 cadeia de 4 capítulos, 3 lembranças, missão de origem, falas dos 5 companheiros reagindo a ela.
4. Cor açafrão perto do dourado do Dimas: conferir na tela (o azul-lápis separa).
5. Se a Lia for baseada em pessoa real, confirmar com ela antes de publicar (itch.io).
