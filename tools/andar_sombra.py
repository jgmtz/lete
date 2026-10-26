#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
andar_sombra.py: deslizar/flutuar de sombras (corpo opaco cuja parte de baixo se desfaz em fiapos, sem pernas).
Deforma a imagem pintada: o corpo sobe e desce devagar, a barra balança e os fiapos ondulam.
Gera a folha (4x3 = 12 quadros no mapa, 3x2 = 6 no idle de batalha) para tools/sprite_andar.py --celula-fixa.
Uso: python andar_sombra.py imagem.png --saida folha.png [--vista frente|costas|lado|batalha]
     [--y-saia 400 --y-fiapo 770 --y-fim 968 --quadros 12 --bob 18 --onda 20 --balanco 10 --inclina 0]
"""
import argparse, math
import numpy as np, cv2
from PIL import Image

def ss(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

def carregar(img, canvas):
    im = Image.open(img).convert("RGB")
    cw, ch = canvas
    c = Image.new("RGB", (cw, ch), (255, 0, 255)); c.paste(im, ((cw - im.width) // 2, 0)); return np.array(c)

def quadro(a, k, n, bob, onda, balanco, inclina, y_saia, y_fiapo, y_fim):
    H, W = a.shape[:2]
    ph = 2 * math.pi * k / n
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    w_s = ss(yy, y_saia, y_fiapo)                       # balanco cresce da cintura para os fiapos
    w_f = ss(yy, y_fiapo, y_fim)                        # so os fiapos ondulam
    dy = -bob * math.sin(ph)                            # sobe e desce (flutua)
    dx = balanco * w_s * math.sin(ph) + onda * w_f * np.sin(2 * ph - yy / 22.0)
    dx = dx + inclina * ss(yy, 0, y_fim) * 0 + inclina * (1 - ss(yy, 0, y_fim)) * math.sin(ph)  # cabeca acompanha um pouco
    r = cv2.remap(a, xx - dx, yy - dy, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=(255, 0, 255))
    return r

def main():
    p = argparse.ArgumentParser()
    p.add_argument("imagem"); p.add_argument("--saida", required=True)
    p.add_argument("--vista", default="frente", choices=["frente", "costas", "lado", "batalha"])
    p.add_argument("--quadros", type=int, default=None)
    p.add_argument("--y-saia", type=int, default=None); p.add_argument("--y-fiapo", type=int, default=None); p.add_argument("--y-fim", type=int, default=None)
    p.add_argument("--bob", type=float, default=None); p.add_argument("--onda", type=float, default=None)
    p.add_argument("--balanco", type=float, default=None); p.add_argument("--inclina", type=float, default=0.0)
    p.add_argument("--canvas", default=None, help="LxA da celula (padrao 1024x1024; batalha paisagem 1024x572)")
    a = p.parse_args()
    bat = a.vista == "batalha"
    n = a.quadros or (6 if bat else 12)
    cols, rows = (3, 2) if bat else (4, 3)
    canvas = tuple(int(v) for v in a.canvas.lower().split("x")) if a.canvas else ((1024, 572) if bat else (1024, 1024))
    d = dict(y_saia=a.y_saia or (230 if bat else 400), y_fiapo=a.y_fiapo or (420 if bat else 770), y_fim=a.y_fim or (540 if bat else 968),
             bob=a.bob if a.bob is not None else (8 if bat else 18), onda=a.onda if a.onda is not None else (12 if bat else 20),
             balanco=a.balanco if a.balanco is not None else (6 if bat else 10))
    base = carregar(a.imagem, canvas)
    folha = Image.new("RGB", (canvas[0] * cols, canvas[1] * rows), (255, 0, 255))
    for k in range(n):
        q = quadro(base, k, n, inclina=a.inclina, **d)
        folha.paste(Image.fromarray(q), ((k % cols) * canvas[0], (k // cols) * canvas[1]))
    folha.save(a.saida); print("salvo", a.saida, n, "quadros")

if __name__ == "__main__": main()
