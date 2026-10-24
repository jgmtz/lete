#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
pixelizar.py
Transforma uma imagem de personagem gerada por IA (em fundo liso) em sprites
pixel art de varios tamanhos e gera folhas para comparar os tamanhos.

Requisitos: Python 3.8+ e Pillow   ->   pip install pillow

Exemplos:
  python pixelizar.py aulo.png
  python pixelizar.py aulo.png ismena.png --alturas 32 48 64
  python pixelizar.py aulo.png --cores 12
  python pixelizar.py aulo.png --paleta "#D9CBB0,#B5573B,#8C1F2E,#25222B,#4E7F86,#C8A24A"

Saida (pasta "saida_sprites" por padrao), para cada imagem:
  <nome>_<altura>px.png   sprite no tamanho real, fundo transparente
  <nome>_comparacao.png   os tamanhos lado a lado, ampliados
  <nome>_na_tela.png      os tamanhos numa tela de 1920x1080, para sentir a escala
"""
import argparse
import os
import sys
from collections import Counter

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageStat

RES_BOX = Image.Resampling.BOX if hasattr(Image, "Resampling") else Image.BOX
RES_NEAREST = Image.Resampling.NEAREST if hasattr(Image, "Resampling") else Image.NEAREST
RES_LANCZOS = Image.Resampling.LANCZOS if hasattr(Image, "Resampling") else Image.LANCZOS
DITHER_NONE = Image.Dither.NONE if hasattr(Image, "Dither") else Image.NONE
QUANT_MEDIANCUT = Image.Quantize.MEDIANCUT if hasattr(Image, "Quantize") else Image.MEDIANCUT

COR_FUNDO_FOLHA = (37, 34, 43, 255)
COR_CHAO = (58, 52, 62, 255)
COR_TEXTO = (217, 203, 176, 255)


def hex_para_rgb(texto):
    t = texto.strip().lstrip("#")
    if len(t) != 6:
        raise ValueError("Cor invalida: %s (use o formato #RRGGBB)" % texto)
    return tuple(int(t[i:i + 2], 16) for i in (0, 2, 4))


def ler_paleta(texto):
    if not texto:
        return None
    cores = [hex_para_rgb(parte) for parte in texto.split(",") if parte.strip()]
    if not cores or len(cores) > 256:
        raise ValueError("A paleta precisa ter de 1 a 256 cores.")
    return cores


def tem_transparencia(img):
    if img.mode != "RGBA":
        return False
    minimo, _ = img.getchannel("A").getextrema()
    return minimo < 250


def limitar_tamanho(img, maximo=1400):
    """Imagens muito grandes deixam a remocao de fundo lenta; reduz antes."""
    maior = max(img.size)
    if maior <= maximo:
        return img
    fator = maximo / maior
    return img.resize((round(img.width * fator), round(img.height * fator)), RES_LANCZOS)


def remover_fundo(img, tolerancia, tolerancia_interna, erosao):
    """Remove o fundo liso (a cor dos cantos). Devolve a imagem em RGBA."""
    rgb = img.convert("RGB")
    largura, altura = rgb.size
    cantos = [(0, 0), (largura - 1, 0), (0, altura - 1), (largura - 1, altura - 1)]
    fundo = Counter(rgb.getpixel(c) for c in cantos).most_common(1)[0][0]

    marcador = (1, 254, 2)
    if fundo == marcador:
        marcador = (3, 252, 4)

    # 1) preenche a partir dos cantos: so remove fundo ligado a borda
    for canto in cantos:
        if rgb.getpixel(canto) != marcador:
            ImageDraw.floodfill(rgb, canto, marcador, thresh=tolerancia)

    # 2) remove fundo "preso" (por exemplo, entre o braco e o corpo)
    px = rgb.load()
    alfa = Image.new("L", (largura, altura), 255)
    ap = alfa.load()
    fr, fg, fb = fundo
    for y in range(altura):
        for x in range(largura):
            r, g, b = px[x, y]
            if (r, g, b) == marcador or abs(r - fr) + abs(g - fg) + abs(b - fb) <= tolerancia_interna:
                ap[x, y] = 0

    # 3) come um pixel da borda para tirar o halo da cor do fundo
    for _ in range(erosao):
        alfa = alfa.filter(ImageFilter.MinFilter(3))

    resultado = img.convert("RGBA")
    # 4) tira o "respingo" magenta das bordas (pixels misturados com a cor do fundo)
    if fundo[0] > 150 and fundo[2] > 150 and fundo[1] < 100:
        r, g, b = resultado.convert("RGB").split()
        rosa_r = ImageChops.subtract(r, g).point(lambda v: 255 if v > 60 else 0)
        rosa_b = ImageChops.subtract(b, g).point(lambda v: 255 if v > 60 else 0)
        rosa = ImageChops.multiply(rosa_r, rosa_b)
        alfa = ImageChops.subtract(alfa, rosa)
    resultado.putalpha(alfa)
    return resultado


def recortar(img):
    caixa = img.getchannel("A").getbbox()
    if caixa is None:
        raise ValueError("Nao sobrou nada depois de remover o fundo. Tente --tolerancia menor.")
    return img.crop(caixa)


def reduzir(img, altura, cores, paleta):
    """Reduz para a altura pedida, sem bordas suaves e com poucas cores."""
    w, h = img.size
    nova_largura = max(1, round(w * altura / h))
    pequeno = img.resize((nova_largura, altura), RES_BOX)
    alfa = pequeno.getchannel("A").point(lambda v: 255 if v >= 128 else 0)

    # Preenche a parte transparente com a cor media do sprite. Assim ela nao cria
    # uma cor falsa (preto) na paleta nem contamina as bordas.
    rgb_cheio = pequeno.convert("RGB")
    media = ImageStat.Stat(rgb_cheio, mask=alfa).mean
    base = Image.new("RGB", pequeno.size, tuple(int(round(m)) for m in media[:3]))
    base.paste(rgb_cheio, mask=alfa)

    if paleta:
        chato = []
        for cor in paleta:
            chato.extend(cor)
        chato.extend(list(paleta[-1]) * (256 - len(paleta)))
        referencia = Image.new("P", (1, 1))
        referencia.putpalette(chato)
        quantizada = base.quantize(palette=referencia, dither=DITHER_NONE)
    else:
        quantizada = base.quantize(colors=cores, method=QUANT_MEDIANCUT, dither=DITHER_NONE)
    rgb = quantizada.convert("RGB")

    saida = rgb.convert("RGBA")
    saida.putalpha(alfa)
    return saida


def folha_comparacao(sprites, zoom):
    margem = 24
    ampliados = []
    for altura, img in sprites:
        ampliados.append((img, img.resize((img.width * zoom, img.height * zoom), RES_NEAREST)))
    largura = margem + sum(g.width + margem for _, g in ampliados)
    alto = max(g.height for _, g in ampliados) + margem * 2 + 20
    folha = Image.new("RGBA", (largura, alto), COR_FUNDO_FOLHA)
    desenho = ImageDraw.Draw(folha)
    x = margem
    base = alto - margem
    for img, grande in ampliados:
        folha.alpha_composite(grande, (x, base - grande.height))
        desenho.text((x, 10), "%dx%d px" % (img.width, img.height), fill=COR_TEXTO)
        x += grande.width + margem
    return folha


def folha_tela(sprites, zoom):
    tela = Image.new("RGBA", (1920, 1080), COR_FUNDO_FOLHA)
    desenho = ImageDraw.Draw(tela)
    chao = 760
    desenho.rectangle([0, chao, 1920, 1080], fill=COR_CHAO)
    x = 240
    for altura, img in sprites:
        grande = img.resize((img.width * zoom, img.height * zoom), RES_NEAREST)
        tela.alpha_composite(grande, (x, chao - grande.height))
        desenho.text((x, chao + 12), "%d px (zoom %dx)" % (altura, zoom), fill=COR_TEXTO)
        x += grande.width + 200
    return tela


def processar(caminho, args, paleta):
    nome = os.path.splitext(os.path.basename(caminho))[0]
    print("Processando:", caminho)
    img = limitar_tamanho(Image.open(caminho))
    if tem_transparencia(img):
        img = img.convert("RGBA")
    else:
        img = remover_fundo(img, args.tolerancia, args.tolerancia_interna, args.erosao)
    img = recortar(img)
    os.makedirs(args.saida, exist_ok=True)

    sprites = []
    for altura in args.alturas:
        sprite = reduzir(img, altura, args.cores, paleta)
        destino = os.path.join(args.saida, "%s_%dpx.png" % (nome, altura))
        sprite.save(destino)
        sprites.append((altura, sprite))
        print("  salvo: %s (%dx%d)" % (destino, sprite.width, sprite.height))

    comparacao = os.path.join(args.saida, "%s_comparacao.png" % nome)
    na_tela = os.path.join(args.saida, "%s_na_tela.png" % nome)
    folha_comparacao(sprites, args.zoom).save(comparacao)
    folha_tela(sprites, args.zoom_tela).save(na_tela)
    print("  salvo:", comparacao)
    print("  salvo:", na_tela)


def main():
    p = argparse.ArgumentParser(description="Transforma imagens de IA em sprites pixel art.")
    p.add_argument("entradas", nargs="+", help="uma ou mais imagens (png, jpg, webp)")
    p.add_argument("--alturas", type=int, nargs="+", default=[32, 48, 64],
                   help="alturas dos sprites em pixels (padrao: 32 48 64)")
    p.add_argument("--cores", type=int, default=16, help="numero maximo de cores (padrao: 16)")
    p.add_argument("--paleta", default=None,
                   help='paleta fixa em hexadecimal, separada por virgulas: "#D9CBB0,#B5573B"')
    p.add_argument("--tolerancia", type=int, default=90,
                   help="tolerancia da cor do fundo ligado a borda (padrao: 90)")
    p.add_argument("--tolerancia-interna", type=int, default=40,
                   help="tolerancia para fundo preso dentro da silhueta (padrao: 40)")
    p.add_argument("--erosao", type=int, default=1,
                   help="pixels comidos na borda para tirar o halo do fundo (padrao: 1)")
    p.add_argument("--zoom", type=int, default=8, help="ampliacao da folha de comparacao (padrao: 8)")
    p.add_argument("--zoom-tela", type=int, default=4, help="ampliacao na tela 1080p (padrao: 4)")
    p.add_argument("--saida", default="saida_sprites", help="pasta de saida")
    args = p.parse_args()

    try:
        paleta = ler_paleta(args.paleta)
        for caminho in args.entradas:
            processar(caminho, args, paleta)
    except Exception as erro:  # mensagem curta para quem nao quer ler traceback
        print("Erro:", erro)
        sys.exit(1)


if __name__ == "__main__":
    main()
