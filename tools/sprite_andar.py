#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sprite_andar.py: reduz uma folha de andar (celulas grandes, fundo magenta) para o sprite de andar
pequeno (ex.: 56 px de altura), com visual de JRPG: reducao suave + nitidez, paleta unica,
contorno escuro de 1 px. Mantem o mesmo fator de escala em todos os quadros e os pes na mesma linha.
Uso: python sprite_andar.py folha.png --colunas 4 --linhas 3 --altura 56 --cores 32 --saida pasta [--fps 8] [--zoom 6]
"""
import argparse, os
import numpy as np
from PIL import Image, ImageFilter
import cv2

def tirar_fundo(img):
    a = np.array(img.convert("RGB")).astype(int)
    fundo = (abs(a[:, :, 0] - 255) < 70) & (a[:, :, 1] < 100) & (abs(a[:, :, 2] - 255) < 70)
    alfa = cv2.erode((~fundo).astype(np.uint8) * 255, np.ones((3, 3), np.uint8))
    rgba = np.dstack([a.astype(np.uint8), alfa])
    # despill: tira o magenta das bordas
    r, g, b = rgba[:, :, 0].astype(int), rgba[:, :, 1].astype(int), rgba[:, :, 2].astype(int)
    exc = np.minimum(r, b) - g
    m = exc > 10
    rgba[:, :, 0] = np.where(m, np.clip(r - exc // 2, 0, 255), r)
    rgba[:, :, 2] = np.where(m, np.clip(b - exc // 2, 0, 255), b)
    return Image.fromarray(rgba, "RGBA")

def main():
    p = argparse.ArgumentParser()
    p.add_argument("folha"); p.add_argument("--colunas", type=int, required=True); p.add_argument("--linhas", type=int, default=1)
    p.add_argument("--altura", type=int, default=56); p.add_argument("--cores", type=int, default=32)
    p.add_argument("--nitidez", type=float, default=1.6); p.add_argument("--contorno", type=int, default=1)
    p.add_argument("--fps", type=int, default=8); p.add_argument("--zoom", type=int, default=6)
    p.add_argument("--celula-fixa", action="store_true", help="reduz a celula inteira (mesma grade em todos os quadros): partes que so se deslocam ficam identicas")
    p.add_argument("--flutuar", action="store_true", help="(com --celula-fixa) corta todos os quadros pelo retangulo uniao, sem realinhar os pes: preserva subir/descer de quem flutua")
    p.add_argument("--saida", required=True); p.add_argument("--nome", default="andar")
    a = p.parse_args()
    os.makedirs(a.saida, exist_ok=True)
    rgba = tirar_fundo(Image.open(a.folha))
    W, H = rgba.size; cw, ch = W // a.colunas, H // a.linhas
    cel = [rgba.crop((c * cw, l * ch, (c + 1) * cw, (l + 1) * ch)) for l in range(a.linhas) for c in range(a.colunas)]
    bbs = [q.getchannel("A").getbbox() for q in cel]
    alto = max(b[3] - b[1] for b in bbs)
    esc = (a.altura - 2 * a.contorno) / alto
    if a.celula_fixa:
        n_ = max(1, int(cw * esc)); esc = n_ / cw
        print("celula fixa: 1 px do sprite = %.2f px da celula" % (1 / esc))
    # ancora x: centro do tronco (metade de cima)
    quadros = []
    for q, b in zip(cel, bbs):
        rec = q if a.celula_fixa else q.crop(b)
        nw, nh = max(1, round(rec.width * esc)), max(1, round(rec.height * esc))
        # pre-multiplica para nao sangrar cor do fundo
        arr = np.array(rec).astype(float); al = arr[:, :, 3:4] / 255
        pm = Image.fromarray(np.dstack([arr[:, :, :3] * al, arr[:, :, 3:4]]).astype(np.uint8), "RGBA")
        small = pm.resize((nw, nh), Image.LANCZOS)
        s = np.array(small).astype(float); al2 = np.clip(s[:, :, 3:4] / 255, 1e-3, 1)
        col = np.clip(s[:, :, :3] / al2, 0, 255)
        rgb = Image.fromarray(col.astype(np.uint8), "RGB").filter(ImageFilter.UnsharpMask(radius=1, percent=int(a.nitidez * 100), threshold=2))
        alfa = Image.fromarray(((s[:, :, 3] > 127) * 255).astype(np.uint8), "L")
        if a.celula_fixa and not a.flutuar:
            bb = alfa.getbbox(); rgb, alfa = rgb.crop(bb), alfa.crop(bb)
        quadros.append((rgb, alfa, b))
    if a.celula_fixa and a.flutuar:
        ub = [al.getbbox() for _, al, _ in quadros]
        U = (min(b[0] for b in ub), min(b[1] for b in ub), max(b[2] for b in ub), max(b[3] for b in ub))
        quadros = [(r.crop(U), al.crop(U), b) for r, al, b in quadros]
    # tela comum: pes alinhados, tronco centralizado
    def ancora(rgb, alfa):
        al = np.array(alfa); topo = al[: max(1, al.shape[0] // 2)]
        xs = np.where(topo > 0)[1]; return int(xs.mean()) if len(xs) else al.shape[1] // 2
    ancs = [ancora(r, al) for r, al, _ in quadros]
    esq = max(ancs); dire = max(r.width - an for (r, _, _), an in zip(quadros, ancs))
    TW, TH = esq + dire + 2 * a.contorno, a.altura
    # paleta unica
    todos = []
    telas = []
    for (rgb, alfa, _), an in zip(quadros, ancs):
        t = Image.new("RGBA", (TW, TH), (0, 0, 0, 0))
        im = rgb.convert("RGBA"); im.putalpha(alfa)
        t.alpha_composite(im, (esq - an + a.contorno, TH - im.height - a.contorno))
        telas.append(t)
    mosaico = Image.new("RGB", (TW * len(telas), TH))
    for i, t in enumerate(telas):
        fundo = Image.new("RGB", t.size, (0, 0, 0)); fundo.paste(t.convert("RGB"), mask=t.getchannel("A")); mosaico.paste(fundo, (i * TW, 0))
    pal = mosaico.quantize(colors=a.cores, method=Image.MEDIANCUT, dither=Image.NONE)
    finais = []
    for t in telas:
        al = np.array(t.getchannel("A")) > 127
        rgb = t.convert("RGB").quantize(palette=pal, dither=Image.NONE).convert("RGB")
        arr = np.array(rgb)
        # contorno escuro de 1 px ao redor
        if a.contorno:
            dil = cv2.dilate(al.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
            borda = dil & ~al
            esc_cor = np.array([22, 18, 20], np.uint8)
            arr[borda] = esc_cor
            al = dil
        out = np.dstack([arr, (al * 255).astype(np.uint8)])
        finais.append(Image.fromarray(out, "RGBA"))
    for i, f in enumerate(finais, 1):
        f.save(os.path.join(a.saida, "%s_%02d.png" % (a.nome, i)))
    Z = a.zoom
    gif = []
    for f in finais:
        bg = Image.new("RGBA", (f.width * Z, f.height * Z), (40, 40, 55, 255))
        bg.alpha_composite(f.resize((f.width * Z, f.height * Z), Image.NEAREST)); gif.append(bg.convert("P", palette=Image.ADAPTIVE))
    gif[0].save(os.path.join(a.saida, "%s_%dfps.gif" % (a.nome, a.fps)), save_all=True, append_images=gif[1:], duration=int(1000 / a.fps), loop=0)
    n = len(finais)
    tira = Image.new("RGBA", (n * (TW * Z + 6), TH * Z), (40, 40, 55, 255))
    for i, f in enumerate(finais): tira.alpha_composite(f.resize((f.width * Z, f.height * Z), Image.NEAREST), (i * (TW * Z + 6), 0))
    tira.save(os.path.join(a.saida, "%s_tira.png" % a.nome))
    print("%d quadros de %dx%d em %s (escala %.3f)" % (n, TW, TH, a.saida, esc))

if __name__ == "__main__":
    main()
