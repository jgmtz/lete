#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
folha_para_quadros.py
Pega uma folha de animacao gerada por IA (varios quadros numa grade, fundo liso),
corta cada quadro, alinha pelos pes, aplica UMA paleta para todos e gera:
  - quadros individuais em pixel art (um PNG por quadro)
  - uma folha horizontal (strip) pronta para o Godot
  - um GIF animado para voce ver o movimento

Requisitos: Python 3.8+ e Pillow  ->  pip install pillow
Precisa estar na mesma pasta do pixelizar.py (usa as funcoes dele).

Exemplos:
  python folha_para_quadros.py aulo_andar.png --colunas 3 --linhas 2
  python folha_para_quadros.py aulo_andar.png --colunas 3 --linhas 2 --alturas 48 64 --fps 8
"""
import argparse
import importlib.util
import os
import sys

from PIL import Image, ImageStat

AQUI = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("pixelizar", os.path.join(AQUI, "pixelizar.py"))
px = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(px)


def cortar_grade(rgba, colunas, linhas):
    """Divide a folha em celulas iguais e devolve o recorte (so o personagem) de cada uma."""
    w, h = rgba.size
    cw, ch = w / colunas, h / linhas
    quadros = []
    for l in range(linhas):
        for c in range(colunas):
            caixa = (round(c * cw), round(l * ch), round((c + 1) * cw), round((l + 1) * ch))
            celula = rgba.crop(caixa)
            bb = celula.getchannel("A").getbbox()
            if bb is None:
                raise ValueError("A celula %d,%d ficou vazia. Confira --colunas e --linhas." % (c + 1, l + 1))
            quadros.append(celula.crop(bb))
    return quadros


def ancora_x(quadro):
    """Centro horizontal do tronco (metade de cima do corpo), que quase nao se mexe ao andar."""
    alfa = quadro.getchannel("A")
    w, h = alfa.size
    topo = alfa.crop((0, 0, w, max(1, h // 2)))
    total = 0
    soma = 0
    dados = topo.load()
    for y in range(topo.size[1]):
        for x in range(w):
            if dados[x, y] > 127:
                total += 1
                soma += x
    return int(soma / total) if total else w // 2


def alinhar(quadros):
    """Poe todos na mesma tela: pes na mesma linha, tronco no mesmo eixo."""
    ancoras = [ancora_x(q) for q in quadros]
    esq = max(ancoras)
    dir_ = max(q.width - a for q, a in zip(quadros, ancoras))
    alto = max(q.height for q in quadros)
    largura = esq + dir_
    saida = []
    for q, a in zip(quadros, ancoras):
        tela = Image.new("RGBA", (largura, alto), (0, 0, 0, 0))
        tela.alpha_composite(q, (esq - a, alto - q.height))
        saida.append(tela)
    return saida


def paleta_unica(quadros_rgb_alfa, cores):
    """Gera uma paleta a partir de TODOS os quadros, para as cores nao mudarem entre quadros."""
    pedacos = []
    for img, alfa in quadros_rgb_alfa:
        media = ImageStat.Stat(img, mask=alfa).mean
        base = Image.new("RGB", img.size, tuple(int(round(m)) for m in media[:3]))
        base.paste(img, mask=alfa)
        pedacos.append(base)
    larg = sum(p.width for p in pedacos)
    mosaico = Image.new("RGB", (larg, pedacos[0].height))
    x = 0
    for p in pedacos:
        mosaico.paste(p, (x, 0))
        x += p.width
    return mosaico.quantize(colors=cores, method=px.QUANT_MEDIANCUT, dither=px.DITHER_NONE)


def reduzir_todos(alinhados, altura, cores, paleta_fixa):
    alto = alinhados[0].height
    escala = altura / alto
    nova_larg = max(1, round(alinhados[0].width * escala))
    pequenos = [q.resize((nova_larg, altura), px.RES_BOX) for q in alinhados]
    alfas = [p.getchannel("A").point(lambda v: 255 if v >= 128 else 0) for p in pequenos]
    rgbs = [p.convert("RGB") for p in pequenos]
    if paleta_fixa:
        chato = []
        for cor in paleta_fixa:
            chato.extend(cor)
        chato.extend(list(paleta_fixa[-1]) * (256 - len(paleta_fixa)))
        ref = Image.new("P", (1, 1))
        ref.putpalette(chato)
    else:
        ref = paleta_unica(list(zip(rgbs, alfas)), cores)
    saida = []
    for rgb, alfa in zip(rgbs, alfas):
        media = ImageStat.Stat(rgb, mask=alfa).mean
        base = Image.new("RGB", rgb.size, tuple(int(round(m)) for m in media[:3]))
        base.paste(rgb, mask=alfa)
        q = base.quantize(palette=ref, dither=px.DITHER_NONE).convert("RGB").convert("RGBA")
        q.putalpha(alfa)
        saida.append(q)
    return saida


def salvar_gif(quadros, destino, fps, zoom):
    fundo = px.COR_FUNDO_FOLHA
    frames = []
    for q in quadros:
        grande = q.resize((q.width * zoom, q.height * zoom), px.RES_NEAREST)
        tela = Image.new("RGBA", (grande.width + 40 * zoom // 4, grande.height + 40 * zoom // 4), fundo)
        tela.alpha_composite(grande, ((tela.width - grande.width) // 2, (tela.height - grande.height) // 2))
        frames.append(tela.convert("RGB").convert("P", palette=Image.Palette.ADAPTIVE))
    frames[0].save(destino, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, disposal=2)


def main():
    p = argparse.ArgumentParser(description="Folha de animacao de IA -> quadros alinhados em pixel art.")
    p.add_argument("entrada", help="imagem da folha (png, jpg, webp)")
    p.add_argument("--colunas", type=int, required=True, help="quadros por linha na folha")
    p.add_argument("--linhas", type=int, default=1, help="linhas na folha (padrao: 1)")
    p.add_argument("--alturas", type=int, nargs="+", default=[48, 64], help="alturas em pixels (padrao: 48 64)")
    p.add_argument("--cores", type=int, default=16, help="maximo de cores (padrao: 16)")
    p.add_argument("--paleta", default=None, help='paleta fixa "#RRGGBB,#RRGGBB"')
    p.add_argument("--fps", type=int, default=8, help="quadros por segundo no GIF (padrao: 8)")
    p.add_argument("--zoom", type=int, default=6, help="ampliacao do GIF e da folha de comparacao (padrao: 6)")
    p.add_argument("--tolerancia", type=int, default=90)
    p.add_argument("--tolerancia-interna", type=int, default=40)
    p.add_argument("--erosao", type=int, default=1)
    p.add_argument("--saida", default="saida_quadros", help="pasta de saida")
    a = p.parse_args()

    try:
        nome = os.path.splitext(os.path.basename(a.entrada))[0]
        img = px.limitar_tamanho(Image.open(a.entrada), 2400)
        rgba = px.remover_fundo(img, a.tolerancia, a.tolerancia_interna, a.erosao)
        quadros = cortar_grade(rgba, a.colunas, a.linhas)
        alinhados = alinhar(quadros)
        paleta = px.ler_paleta(a.paleta)
        os.makedirs(a.saida, exist_ok=True)
        for altura in a.alturas:
            pix = reduzir_todos(alinhados, altura, a.cores, paleta)
            for i, q in enumerate(pix, 1):
                q.save(os.path.join(a.saida, "%s_%dpx_%02d.png" % (nome, altura, i)))
            tira = Image.new("RGBA", (pix[0].width * len(pix), pix[0].height), (0, 0, 0, 0))
            for i, q in enumerate(pix):
                tira.paste(q, (i * pix[0].width, 0))
            tira.save(os.path.join(a.saida, "%s_%dpx_folha.png" % (nome, altura)))
            salvar_gif(pix, os.path.join(a.saida, "%s_%dpx.gif" % (nome, altura)), a.fps, a.zoom)
            ampl = tira.resize((tira.width * a.zoom, tira.height * a.zoom), px.RES_NEAREST)
            comp = Image.new("RGBA", (ampl.width + 40, ampl.height + 40), px.COR_FUNDO_FOLHA)
            comp.alpha_composite(ampl, (20, 20))
            comp.save(os.path.join(a.saida, "%s_%dpx_comparacao.png" % (nome, altura)))
            print("%dpx: %d quadros de %dx%d -> %s" % (altura, len(pix), pix[0].width, pix[0].height, a.saida))
    except Exception as erro:
        print("Erro:", erro)
        sys.exit(1)


if __name__ == "__main__":
    main()
