#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
andar_vestido.py: andar para personagens de vestido longo (pernas escondidas). Deforma a imagem pintada
(celula 1024x1024, fundo magenta): balanço da barra, salto do corpo e a ponta do sapato alternando.
Gera a folha 4x3 (12 quadros) para tools/sprite_andar.py --celula-fixa.
Uso: python andar_vestido.py imagem.png --vista frente|costas|lado --saida folha.png [--espelhar]
"""
import argparse, math
import numpy as np, cv2
from PIL import Image

Q = 17.36   # 1 px do sprite em px da celula (grade do sprite_andar --celula-fixa p/ figura de 66..985)

def ss(x, a, b):
    t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

def celula(img):
    im = Image.open(img).convert("RGB")
    c = Image.new("RGB", (1024, 1024), (255, 0, 255)); c.paste(im, ((1024 - im.width) // 2, 0)); return np.array(c)

def quadro(a, k, vista, bob, sway, lift, y_sway0, y_hem0, y_feet0, cx, pe=None):
    H, W = a.shape[:2]
    ph = 2 * math.pi * k / 12
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    w_s = ss(yy, y_sway0, y_hem0 + 60)          # balanco cresce da cintura para a barra
    w_h = ss(yy, y_hem0, y_feet0)               # so a barra/sapato sobe
    w_f = 1 - ss(yy, y_hem0, y_feet0 + 20)      # corpo sobe, pes ficam
    s = math.sin(ph)
    dy = -bob * abs(s) * w_f
    dx = sway * w_s * s
    if vista == "lado":
        frente = ss(xx, cx - 30, cx + 30)       # metade da frente
        dy = dy - lift * w_h * (max(0, s) * frente + max(0, -s) * (1 - frente))
    else:
        esq = ss(xx, cx + 30, cx - 30)          # metade esquerda da imagem
        dy = dy - lift * w_h * (max(0, s) * esq + max(0, -s) * (1 - esq))
    mx, my = xx - dx, yy - dy
    r = cv2.remap(a, mx, my, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=(255, 0, 255))
    if pe is not None and vista == "lado" and max(0, -s) > 0.15:
        # pe de tras aparece por baixo da barra levantada (so onde ha fundo = atras do vestido)
        (x0, x1, y0, y1), dxp = pe["caixa"], pe["desl"]
        patch = a[y0:y1, x0:x1]; solido = ~((abs(patch[..., 0].astype(int) - 255) < 70) & (patch[..., 1] < 100) & (abs(patch[..., 2].astype(int) - 255) < 70))
        sub = r[y0:y1, x0 + dxp:x1 + dxp]
        fundo = (abs(sub[..., 0].astype(int) - 255) < 70) & (sub[..., 1] < 100) & (abs(sub[..., 2].astype(int) - 255) < 70)
        m = solido & fundo
        sub[m] = patch[m]; r[y0:y1, x0 + dxp:x1 + dxp] = sub
    return r

def main():
    p = argparse.ArgumentParser(); p.add_argument("img"); p.add_argument("--vista", required=True)
    p.add_argument("--saida", required=True); p.add_argument("--espelhar", action="store_true")
    p.add_argument("--bob", type=float, default=1.0); p.add_argument("--sway", type=float, default=1.4)
    p.add_argument("--lift", type=float, default=2.0)
    p.add_argument("--y-sway0", type=int, default=430); p.add_argument("--y-hem0", type=int, default=900)
    p.add_argument("--y-feet0", type=int, default=985)
    p.add_argument("--pe-tras", action="store_true", help="perfil: copia o sapato da frente como pe de tras, visivel quando a barra de tras sobe")
    p.add_argument("--pe-x0", type=int, default=498); p.add_argument("--pe-x1", type=int, default=615)
    p.add_argument("--pe-y0", type=int, default=962); p.add_argument("--pe-y1", type=int, default=990)
    p.add_argument("--pe-desl", type=int, default=110)
    a = p.parse_args()
    base = celula(a.img)
    if a.espelhar: base = base[:, ::-1].copy()
    cx = 512
    pe = None
    if a.pe_tras:
        x0, x1 = (a.pe_x0, a.pe_x1) if not a.espelhar else (1023 - a.pe_x1, 1023 - a.pe_x0)
        pe = dict(caixa=(x0, x1, a.pe_y0, a.pe_y1), desl=(-a.pe_desl if not a.espelhar else a.pe_desl))
    frames = [quadro(base, k, a.vista, a.bob * Q, a.sway * Q, a.lift * Q, a.y_sway0, a.y_hem0, a.y_feet0, cx, pe) for k in range(12)]
    sheet = np.zeros((3 * 1024, 4 * 1024, 3), np.uint8)
    for k, f in enumerate(frames): sheet[(k // 4) * 1024:(k // 4 + 1) * 1024, (k % 4) * 1024:(k % 4 + 1) * 1024] = f
    Image.fromarray(sheet).save(a.saida)

if __name__ == "__main__": main()
