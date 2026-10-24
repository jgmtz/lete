#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
refazer_pernas_opostas.py
O Gemini acerta as poses de contato e de passagem, mas nao troca a perna da frente
(copia a pose 1 no quadro 3 e a pose 2 no quadro 4). Este script cria as poses
OPOSTAS a partir de duas poses boas: recorta as duas pernas na resolucao original,
troca uma pela outra de quadril (sem espelhar, sem girar) e cola por baixo da saia.

Monta uma folha 2x2 (1=contato, 2=passagem, 3=contato oposto, 4=passagem oposta) em fundo
magenta, pronta para: folha_para_quadros.py --colunas 2 --linhas 2 e depois
montar_ciclo_pernas.py --originais 1,2,3,4.

Uso:
  python refazer_pernas_opostas.py folha.png --contato 110,0,410,360 --passagem 640,0,940,360 \
      --corte-y 246 --saida aulo_andar_f_4poses.png

  --contato / --passagem  caixa (x0,y0,x1,y1) de cada pose boa na folha do Gemini
  --corte-y               linha (em pixels da folha) logo abaixo da barra da saia
  --subida                linhas que a coxa sobe por baixo da saia para nao deixar fenda (padrao 14)
"""
import argparse
import importlib.util
import os
import sys

from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("pixelizar", os.path.join(AQUI, "pixelizar.py"))
px = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(px)

MAX_GIRO = 32
MAGENTA = (255, 0, 255, 255)


def componentes(alfa, y_min):
    """Grupos conectados (8 vizinhos) de pixels opacos nas linhas >= y_min."""
    a = alfa.load()
    w, h = alfa.size
    visto = set()
    grupos = []
    for y0 in range(y_min, h):
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
                            if 0 <= nx < w and y_min <= ny < h and a[nx, ny] > 0 and (nx, ny) not in visto:
                                visto.add((nx, ny))
                                fila.append((nx, ny))
                grupos.append(grupo)
    return grupos


def e_pele(c):
    r, g, b, al = c
    return al > 0 and r > 170 and g > 120 and (r - b) >= 45


def separar(pose, corte_y):
    """Devolve (tronco, perna_esq, perna_dir): imagens do mesmo tamanho da pose."""
    alfa = pose.getchannel("A")
    grupos = sorted(componentes(alfa, corte_y), key=len, reverse=True)
    if len(grupos) < 2 or len(grupos[1]) < 0.15 * len(grupos[0]):
        # pernas coladas: divide o grupo maior numa coluna de vale (menos pixels)
        g = grupos[0]
        ys = [y for _, y in g]
        base = max(ys)
        cont = {}
        for x, y in g:
            if y >= corte_y + 25:
                cont[x] = cont.get(x, 0) + 1
        xs = sorted(cont)
        lo, hi = xs[0] + (xs[-1] - xs[0]) * 0.3, xs[-1] - (xs[-1] - xs[0]) * 0.3
        cand = [x for x in xs if lo <= x <= hi]
        v = min(cand, key=lambda x: (cont[x], abs(x - (xs[0] + xs[-1]) / 2)))
        grupos = [[p for p in g if p[0] < v], [p for p in g if p[0] >= v]]
    pernas = grupos[:2]
    resto = grupos[2:]  # ex.: ponta da espada: fica com o tronco
    pernas.sort(key=lambda g: sum(x for x, _ in g) / len(g))
    src = pose.load()

    def so(grupo):
        out = Image.new("RGBA", pose.size, (0, 0, 0, 0))
        dst = out.load()
        for x, y in grupo:
            dst[x, y] = src[x, y]
        return out

    tronco = pose.copy()
    t = tronco.load()
    for g in pernas:
        for x, y in g:
            t[x, y] = (0, 0, 0, 0)
    return tronco, so(pernas[0]), so(pernas[1])


def quadril(perna, corte_y, linhas=6):
    a = perna.load()
    xs = []
    for y in range(corte_y, min(perna.height, corte_y + linhas)):
        for x in range(perna.width):
            if a[x, y][3] > 0:
                xs.append(x)
    return sum(xs) / len(xs) if xs else perna.width / 2


def subir_coxa(perna, corte_y, linhas):
    """Estica o topo da coxa para cima (so pele) para entrar por baixo da saia."""
    out = perna.copy()
    p = out.load()
    topo = None
    for y in range(corte_y, out.height):
        if any(e_pele(p[x, y]) for x in range(out.width)):
            topo = y
            break
    if topo is None:
        return out
    for k in range(1, linhas + 1):
        yy = topo - k
        if yy < 0:
            break
        for x in range(out.width):
            if e_pele(p[x, topo]):
                p[x, yy] = p[x, topo]
    return out


def deslocar(img, dx):
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    dx = int(round(dx))
    if dx >= 0:
        out.alpha_composite(img.crop((0, 0, img.width - dx, img.height)), (dx, 0))
    else:
        out.alpha_composite(img.crop((-dx, 0, img.width, img.height)), (0, 0))
    return out


def pe_mais_a_frente(perna):
    """x do ponto mais a direita da perna (o pe da frente avanca para a direita)."""
    bb = perna.getchannel("A").getbbox()
    return bb[2] if bb else 0


def girar(perna, graus, cx, cy):
    return perna.rotate(graus, resample=Image.BICUBIC, center=(cx, cy))


def pe_x(perna):
    """x medio do pe (ultimas 20 linhas opacas)."""
    a = perna.load()
    bb = perna.getchannel("A").getbbox()
    xs = [x for y in range(max(bb[1], bb[3] - 20), bb[3]) for x in range(perna.width) if a[x, y][3] > 0]
    return sum(xs) / len(xs)


def oposta(pose, corte_y, subida):
    tronco, esq, dir_ = separar(pose, corte_y)
    he, hd = quadril(esq, corte_y), quadril(dir_, corte_y)
    d = hd - he
    alvo = abs(pe_x(dir_) - pe_x(esq))  # passada original (pe da frente - pe de tras)
    nova_esq = deslocar(dir_, -d)  # a perna da direita passa a ser a da esquerda
    nova_dir = deslocar(esq, d)    # e vice-versa
    # alarga a passada girando cada perna em torno do quadril ate igualar a original
    melhor = (1e9, 0)
    for g in range(0, MAX_GIRO + 1):
        e2 = girar(nova_esq, g if pe_x(nova_esq) >= pe_x(nova_dir) else -g, he, corte_y)
        d2 = girar(nova_dir, -g if pe_x(nova_esq) >= pe_x(nova_dir) else g, hd, corte_y)
        erro = abs(abs(pe_x(e2) - pe_x(d2)) - alvo)
        if erro < melhor[0]:
            melhor = (erro, g)
    g = melhor[1]
    frente_e = pe_x(nova_esq) >= pe_x(nova_dir)
    nova_esq = girar(nova_esq, g if frente_e else -g, he, corte_y)
    nova_dir = girar(nova_dir, -g if frente_e else g, hd, corte_y)
    print("giro:", g, "graus; passada alvo", round(alvo), "obtida", round(abs(pe_x(nova_esq) - pe_x(nova_dir))))
    nova_esq, nova_dir = subir_coxa(nova_esq, corte_y, subida), subir_coxa(nova_dir, corte_y, subida)
    # quem avanca mais para a direita (a da frente) fica por cima
    atras, frente = (nova_esq, nova_dir) if pe_mais_a_frente(nova_dir) >= pe_mais_a_frente(nova_esq) else (nova_dir, nova_esq)
    out = Image.new("RGBA", pose.size, (0, 0, 0, 0))
    out.alpha_composite(atras)
    out.alpha_composite(frente)
    out.alpha_composite(tronco)
    return out


def caixa(texto):
    v = [int(n) for n in texto.split(",")]
    if len(v) != 4:
        raise ValueError("Caixa precisa de 4 numeros x0,y0,x1,y1")
    return tuple(v)


def main():
    p = argparse.ArgumentParser(description="Cria as poses opostas (perna da frente trocada) a partir de 2 poses boas.")
    p.add_argument("folha")
    p.add_argument("--contato", required=True, help="caixa x0,y0,x1,y1 da pose de contato")
    p.add_argument("--passagem", required=True, help="caixa x0,y0,x1,y1 da pose de passagem")
    p.add_argument("--corte-y", type=int, required=True, help="linha logo abaixo da barra da saia (na folha)")
    p.add_argument("--subida", type=int, default=14)
    p.add_argument("--tolerancia", type=int, default=90)
    p.add_argument("--tolerancia-interna", type=int, default=40)
    p.add_argument("--erosao", type=int, default=1)
    p.add_argument("--saida", default="folha_4poses.png")
    a = p.parse_args()
    try:
        img = Image.open(a.folha)
        rgba = px.remover_fundo(img, a.tolerancia, a.tolerancia_interna, a.erosao)
        c1 = rgba.crop(caixa(a.contato))
        c2 = rgba.crop(caixa(a.passagem))
        corte = a.corte_y - caixa(a.contato)[1]
        corte2 = a.corte_y - caixa(a.passagem)[1]
        c3 = oposta(c1, corte, a.subida)
        c4 = oposta(c2, corte2, a.subida)
        w = max(c1.width, c2.width)
        h = max(c1.height, c2.height)
        folha = Image.new("RGBA", (w * 2, h * 2), MAGENTA)
        for i, c in enumerate((c1, c2, c3, c4)):
            folha.alpha_composite(c, ((i % 2) * w, (i // 2) * h))
        folha.convert("RGB").save(a.saida)
        print("salvo:", a.saida, folha.size)
    except Exception as erro:
        print("Erro:", erro)
        sys.exit(1)


if __name__ == "__main__":
    main()
