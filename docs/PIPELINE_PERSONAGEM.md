# Pipeline de personagem (receita completa, feita com o Aulo em 23-24/out/2026)

## REGRA DE SIMETRIA (decisão do João, 24/out/2026): vale para TODOS os personagens
Todo personagem é desenhado **simétrico** e **sem itens nas mãos** (nada de capacete, espada, bolsa, lanterna, lira etc. nas imagens base). Armas e objetos viram camada de equipamento depois, no jogo. Consequências:
- O Gemini gera só **3 imagens**: **frente reta** (não 3/4, rosto reto, braços soltos iguais), **costas reta** (mesma simetria; conferir mãos vistas por trás e dobra atrás do joelho; sem tiras assimétricas) e **perfil direito** (90°, passo curto, duas pernas separadas, sem objetos). O **perfil esquerdo é o espelho do direito** (feito por código).
- Anexar a frente e as costas aprovadas ao pedir o perfil. Frente: sem anexo (ou só referência de rosto).
- Vista de frente/costas é reta (yaw 0/180 do esqueleto 3D: vistas `f0`/`c0` em `joints.json`); o passo aparece como pé que avança descendo e o outro subindo.
- Aulo simétrico fechado: imagens em `art/concept/gemini/companheiros/aulo_sim/` (`aulo_f.png`, `aulo_c.png`, `aulo_perfil_direita_765.png`, `sim_*`), sprites em `art/sprites/companheiros/aulo_sim/{andar_56px,parado}`; configs `sim_frente`, `sim_costas`, `sim_direita`, `sim_esquerda` em `marionete.py`. As imagens e sprites do Aulo antigo (assimétrico) foram descartados em 24/out; as configs antigas do rig também. Parado de lado: imagem Gemini `aulo_pp_parado_direita.png` (4ª imagem de cada personagem: perfil parado, braços rentes, pernas juntas), reduzida com `sprite_andar.py --celula-fixa` (1 coluna, 56 px); o esquerdo é o espelho. **Regra: 4 imagens por personagem (F, C, PD andando, PP parado).**
- Para o perfil, o nariz de 1 px não sobrevive à redução: alongar o nariz na imagem de perfil (`sim_pd_nariz.png`, ~14 px) antes de reduzir.
- Pivô do quadril: pôr o ponto do quadril **dentro da saia** (y≈672 em células de 1024), senão a coxa mostra a quina fora da barra ao girar. Usar `coxa_estreita` para cortar a base da coxa.
- Caixas das pernas devem cobrir as botas inteiras (senão sobram pedaços no chão).
- Parado: quadros do andar com os dois pés no chão (frente/costas: a pose de repouso `REPOUSO=1`; perfis: quadro 4).


Tudo que foi aprendido ao fechar o Aulo, em ordem de uso. Para cada personagem novo, seguir esta lista.

## 1. O que cada personagem precisa (dois visuais)
- **Retrato** (inventário, menu, diálogo): imagem do Gemini, ≥96 px, F (frente 3/4) e C (costas). `pixelizar.py --cores 28` (escuros: 56 cores; pixelizar F+C juntas para paleta comum).
- **Sprite do mapa** (andando): 56 px de altura (referência Suikoden II), 12 quadros, 8 fps, quatro direções: frente, costas, esquerda, direita.
- **Parado**: um quadro do andar em que os dois pés estão no chão (`art/sprites/companheiros/<nome>/parado/`). Respiração leve é opcional, fica para depois.

## 2. Imagens que vão ao Gemini (4 por personagem de pernas visíveis)
1. **F** e **C** (prompts em `docs/ELENCO_IMAGENS.md`). C anexa F.
2. **Perfil esquerda** e **perfil direita**, estritos (90°), com as duas pernas separadas em passo curto, botas inteiras apontando para o lado do rosto, fundo magenta. Anexar F.
- O Gemini erra o lado e devolve 3/4 com frequência. Táticas que funcionaram:
  - Pedir "perfil estrito, só um olho visível, braço do outro lado escondido atrás do tronco, peito de lado".
  - Se virar para o lado errado: chat novo, anexar **só** a imagem do perfil que deu certo e pedir "novo desenho, personagem virado para o lado oposto (não espelhar)". Anexar também a F ou 3/4 faz o Gemini copiar o 3/4: **não anexar**.
  - Mais confiável: **pedir uma edição** da imagem que deu certo (ex.: "remover o capacete, braço pendurado ao lado do corpo, mesmo resto"). O Gemini edita bem; desenha mal do zero para o lado oposto.
  - Objetos carregados mudam de lado entre os perfis (capacete no braço esquerdo, espada no quadril direito). Decidir antes qual objeto some num dos lados (no Aulo, o capacete saiu do perfil direito).
- **Mesmo rosto nos dois perfis**: pegar a cabeça (linhas ~84-236, colunas ~415-610) de um perfil, espelhar e colar no outro (cabeças têm o mesmo tamanho). Foi o que se fez com o Aulo (`aulo_perfil_esquerda_rosto_novo.png`).

## 3. Andar por marionete 3D (`tools/render3d/`)
Em vez de pedir quadros de andar ao Gemini (ele repete a mesma perna), anima-se a imagem pintada com um esqueleto 3D.
- `index.html`: esqueleto three.js, ciclo de andar (joelho `0.10+0.78*pow`, braço `0.30*sin`).
- `joints.py`: gera `joints.json` (vistas `f` yaw 40, `c` yaw 140, `l` yaw -90, `d` yaw 90; 12 quadros).
- `marionete.py <config> joints.json saida.png`: recorta coxa, canela, pé, braço, cabeça e base da imagem pintada e move cada peça pelo esqueleto. Configs em `CONFIG` (frente, costas, esquerda, direita): articulações em repouso lidas da imagem, caixas das pernas, `inicio` (quadro do ciclo que bate com a pose da imagem), `absoluto`, `cabeca_rigida`, `pe_rigido`, `desloca_x`, `sem_perna` (região que as pernas não levam, ex.: ponta da bainha).
- **Mapear a perna certa**: no perfil, a perna da frente da imagem deve receber o movimento da perna que o 3D mostra adiantada em `inicio`. Conferir `joints.json` (x do tornozelo por quadro) e ordenar o desenho pelo z do tornozelo.
- **Pés rígidos** (`pe_rigido`): o pé gira junto com a canela, sem dobrar; só o joelho articula.
- **Cabeça rígida** (`cabeca_rigida`): cabeça e base se movem juntas em passos de 1 px do sprite final (`q = 1024/62`), senão o nariz de 1 px muda de forma a cada quadro.
- Braço parado nos perfis (decisão do João: sem balanço).

## 4. Reduzir para 56 px
`python tools/sprite_andar.py folha.png --colunas 4 --linhas 3 --altura 56 --cores 32 --celula-fixa --saida pasta --nome nome --fps 8 --zoom 6`
- `--celula-fixa` reduz a célula inteira na mesma grade em todos os quadros (sem isso o rosto muda de quadro para quadro). Conferir a silhueta do rosto nos 12 quadros.
- Se o nariz sumir num lado, `desloca_x` (px da célula, passos de 2) alinha o nariz com a grade; no Aulo, esquerda = -2.

## 5. Checklist de aprovação
Ver os 12 quadros ampliados (nunca só o GIF), medir posição do pé por quadro, enviar GIF ao João, ajustar. Salvar presets e atualizar `docs/ELENCO_IMAGENS.md`.

## 6. Personagens de vestido longo (Ismena, Septímia)
As pernas não aparecem: o rig de pernas não se aplica. Usar balanço da barra do vestido + subida e descida do corpo + ponta do calçado aparecendo alternadamente. Pedir ao Gemini os perfis com a barra do vestido e a ponta de um calçado visível.


## Personagem de vestido longo (Ismena, Septímia)
1. Imagens: F, C, PD (perfil com a ponta de um sapato). Um só perfil serve de andando e parado.
2. `python tools/andar_vestido.py img.png --vista frente|costas|lado [--espelhar --pe-tras --lift 3.2 --pe-desl 120] --saida folha.png`, depois `python tools/sprite_andar.py folha.png --colunas 4 --linhas 3 --altura 56 --cores 56 --celula-fixa --saida pasta --nome nome` (personagens escuros: 56 cores).
3. Gerar o perfil ESQUERDO (`--espelhar --pe-tras`) e espelhar os quadros para a direita. Parado = quadro 1 de cada direção.
