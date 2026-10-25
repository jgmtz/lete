"""Gera a arte provisória da interface (barro e ouro) em art/ui/, com as cores de ui_paleta.tres.

Uso: python tools/ui_arte.py

Tudo é pixel art pequeno, desenhado aqui como grades de letras (cada letra é uma cor da paleta).
As cores vêm de src/Lete.Game/UI/ui_paleta.tres: mude a paleta lá e rode de novo.
Os desenhos são PROVISÓRIOS (feitos pelo Claude, 25/out/2026); o João pode trocar qualquer arquivo
por um definitivo de mesmo nome e tamanho sem mexer no código.
"""

import random
import re
from pathlib import Path

from PIL import Image

RAIZ = Path(__file__).resolve().parent.parent
PALETA = RAIZ / "src" / "Lete.Game" / "UI" / "ui_paleta.tres"
SAIDA = RAIZ / "art" / "ui"

# Escala dos painéis da exploração (o mundo é desenhado a 3x em 1080p).
ESCALA_EXPLORACAO = 3


def ler_paleta():
    cores = {}
    alfa_painel = 1.0
    for linha in PALETA.read_text(encoding="utf-8").splitlines():
        m = re.match(r"(\w+) = Color\(([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+)\)", linha)
        if m:
            cores[m.group(1)] = tuple(round(float(v) * 255) for v in m.groups()[1:4])
        m = re.match(r"PanelAlpha = ([\d.]+)", linha)
        if m:
            alfa_painel = float(m.group(1))
    return cores, alfa_painel


CORES, ALFA_PAINEL = ler_paleta()

# Letra -> cor. Minúsculas e maiúsculas distintas; "." é transparente.
LETRAS = {
    "O": CORES["Outline"],
    "G": CORES["Gold"],
    "D": CORES["GoldDark"],
    "C": CORES["Clay"],
    "T": CORES["Terracotta"],
    "I": CORES["Ivory"],
    "L": CORES["Lapis"],
    "S": CORES["ShadeFill"],
    "N": CORES["Night"],
    "B": CORES["ClayTablet"],
}


def grade(linhas, transparente_em=None):
    """Imagem a partir de linhas de letras; transparente_em = {letra: alfa 0..1} para fundos translúcidos."""
    altura, largura = len(linhas), len(linhas[0])
    img = Image.new("RGBA", (largura, altura), (0, 0, 0, 0))
    for y, linha in enumerate(linhas):
        assert len(linha) == largura, f"linha {y} com largura {len(linha)} (esperado {largura})"
        for x, letra in enumerate(linha):
            if letra == ".":
                continue
            alfa = (transparente_em or {}).get(letra, 1.0)
            img.putpixel((x, y), LETRAS[letra] + (round(alfa * 255),))
    return img


def salvar(img, nome, escala=1):
    if escala != 1:
        img = img.resize((img.width * escala, img.height * escala), Image.NEAREST)
    img.save(SAIDA / nome)
    print(f"  {nome} {img.size}")


# Quadrado de meandro (grega) dos cantos, 5x5, desenhado para o canto superior esquerdo.
CANTO = [
    "GGGGG",
    "G...G",
    "G.G.G",
    "G.GGG",
    "G....",
]


def espelhar(linhas, horizontal, vertical):
    out = [l[::-1] if horizontal else l for l in linhas]
    return out[::-1] if vertical else out


def moldura(fundo, alfa_fundo, tamanho=16):
    """Moldura dupla 9-patch: contorno escuro, barro por fora, filete de ouro por dentro, sombra do filete,
    fundo translúcido e um quadrado de meandro em cada canto. Margem do 9-patch: 5 px."""
    camadas = ["O", "C", "C", "G", "D"]
    linhas = []
    for y in range(tamanho):
        linha = ""
        for x in range(tamanho):
            borda = min(x, y, tamanho - 1 - x, tamanho - 1 - y)
            linha += camadas[borda] if borda < len(camadas) else fundo
        linhas.append(linha)
    linhas = [list(l) for l in linhas]
    for horizontal in (False, True):
        for vertical in (False, True):
            canto = espelhar(CANTO, horizontal, vertical)
            for cy, linha in enumerate(canto):
                for cx, letra in enumerate(linha):
                    x = tamanho - 5 + cx if horizontal else cx
                    y = tamanho - 5 + cy if vertical else cy
                    linhas[y][x] = letra if letra != "." else "C"
    return grade(["".join(l) for l in linhas], {fundo: alfa_fundo})


# O anel cobre 2 x TurnRingRadius do chão = 0,8 da altura de um personagem = 128 px da arte (160 px de altura):
# assim cada pixel da imagem vira um pixel da arte na tela.
ANEL_TAMANHO = 128
ANEL_RAIO = 47.5


def anel_alvo(tamanho=ANEL_TAMANHO, raio=ANEL_RAIO, espessura=2.0):
    """Aro de ouro envelhecido para o chão (visto de cima), com desgaste e um quadrado de meandro nos 4 pontos."""
    rnd = random.Random(7)  # desgaste sempre igual
    img = Image.new("RGBA", (tamanho, tamanho), (0, 0, 0, 0))
    centro = (tamanho - 1) / 2
    for y in range(tamanho):
        for x in range(tamanho):
            d = ((x - centro) ** 2 + (y - centro) ** 2) ** 0.5
            if abs(d - raio) <= espessura / 2:
                cor = CORES["GoldDark"] if rnd.random() < 0.22 else CORES["Gold"]
                img.putpixel((x, y), cor + (255,))
    marcador = grade(CANTO)
    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        mx = round(centro + dx * (raio + 4) - 2)
        my = round(centro + dy * (raio + 4) - 2)
        img.alpha_composite(marcador, (mx, my))
    return img


def main():
    SAIDA.mkdir(parents=True, exist_ok=True)
    print("Gerando arte da UI em", SAIDA)

    # Friso de meandro (tabuinha do menu e faixa da legenda), com filete de ouro escuro em cima e embaixo.
    meandro = grade([
        "DDDDDDDD",
        "CGGGGGGC",
        "CGCCCCGC",
        "CGCGGCGC",
        "CGCCGCGC",
        "GGCCGCGG",
        "DDDDDDDD",
    ])
    salvar(meandro, "meandro.png")
    salvar(grade(["GGGGGG", "G.....", "G.GGG.", "G...G."]), "meandro_fino.png")

    # Painéis: moldura dupla com fundo azul-noite translúcido; tabuinha de barro do menu.
    salvar(moldura("N", ALFA_PAINEL), "moldura.png")
    salvar(moldura("N", ALFA_PAINEL), "moldura_3x.png", ESCALA_EXPLORACAO)
    salvar(moldura("B", 0.94), "painel_acoes.png")
    salvar(grade(CANTO), "canto_meandro.png")

    # Medalhão do retrato: aro de ouro grosso com sombra, folhas de louro nos cantos.
    salvar(grade([
        "..OOOOOOOOOOOO..",
        ".OGGGGGGGGGGGGO.",
        "OGGDDDDDDDDDDGGO",
        "OGD.........DDGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGD.NNNNNNNN.DGO",
        "OGDD.........DGO",
        "OGGDDDDDDDDDDGGO",
        ".OGGGGGGGGGGGGO.",
        "..OOOOOOOOOOOO..",
    ], {"N": ALFA_PAINEL}), "moldura_retrato.png")

    # Cursor: folha de louro / ponta de lança dourada, apontando para a opção.
    salvar(grade([
        ".OOO...",
        "OGGGOO.",
        "OGDGGGO",
        "OGGGOO.",
        ".OOO...",
    ]), "cursor.png")

    # Tampas das barras (quadrado de meandro em ouro) e marca de alvo.
    salvar(grade([
        "GGGGGG",
        "G....G",
        "G.GG.G",
        "G.G..G",
        "G.GGGG",
        "G.....",
        "GGGGGG",
    ]), "barra_tampa.png")
    salvar(grade([
        "OOOOOOO",
        "OGGGGGO",
        ".OGGGO.",
        "..OGO..",
        "...O...",
    ]), "marca_alvo.png")

    # Óbolos da Compreensão: cheio em ouro, vazio em barro.
    salvar(grade([".OOOOO.", "OGGGGGO", "OGGOGGO", "OGOGOGO", "OGGOGGO", "OGGGGGO", ".OOOOO."]), "obolo_cheio.png")
    salvar(grade([".OOOOO.", "OCCCCCO", "OCNNNCO", "OCNNNCO", "OCNNNCO", "OCCCCCO", ".OOOOO."]), "obolo_vazio.png")

    # Ícones das barras: Fôlego (sopro, em ouro), Lucidez (gota de lápis), Firmeza da sombra (vulto).
    salvar(grade([
        ".......",
        "GGGGG..",
        ".....G.",
        "GGGG.G.",
        "....G..",
        "GGG....",
        ".......",
    ]), "icone_folego.png")
    salvar(grade([
        "...O...",
        "..OLO..",
        ".OLLLO.",
        "OLLILLO",
        "OLLLILO",
        ".OLLLO.",
        "..OOO..",
    ]), "icone_lucidez.png")
    salvar(grade([
        "..SSS..",
        ".SSSSS.",
        ".SOSOS.",
        ".SSSSS.",
        "SSSSSSS",
        "SSSSSSS",
        "S.S.S.S",
    ]), "icone_firmeza.png")

    # Turno: coroa de louro sobre quem age; tocha ao lado do nome da sombra na vez dela.
    salvar(grade([
        "G.........G",
        ".G.......G.",
        "G.G.....G.G",
        ".G.G...G.G.",
        "..G.GDG.G..",
        "....DDD....",
    ]), "louro_turno.png")
    salvar(grade([
        "..T..",
        ".TGT.",
        ".TGT.",
        "..G..",
        "ODDDO",
        ".DGD.",
        "..D..",
        "..D..",
        "..D..",
    ]), "tocha.png")

    # Anel de alvo no chão.
    salvar(anel_alvo(), "anel_alvo.png")


if __name__ == "__main__":
    main()
