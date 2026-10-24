#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
montar_ciclo_pernas.py
Conserta o ciclo de andar gerado por IA: monta um ciclo de 4 quadros em que as
pernas ALTERNAM de verdade, usando UM tronco fixo (parte de cima) e pernas tiradas
de dois quadros bons (um de contato e um de passagem), espelhando-as para o lado oposto.

Por que funciona: as pernas, vistas de frente ou de costas, sao quase simetricas.
O Gemini costuma desenhar so uma perna se mexendo. Aqui o Gemini fornece as POSES
e o script cuida da ALTERNANCIA e do balanco do corpo.

Entrada: a pasta de quadros gerada pelo folha_para_quadros.py (ja alinhados e com
paleta unica), ex.: saida/aulo_andar_costas_96px_01.png ... _06.png

Uso:
  python montar_ciclo_pernas.py pasta_quadros/ --prefixo aulo_andar_costas_96px \
      --tronco 4 --contato 1 --passagem 3 --corte 72 --saida ciclo_aulo_costas

Quadros sao numerados a partir de 1. Opcoes principais:
  --tronco N     quadro cujo tronco (parte de cima) sera usado em todos os quadros
  --contato N    quadro com o passo mais largo (pose de contato)
  --passagem N   quadro em que uma perna passa pela outra (uma levantada)
  --corte Y      linha (em pixels, de cima) onde separa tronco e pernas: a barra da saia
  --balanco P    pixels que o tronco desce nos contatos (padrao 1)
  --sobra N --sobra-x X   copia do tronco N linhas abaixo do corte, so na parte x >= X
                          (para a ponta da espada nao ser cortada); 0 desliga
  --pernas A,B,C,D  SEM espelho: usa as pernas destes quadros, nesta ordem (ex.: 1,2,4,3).
                 Use quando o personagem anda na diagonal (pes apontam para um lado): espelhar
                 faria os pes apontarem para o lado errado.
  --originais A,B,C  nao corta nada: reordena os quadros inteiros (recomendado quando as pernas trocadas ficam cortadas, ex.: andar de frente)
  --transicao N  quadro de transicao (opcional): faz um ciclo de 6 quadros, mais suave e lento
  --sobra-lado esq   a sobra fica no lado esquerdo (x <= sobra-x), ex.: espada de frente
  --bainha N     N linhas abaixo do corte: separa a bainha/punho da espada (cinza) das pernas
  --inverter     usa a passagem espelhada primeiro (se a ordem ficar trocada)
  --fps 6        velocidade do GIF
  --config ARQ   le todos os parametros de um arquivo JSON (presets em tools/ciclos/);
                 parametros da linha de comando sobrescrevem o arquivo
Saida: quadros PNG, folha horizontal (_folha.png), comparacao ampliada e GIF.
"""
import argparse
import glob
import os
import sys

from PIL import Image

NEAREST = Image.Resampling.NEAREST if hasattr(Image, "Resampling") else Image.NEAREST
FUNDO = (37, 34, 43, 255)


def centro_barra(img, corte):
    """Centro horizontal da barra da saia (as 3 linhas logo acima do corte)."""
    faixa = img.getchannel("A").crop((0, max(0, corte - 3), img.width, corte))
    bb = faixa.getbbox()
    if bb is None:
        return img.width / 2.0
    return (bb[0] + bb[2]) / 2.0


def _cinza_bainha(c):
    """Pixel cinza-acastanhado, pouco saturado (bainha/punho da espada), nao pele nem bota."""
    r, g, b, al = c
    if al == 0:
        return False
    lum = (r * 299 + g * 587 + b * 114) / 1000
    return 85 <= lum <= 130 and (r - g) < 32


def _vermelho_saia(c):
    """Resto vermelho/terracota da barra da saia que ficou abaixo do corte."""
    r, g, b, al = c
    if al == 0:
        return False
    lum = (r * 299 + g * 587 + b * 114) / 1000
    return (r - g) >= 38 and 50 <= lum <= 135 and r >= 85


def tirar_bainha(img, corte, linhas, xmax=999):
    """Remove a bainha das pernas e preenche com tom de pele (o buraco fica onde ela cobria a coxa)."""
    out = img.copy()
    px = out.load()
    pele = []
    for y in range(corte, min(img.height, corte + linhas)):
        for x in range(img.width):
            r, g, b, al = px[x, y]
            if al and r > 170 and g > 120 and (r - b) >= 45:
                pele.append((r, g, b))
    tom = max(set(pele), key=pele.count) if pele else (210, 170, 130)
    for y in range(corte, min(img.height, corte + linhas)):
        for x in range(img.width):
            if (x <= xmax and _cinza_bainha(px[x, y])) or _vermelho_saia(px[x, y]):
                px[x, y] = tom + (255,)
    return out


def so_bainha(img, corte, linhas, xmax=999):
    """So a bainha (cinza) das primeiras linhas abaixo do corte, para desenhar por cima, fixa no quadril."""
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    px, dst = img.load(), out.load()
    for y in range(corte, min(img.height, corte + linhas)):
        for x in range(img.width):
            if x <= xmax and _cinza_bainha(px[x, y]):
                dst[x, y] = px[x, y]
    return out


def _na_sobra(x, y, corte, sobra, sobra_x, lado):
    if not sobra or not (corte <= y < corte + sobra):
        return False
    return x >= sobra_x if lado != "esq" else x <= sobra_x


def so_pernas(img, corte, sobra, sobra_x, lado="dir"):
    """Mantem so as linhas do corte para baixo (e apaga a 'sobra', como a ponta da espada)."""
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            if y < corte or _na_sobra(x, y, corte, sobra, sobra_x, lado):
                px[x, y] = (0, 0, 0, 0)
    return out


def so_tronco(img, corte, sobra, sobra_x, lado="dir"):
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            dentro_sobra = _na_sobra(x, y, corte, sobra, sobra_x, lado)
            if y >= corte and not dentro_sobra:
                px[x, y] = (0, 0, 0, 0)
    return out


def limpar_ilhas(img, minimo=12):
    """Apaga grupos minusculos de pixels soltos (restos do corte)."""
    a = img.getchannel("A").load()
    w, h = img.size
    visto = set()
    px = img.load()
    for y0 in range(h):
        for x0 in range(w):
            if a[x0, y0] > 0 and (x0, y0) not in visto:
                grupo, fila = [], [(x0, y0)]
                visto.add((x0, y0))
                while fila:
                    x, y = fila.pop()
                    grupo.append((x, y))
                    for dx in (-1, 0, 1):
                        for dy in (-1, 0, 1):
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < w and 0 <= ny < h and a[nx, ny] > 0 and (nx, ny) not in visto:
                                visto.add((nx, ny))
                                fila.append((nx, ny))
                if len(grupo) < minimo:
                    for x, y in grupo:
                        px[x, y] = (0, 0, 0, 0)
    return img


def so_maior_grupo(img):
    """Mantem so o maior grupo conectado (a perna); apaga restos como a ponta da espada."""
    a = img.getchannel("A").load()
    w, h = img.size
    visto = set()
    grupos = []
    for y0 in range(h):
        for x0 in range(w):
            if a[x0, y0] > 0 and (x0, y0) not in visto:
                grupo, fila = [], [(x0, y0)]
                visto.add((x0, y0))
                while fila:
                    x, y = fila.pop()
                    grupo.append((x, y))
                    for dx in (-1, 0, 1):
                        for dy in (-1, 0, 1):
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < w and 0 <= ny < h and a[nx, ny] > 0 and (nx, ny) not in visto:
                                visto.add((nx, ny))
                                fila.append((nx, ny))
                grupos.append(grupo)
    if not grupos:
        return img
    maior = max(grupos, key=len)
    manter = set(maior)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    src, dst = img.load(), out.load()
    for x, y in manter:
        dst[x, y] = src[x, y]
    return out


def centro_coxa(img, corte, linhas=5):
    """Centro horizontal do topo da coxa (primeiras linhas com conteudo abaixo do corte)."""
    a = img.getchannel("A").load()
    xs = []
    for y in range(corte, min(img.height, corte + linhas)):
        for x in range(img.width):
            if a[x, y] > 0:
                xs.append(x)
    return sum(xs) / len(xs) if xs else img.width / 2.0


def esticar_topo(img, corte, linhas=4):
    """Repete a 1a linha da coxa para cima, para ela entrar por baixo da saia (sem fenda)."""
    out = img.copy()
    a = out.getchannel("A").load()
    px = out.load()
    topo = None
    for y in range(corte, img.height):
        if any(a[x, y] > 0 for x in range(img.width)):
            topo = y
            break
    if topo is None:
        return out
    for k in range(1, linhas + 1):
        yy = topo - k
        if yy < 0:
            break
        for x in range(img.width):
            if a[x, topo] > 0:
                px[x, yy] = px[x, topo]
    return out


def _e_pele(c):
    r, g, b, al = c
    return al > 0 and r > 170 and g > 120 and (r - b) >= 45


def topo_pele(img, corte, linhas=9):
    """Apaga o contorno escuro da barra da saia que sobrou acima da coxa e estica a pele para cima,
    para a coxa entrar por baixo da saia sem fenda."""
    out = img.copy()
    px = out.load()
    w, h = out.size
    topo = None
    for y in range(corte, h):
        if any(_e_pele(px[x, y]) for x in range(w)):
            topo = y
            break
    if topo is None:
        return out
    for y in range(corte, topo):
        for x in range(w):
            px[x, y] = (0, 0, 0, 0)
    for k in range(1, linhas + 1):
        yy = topo - k
        if yy < 0:
            break
        for x in range(w):
            if _e_pele(px[x, topo]):
                px[x, yy] = px[x, topo]
    return out


def deslocar(img, dx):
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    _colar_neg(out, img, int(round(dx)), 0)
    return out


def coluna_divisoria(img, corte, zona=0):
    """Coluna entre as duas pernas: a menos ocupada na parte de baixo, na faixa central."""
    alfa = img.getchannel("A")
    w, h = alfa.size
    px = alfa.load()
    melhor, ocup = w // 2, 10 ** 9
    for x in range(int(w * 0.3), int(w * 0.7)):
        ini, fim = (corte, min(h, corte + zona)) if zona else (corte + 4, h)
        o = sum(1 for y in range(ini, fim) if px[x, y] > 0)
        if o < ocup:
            melhor, ocup = x, o
    return melhor


def metade(img, x0, x1, corte, sobra, sobra_x, lado="dir"):
    base = so_pernas(img, corte, sobra, sobra_x, lado)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.alpha_composite(base.crop((x0, 0, x1, img.height)), (x0, 0))
    return out


def trocar_pernas(img, corte, d, sobra, sobra_x):
    """Perna esquerda vira a direita (e vice-versa) SEM espelhar: os pes continuam apontando
    para o mesmo lado. d = distancia horizontal entre as duas pernas (em pixels)."""
    xs = coluna_divisoria(img, corte)
    esq = metade(img, 0, xs, corte, sobra, sobra_x)
    dir_ = metade(img, xs, img.width, corte, sobra, sobra_x)
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    _colar_neg(out, dir_, -d, 0)   # perna direita passa a ser a esquerda
    _colar_neg(out, esq, d, 0)     # perna esquerda passa a ser a direita
    return out


def _juntar(a, b):
    out = a.copy()
    out.alpha_composite(b)
    return out


def colocar(destino, camada, dx, dy):
    destino.alpha_composite(camada, (dx, dy)) if dx >= 0 and dy >= 0 else _colar_neg(destino, camada, dx, dy)


def _colar_neg(destino, camada, dx, dy):
    recorte = camada.crop((max(0, -dx), max(0, -dy), camada.width, camada.height))
    destino.alpha_composite(recorte, (max(0, dx), max(0, dy)))


def montar_originais(frames, a):
    """Sem cortar nada: so reordena os quadros inteiros do Gemini (corpo e pernas originais)."""
    idx = [int(n) for n in a.originais.split(",")]
    for n in idx:
        if not 1 <= n <= len(frames):
            raise ValueError("Quadro %d nao existe (ha %d)." % (n, len(frames)))
    w = max(f.width for f in frames)
    h = max(f.height for f in frames)
    saida = []
    for n in idx:
        f = frames[n - 1]
        t = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        t.alpha_composite(f, ((w - f.width) // 2, h - f.height))
        saida.append(t)
    return saida


def montar(frames, a):
    if a.originais:
        return montar_originais(frames, a)
    h = frames[0].height
    tronco = frames[a.tronco - 1]
    cont = frames[a.contato - 1]
    pas = frames[a.passagem - 1]

    margem = 10
    largura = frames[0].width + margem * 2
    c_t = centro_barra(tronco, a.corte) + margem
    tr = so_tronco(tronco, a.corte, a.sobra, a.sobra_x, a.sobra_lado)

    def perna(src, espelhar):
        p = so_pernas(src, a.corte, a.sobra, a.sobra_x, a.sobra_lado)
        c = centro_barra(src, a.corte)
        if espelhar:
            p = p.transpose(Image.Transpose.FLIP_LEFT_RIGHT if hasattr(Image, "Transpose") else Image.FLIP_LEFT_RIGHT)
            c = src.width - c
        return p, c

    ordem = [(cont, False, a.balanco), (pas, True, 0), (cont, True, a.balanco), (pas, False, 0)]
    if a.inverter:
        ordem = [(cont, False, a.balanco), (pas, False, 0), (cont, True, a.balanco), (pas, True, 0)]
    if a.transicao:
        tra = frames[a.transicao - 1]
        e1 = not a.inverter
        # contato, transicao, passagem, contato oposto, transicao oposta, passagem oposta
        ordem = [(cont, False, a.balanco), (tra, False, a.balanco // 2), (pas, e1, 0),
                 (cont, True, a.balanco), (tra, True, a.balanco // 2), (pas, not e1, 0)]

    if a.troca:
        idx = [int(n) for n in a.troca.split(",")]
        for n in idx:
            if not 1 <= n <= len(frames):
                raise ValueError("Quadro %d nao existe (ha %d)." % (n, len(frames)))
        pares = []
        for n in idx:
            f = frames[n - 1]
            if a.bainha:
                f = tirar_bainha(f, a.corte, a.bainha, a.bainha_x)
            xs = coluna_divisoria(f, a.corte, a.zona_coxa)
            esq = so_maior_grupo(metade(f, 0, xs, a.corte, a.sobra, a.sobra_x, a.sobra_lado))
            dir_ = so_maior_grupo(metade(f, xs, f.width, a.corte, a.sobra, a.sobra_x, a.sobra_lado))
            pares.append((esq, dir_))
        # quadris: media do centro da coxa de cada perna nos quadros usados
        quadril_e = sum(centro_coxa(e, a.corte) for e, _ in pares) / len(pares)
        quadril_d = sum(centro_coxa(d, a.corte) for _, d in pares) / len(pares)
        def encaixar(meia, alvo):
            moved = deslocar(meia, alvo - centro_coxa(meia, a.corte))
            return topo_pele(moved, a.corte) if a.topo_pele else esticar_topo(moved, a.corte)
        pernas_prontas = []
        for e, d in pares:  # 1a metade: pernas originais, encaixadas no quadril
            pernas_prontas.append(limpar_ilhas(_juntar(encaixar(e, quadril_e), encaixar(d, quadril_d))))
        for e, d in pares:  # 2a metade: as pernas trocam de quadril (sem espelhar)
            pernas_prontas.append(limpar_ilhas(_juntar(encaixar(d, quadril_e), encaixar(e, quadril_d))))
        cont_pos = [int(n) - 1 for n in a.contatos.split(",")] if a.contatos else [0, len(idx)]
        bainha = so_bainha(tronco, a.corte, a.bainha, a.bainha_x) if a.bainha else None
        quadros = []
        for i, pr in enumerate(pernas_prontas):
            tela = Image.new("RGBA", (largura, h + a.balanco), (0, 0, 0, 0))
            colocar(tela, pr, margem, 0)
            colocar(tela, tr, margem, a.balanco if i in cont_pos else 0)
            if bainha is not None:
                colocar(tela, bainha, margem, a.balanco if i in cont_pos else 0)
            quadros.append(tela)
        caixa = None
        for q in quadros:
            bb = q.getchannel("A").getbbox()
            caixa = bb if caixa is None else (min(caixa[0], bb[0]), min(caixa[1], bb[1]), max(caixa[2], bb[2]), max(caixa[3], bb[3]))
        return [q.crop(caixa) for q in quadros]
    if a.pernas:
        idx = [int(n) for n in a.pernas.split(",")]
        for n in idx:
            if not 1 <= n <= len(frames):
                raise ValueError("Quadro %d nao existe (ha %d)." % (n, len(frames)))
        # contatos (posicoes pares: 1a, 3a, ...) descem o balanco; passagens ficam em cima
        cont_pos = [int(n) - 1 for n in a.contatos.split(",")] if a.contatos else list(range(0, len(idx), 2))
        ordem = [(frames[n - 1], False, a.balanco if i in cont_pos else 0) for i, n in enumerate(idx)]
    quadros = []
    for src, esp, desce in ordem:
        tela = Image.new("RGBA", (largura, h + a.balanco), (0, 0, 0, 0))
        p, c = perna(src, esp)
        colocar(tela, p, int(round(c_t - c)), 0)  # pernas: pes sempre na mesma linha
        colocar(tela, tr, margem, desce)  # tronco por cima, descendo nos contatos
        quadros.append(tela)
    # tira a margem vazia em volta, igual para todos (mantem alinhamento)
    caixa = None
    for q in quadros:
        bb = q.getchannel("A").getbbox()
        caixa = bb if caixa is None else (min(caixa[0], bb[0]), min(caixa[1], bb[1]), max(caixa[2], bb[2]), max(caixa[3], bb[3]))
    return [q.crop(caixa) for q in quadros]


def main():
    p = argparse.ArgumentParser(description="Monta ciclo de andar com pernas alternando.")
    p.add_argument("pasta", nargs="?", default=None, help="pasta com os quadros do folha_para_quadros.py")
    p.add_argument("--config", default=None, help="arquivo JSON de preset (ver tools/ciclos/)")
    p.add_argument("--prefixo", default=None, help="nome base dos quadros (ex.: aulo_andar_costas_96px)")
    p.add_argument("--tronco", type=int, default=None)
    p.add_argument("--contato", type=int, default=1)
    p.add_argument("--passagem", type=int, default=3)
    p.add_argument("--corte", type=int, default=None, help="linha da barra da saia/tunica (de cima para baixo)")
    p.add_argument("--balanco", type=int, default=1)
    p.add_argument("--sobra", type=int, default=0)
    p.add_argument("--sobra-x", type=int, default=0)
    p.add_argument("--topo-pele", action="store_true", help="limpa o contorno da saia sobre a coxa e estica so a pele para cima (modo de frente)")
    p.add_argument("--zona-coxa", type=int, default=0, help="separa as pernas olhando so estas linhas abaixo do corte (a zona das coxas); 0 = como antes")
    p.add_argument("--bainha-x", type=int, default=999, help="so considera bainha em x <= este valor (lado da espada)")
    p.add_argument("--bainha", type=int, default=0, help="linhas abaixo do corte onde a bainha da espada cruza a coxa: tira das pernas e redesenha fixa no quadril (0 desliga)")
    p.add_argument("--sobra-lado", default="dir", choices=["dir", "esq"], help="dir: sobra em x >= sobra-x; esq: sobra em x <= sobra-x (espada do lado esquerdo)")
    p.add_argument("--transicao", type=int, default=0)
    p.add_argument("--pernas", default="")
    p.add_argument("--originais", default="", help="ordem dos quadros INTEIROS do Gemini (ex.: 4,1,6,3,5,2), sem cortar nem trocar pernas")
    p.add_argument("--troca", default="", help="quadros da 1a metade do ciclo (ex.: 1,2,3); a 2a metade e a mesma com as pernas trocadas, sem espelho")
    p.add_argument("--d", type=int, default=20, help="distancia horizontal entre as pernas (usada com --troca)")
    p.add_argument("--contatos", default="", help="posicoes (1,2,...) do ciclo em que o tronco desce; padrao: 1a, 3a, 5a...")
    p.add_argument("--inverter", action="store_true")
    p.add_argument("--fps", type=int, default=6)
    p.add_argument("--zoom", type=int, default=4)
    p.add_argument("--saida", default="ciclo_pernas")
    a = p.parse_args()
    try:
        if a.config:
            import json
            with open(a.config, encoding="utf-8") as fh:
                cfg = json.load(fh)
            base = os.path.dirname(os.path.abspath(a.config))
            padrao = p.parse_args([])
            for chave, valor in cfg.items():
                if chave.startswith("_"):
                    continue
                nome = chave.replace("-", "_")
                if nome in ("pasta", "saida") and isinstance(valor, str):
                    valor = os.path.normpath(os.path.join(base, valor))
                if hasattr(a, nome) and getattr(a, nome) == getattr(padrao, nome):
                    setattr(a, nome, valor)
        obrig = ("pasta", "prefixo") if a.originais else ("pasta", "prefixo", "tronco", "corte")
        for obrigatorio in obrig:
            if getattr(a, obrigatorio) in (None, ""):
                raise ValueError("Falta o parametro %s (na linha de comando ou no --config)." % obrigatorio)
        arquivos = sorted(glob.glob(os.path.join(a.pasta, a.prefixo + "_[0-9][0-9].png")))
        if not arquivos:
            raise ValueError("Nao achei quadros %s_NN.png em %s" % (a.prefixo, a.pasta))
        frames = [Image.open(f).convert("RGBA") for f in arquivos]
        for n in ((a.tronco, a.contato, a.passagem) if not a.originais else ()):
            if not 1 <= n <= len(frames):
                raise ValueError("Quadro %d nao existe (ha %d)." % (n, len(frames)))
        quadros = montar(frames, a)
        os.makedirs(a.saida, exist_ok=True)
        for i, q in enumerate(quadros, 1):
            q.save(os.path.join(a.saida, "%s_ciclo_%02d.png" % (a.prefixo, i)))
        w, h = quadros[0].size
        tira = Image.new("RGBA", (w * len(quadros), h), (0, 0, 0, 0))
        for i, q in enumerate(quadros):
            tira.paste(q, (i * w, 0))
        tira.save(os.path.join(a.saida, "%s_ciclo_folha.png" % a.prefixo))
        amp = tira.resize((tira.width * a.zoom, tira.height * a.zoom), NEAREST)
        comp = Image.new("RGBA", (amp.width + 40, amp.height + 40), FUNDO)
        comp.alpha_composite(amp, (20, 20))
        comp.save(os.path.join(a.saida, "%s_ciclo_comparacao.png" % a.prefixo))
        gifs = []
        for q in quadros:
            g = q.resize((w * a.zoom, h * a.zoom), NEAREST)
            t = Image.new("RGBA", (g.width + 40, g.height + 40), FUNDO)
            t.alpha_composite(g, (20, 20))
            gifs.append(t.convert("RGB").convert("P", palette=Image.Palette.ADAPTIVE))
        gifs[0].save(os.path.join(a.saida, "%s_ciclo.gif" % a.prefixo), save_all=True,
                     append_images=gifs[1:], duration=int(1000 / a.fps), loop=0, disposal=2)
        print("%d quadros de %dx%d em %s" % (len(quadros), w, h, a.saida))
    except Exception as erro:
        print("Erro:", erro)
        sys.exit(1)


if __name__ == "__main__":
    main()
