#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
placeholders.py
Gera a arte PROVISORIA do M0 (nada aqui e arte final):
  - "conceitos" do protagonista (frente e costas) em fundo magenta, no mesmo
    formato que o Gemini entrega, para serem passados pelo pixelizar.py;
  - texturas 32x32 do cenario (piso, parede, terracota, agua) e a sombra de chao.

Requisitos: Python 3.8+ e Pillow.

Uso (a partir da raiz do repositorio):
  python tools/placeholders.py
  python tools/pixelizar.py art/concept/placeholder/protagonista_frente.png \
      art/concept/placeholder/protagonista_costas.png --saida <pasta_temporaria>
"""
import os
import random

from PIL import Image, ImageDraw

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PASTA_CONCEITO = os.path.join(RAIZ, "art", "concept", "placeholder")
PASTA_TILES = os.path.join(RAIZ, "art", "tiles")
PASTA_SPRITES = os.path.join(RAIZ, "art", "sprites", "placeholder")

# Paleta "ceramica dos mortos" (ARTE.md) e tons derivados dela.
OSSO = (217, 203, 176)
TERRACOTA = (181, 87, 59)
CARVAO = (37, 34, 43)
AGUA_LETE = (78, 127, 134)
OURO = (200, 162, 74)
PELE = (201, 166, 135)
AREIA = (156, 142, 116)
CINZA_CAPA = (104, 99, 96)
CABELO = (58, 47, 42)
MAGENTA = (255, 0, 255)


def misturar(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ---------------------------------------------------------------- conceitos

def desenhar_corpo(d, costas):
    """Figura de ~1000 px de altura, cabeca ~1/4 do corpo, contorno grosso."""
    c = CARVAO
    w = 14
    # pernas e sandalias
    d.rectangle([232, 800, 288, 1060], fill=PELE, outline=c, width=w)
    d.rectangle([312, 800, 368, 1060], fill=PELE, outline=c, width=w)
    d.rectangle([222, 1030, 296, 1080], fill=CABELO, outline=c, width=w)
    d.rectangle([304, 1030, 378, 1080], fill=CABELO, outline=c, width=w)
    # bracos
    d.rectangle([140, 350, 200, 700], fill=PELE, outline=c, width=w)
    d.rectangle([400, 350, 460, 700], fill=PELE, outline=c, width=w)
    # tunica e capa de viagem
    d.polygon([(190, 330), (410, 330), (440, 830), (160, 830)], fill=AREIA, outline=c, width=w)
    if costas:
        d.polygon([(180, 320), (420, 320), (450, 800), (150, 800)], fill=CINZA_CAPA, outline=c, width=w)
    else:
        d.polygon([(180, 320), (250, 320), (215, 800), (150, 800)], fill=CINZA_CAPA, outline=c, width=w)
        d.polygon([(350, 320), (420, 320), (450, 800), (385, 800)], fill=CINZA_CAPA, outline=c, width=w)
    # cinto com a Tabuinha Rasurada
    d.rectangle([170, 540, 430, 590], fill=TERRACOTA, outline=c, width=w)
    tx = 330 if not costas else 380
    d.rectangle([tx, 560, tx + 70, 650], fill=OSSO, outline=c, width=10)
    d.line([tx + 14, 600, tx + 56, 590], fill=c, width=8)
    # cabeca
    d.ellipse([185, 90, 415, 340], fill=PELE, outline=c, width=w)
    if costas:
        d.ellipse([180, 80, 420, 330], fill=CABELO, outline=c, width=w)
    else:
        d.chord([180, 70, 420, 280], start=180, end=360, fill=CABELO, outline=c, width=w)
        # rosto em 3/4 voltado para a direita
        d.rectangle([300, 200, 324, 232], fill=c)
        d.rectangle([360, 200, 384, 232], fill=c)
        d.line([318, 290, 372, 290], fill=c, width=10)


def gerar_conceitos():
    os.makedirs(PASTA_CONCEITO, exist_ok=True)
    for nome, costas in (("protagonista_frente", False), ("protagonista_costas", True)):
        img = Image.new("RGB", (600, 1160), MAGENTA)
        desenhar_corpo(ImageDraw.Draw(img), costas)
        destino = os.path.join(PASTA_CONCEITO, nome + ".png")
        img.save(destino)
        print("salvo:", destino)


# ---------------------------------------------------------------- texturas

def variar(cor, rnd, amplitude):
    delta = rnd.randint(-amplitude, amplitude)
    return tuple(max(0, min(255, v + delta)) for v in cor)


def piso_pedra(rnd):
    base = misturar(CARVAO, OSSO, 0.28)
    junta = CARVAO
    img = Image.new("RGB", (32, 32), junta)
    px = img.load()
    for linha in range(2):
        deslocamento = 8 if linha else 0
        for coluna in range(-1, 2):
            x0 = coluna * 16 + deslocamento
            cor = variar(base, rnd, 8)
            for y in range(linha * 16, linha * 16 + 15):
                for x in range(x0, x0 + 15):
                    if 0 <= x < 32:
                        px[x, y] = cor
            # luz no topo e na esquerda de cada laje
            for x in range(x0, x0 + 15):
                if 0 <= x < 32:
                    px[x, linha * 16] = misturar(cor, OSSO, 0.15)
    for _ in range(18):
        x, y = rnd.randrange(32), rnd.randrange(32)
        if px[x, y] != junta:
            px[x, y] = misturar(px[x, y], CARVAO, 0.35)
    return img


def parede_pedra(rnd):
    base = misturar(CARVAO, OSSO, 0.36)
    img = Image.new("RGB", (32, 32), CARVAO)
    px = img.load()
    for linha in range(4):
        deslocamento = 8 if linha % 2 else 0
        for coluna in range(-1, 3):
            x0 = coluna * 16 + deslocamento
            cor = variar(base, rnd, 10)
            for y in range(linha * 8, linha * 8 + 7):
                for x in range(x0, x0 + 15):
                    if 0 <= x < 32:
                        px[x, y] = cor if y > linha * 8 else misturar(cor, OSSO, 0.18)
    return img


def terracota(rnd):
    img = Image.new("RGB", (32, 32), TERRACOTA)
    px = img.load()
    for y in range(32):
        for x in range(32):
            if y in (6, 7, 24, 25):
                px[x, y] = CARVAO
            elif 11 <= y <= 19 and (x // 4 + y // 4) % 2 == 0:
                px[x, y] = misturar(TERRACOTA, CARVAO, 0.55)
            elif rnd.random() < 0.05:
                px[x, y] = misturar(TERRACOTA, OSSO, 0.2)
    return img


def agua(rnd):
    escura = misturar(CARVAO, AGUA_LETE, 0.45)
    img = Image.new("RGB", (32, 32), escura)
    px = img.load()
    for _ in range(7):
        x, y = rnd.randrange(32), rnd.randrange(32)
        for i in range(rnd.randint(3, 6)):
            px[(x + i) % 32, y] = AGUA_LETE
    return img


def sombra_chao():
    """Sombra de chao em elipse, com borda dura (pixel art). Some na Marca alta."""
    img = Image.new("RGBA", (32, 16), (0, 0, 0, 0))
    px = img.load()
    for y in range(16):
        for x in range(32):
            dx, dy = (x + 0.5 - 16) / 16, (y + 0.5 - 8) / 8
            r = dx * dx + dy * dy
            if r <= 0.45:
                px[x, y] = CARVAO + (150,)
            elif r <= 1.0:
                px[x, y] = CARVAO + (90,)
    return img


def gerar_texturas():
    os.makedirs(PASTA_TILES, exist_ok=True)
    os.makedirs(PASTA_SPRITES, exist_ok=True)
    rnd = random.Random(23)  # semente fixa: mesma saida a cada execucao
    for nome, funcao in (("piso_pedra", piso_pedra), ("parede_pedra", parede_pedra),
                         ("terracota", terracota), ("agua_lete", agua)):
        destino = os.path.join(PASTA_TILES, nome + ".png")
        funcao(rnd).save(destino)
        print("salvo:", destino)
    destino = os.path.join(PASTA_SPRITES, "sombra_chao.png")
    sombra_chao().save(destino)
    print("salvo:", destino)


if __name__ == "__main__":
    gerar_conceitos()
    gerar_texturas()
