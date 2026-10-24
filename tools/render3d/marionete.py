#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
marionete.py: anima o SPRITE do Gemini (estilo pintado) com o movimento de um esqueleto 3D.
Recorta a imagem parada do personagem em pecas (cabeca, braco, pernas) e, para cada quadro,
move/gira as pecas conforme as juntas do esqueleto 3D (joints.json, gerado por joints.py
a partir de index.html). Reconstroi o que o braco escondia (manga, saia, bainha da espada).

Uso:  python marionete.py frente|costas <joints.json> <saida.png>
Saida: folha magenta 4 colunas de celulas 1024x1024, pronta para
       folha_para_quadros.py --colunas 4 --linhas 3 --alturas 96 --cores 28
Para outro personagem/vista: copie um bloco de CONFIG e marque as juntas na imagem parada
(grade de 20 px ajuda). R = lado da perna/braco com nome "R" no modelo 3D (aqui o braco que balanca).
"""
import json, sys, math, os
import numpy as np
from PIL import Image, ImageDraw
import cv2

KL_PERNA = [(0.88, 1.05), (0.93, 1.05), (1.0, 1.0)]  # coxa, canela+bota, pe (o pe nao encolhe)
KL_BRACO = [(0.85, 1.05), (0.85, 1.05)]

CONFIG = {
 "sim_direita": dict(
    img="../../art/concept/gemini/companheiros/aulo_sim/sim_pd_nariz.png", vista="d", absoluto=True,
    inicio=3, cabeca_rigida=True, pe_rigido=True,
    rest={"hipR": (584, 672), "kneeR": (588, 825), "ankleR": (612, 918), "toeR": (718, 968),
          "hipL": (491, 672), "kneeL": (405, 808), "ankleL": (330, 905), "toeL": (420, 962),
          "neck": (530, 225), "pelvis": (500, 650)},
    legR=(540, 700, 740, 1000), legL=(290, 700, 540, 1000),
    thigh_ymax=818, shin_y0=812, foot_y={"R": 925, "L": 920}, thigh_ref=(712, 672), coxa_estreita=30, q_n=58.0,
    arm_poly=None, head=(440, 60, 600, 232)),
 "sim_esquerda": dict(
    img="../../art/concept/gemini/companheiros/aulo_sim/sim_pe_nariz.png", vista="l", absoluto=True,
    inicio=3, cabeca_rigida=True, pe_rigido=True,
    rest={"hipR": (439, 672), "kneeR": (435, 825), "ankleR": (411, 918), "toeR": (305, 968),
          "hipL": (532, 672), "kneeL": (618, 808), "ankleL": (693, 905), "toeL": (603, 962),
          "neck": (493, 225), "pelvis": (523, 650)},
    legR=(283, 700, 483, 1000), legL=(483, 700, 733, 1000),
    thigh_ymax=818, shin_y0=812, foot_y={"R": 925, "L": 920}, thigh_ref=(712, 672), coxa_estreita=30, q_n=58.0,
    arm_poly=None, head=(423, 60, 583, 232)),
 "sim_frente": dict(
    img="../../art/concept/gemini/companheiros/aulo_sim/sim_f.png", vista="f0",
    cabeca_rigida=True, pe_rigido=True, kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)],
    rest={"hipR": (441, 672), "kneeR": (443, 785), "ankleR": (442, 935), "toeR": (442, 975),
          "hipL": (584, 672), "kneeL": (582, 785), "ankleL": (586, 935), "toeL": (586, 975),
          "neck": (529, 215), "pelvis": (512, 650)},
    legR=(360, 700, 512, 1020), legL=(512, 700, 700, 1020),
    thigh_ymax=822, shin_y0=815, foot_y={"R": 940, "L": 940}, thigh_ref=(712, 672), coxa_estreita=45, q_n=58.0,
    arm_poly=None, head=(440, 55, 600, 232)),
 "sim_costas": dict(
    img="../../art/concept/gemini/companheiros/aulo_sim/sim_c.png", vista="c0",
    cabeca_rigida=True, pe_rigido=True, kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)],
    rest={"hipL": (441, 672), "kneeL": (443, 785), "ankleL": (442, 935), "toeL": (442, 975),
          "hipR": (584, 672), "kneeR": (582, 785), "ankleR": (586, 935), "toeR": (586, 975),
          "neck": (529, 205), "pelvis": (512, 650)},
    legL=(360, 700, 512, 1020), legR=(512, 700, 700, 1020),
    thigh_ymax=822, shin_y0=815, foot_y={"R": 940, "L": 940}, thigh_ref=(712, 672), coxa_estreita=45, q_n=58.0,
    arm_poly=None, head=(440, 55, 600, 225)),
}

def carregar(path):
    im = Image.open(path).convert("RGB")
    a = np.array(im).astype(int)
    fundo = (abs(a[:, :, 0] - 255) < 60) & (a[:, :, 1] < 90) & (abs(a[:, :, 2] - 255) < 60)
    alfa = cv2.erode((~fundo).astype(np.uint8) * 255, np.ones((2, 2), np.uint8))
    return np.dstack([np.array(im), alfa])

def recorte(rgba, mask):
    out = rgba.copy(); out[:, :, 3] = np.where(mask, rgba[:, :, 3], 0)
    return Image.fromarray(out, "RGBA")

def ret(x0, y0, x1, y1):
    m = np.zeros((1024, 1024), bool); m[y0:y1, x0:x1] = True; return m

def poligono(pts):
    m = Image.new("L", (1024, 1024), 0); ImageDraw.Draw(m).polygon(pts, fill=255); return np.array(m) > 0

def estender_topo(img, y_ref, y_ini):
    a = np.array(img)
    for y in range(y_ini, y_ref): a[y] = a[y_ref]
    return Image.fromarray(a, "RGBA")

def pecas(rgba, c):
    P = {}
    legs = {"R": ret(*c["legR"]), "L": ret(*c["legL"])}
    if c.get("sem_perna"):
        legs = {k: v & ~ret(*c["sem_perna"]) for k, v in legs.items()}
    for n, leg in legs.items():
        P["thigh" + n] = estender_topo(recorte(rgba, leg & ret(0, 0, 1024, c["thigh_ymax"])), *c["thigh_ref"])
        if c.get("coxa_estreita"):
            hx = c["rest"]["hip" + n][0]; arr = np.array(P["thigh" + n]); w0 = c["coxa_estreita"]
            for y in range(0, arr.shape[0]):
                meia = w0 + max(0, y - c["thigh_ref"][0] + 15) * 1.0
                arr[y, :int(max(0, hx - meia)), 3] = 0; arr[y, int(hx + meia):, 3] = 0
            P["thigh" + n] = Image.fromarray(arr, "RGBA")
        yf = c["foot_y"][n]
        P["shin" + n] = recorte(rgba, leg & ret(0, c["shin_y0"], 1024, yf + 8))
        P["foot" + n] = recorte(rgba, leg & ret(0, yf, 1024, 960))
    arm = poligono(c["arm_poly"]) if c.get("arm_poly") else np.zeros((1024, 1024), bool)
    if c.get("arm_poly"):
        ya, yb = c["arm_split"]
        P["upperR"] = recorte(rgba, arm & ret(0, 0, 1024, ya))
        P["foreR"] = recorte(rgba, arm & ret(0, yb, 1024, 1024))
    head = ret(*c["head"]); P["head"] = recorte(rgba, head)
    mover = legs["R"] | legs["L"] | arm | head
    base = rgba.copy(); base[:, :, 3] = np.where(mover, 0, rgba[:, :, 3])
    armpx = arm & (rgba[:, :, 3] > 0)
    def preencher(y0, y1, x0, x1, cor):
        for y in range(y0, y1):
            for x in range(x0, x1):
                if armpx[y, x] and base[y, x, 3] == 0:
                    base[y, x, :3] = cor; base[y, x, 3] = 255
    if c.get("arm_poly"):
        y0, y1, x0, x1, cor = c["manga"]; preencher(y0, y1, x0, x1, cor)
        y0, y1, x0, x1, cor = c["atras"]; preencher(y0, y1, x0, x1, cor)
    # bainha da espada atras da mao
    faixa = Image.new("L", (1024, 1024), 0); dd = ImageDraw.Draw(faixa)
    p0, p1 = np.array(c.get("faixa", ((0, 0), (1, 1), 1))[0], float), np.array(c.get("faixa", ((0, 0), (1, 1), 1))[1], float); hw = c.get("faixa", ((0, 0), (1, 1), 1))[2]
    dv = p1 - p0; nv = np.array([-dv[1], dv[0]]) / np.linalg.norm(dv)
    for halfw, val in ((hw + 4, 128), (hw, 255)):
        dd.polygon([tuple(p0 + nv * halfw), tuple(p0 - nv * halfw), tuple(p1 - nv * halfw), tuple(p1 + nv * halfw)], fill=val)
    fa = np.array(faixa); ocupado = (rgba[:, :, 3] > 0) & ~arm
    cont = (fa == 128) & ~ocupado; cor = (fa == 255) & ~ocupado
    if c.get("arm_poly"):
        base[cont, :3] = (18, 14, 12); base[cont, 3] = 255
        base[cor, :3] = (92, 78, 62); base[cor, 3] = 255
    P["base"] = Image.fromarray(base, "RGBA")
    return P

def afim(img, A0, B0, A1, th, k):
    """gira a peca em torno de A0->A1 por th e encurta k ao longo do osso A0->B0"""
    A0 = np.array(A0, float); B0 = np.array(B0, float); A1 = np.array(A1, float)
    d0 = B0 - A0
    u = d0 / np.linalg.norm(d0); v = np.array([-u[1], u[0]])
    U = np.stack([u, v], 1); Sk = U @ np.diag([k, 1.0]) @ U.T
    cs, sn = math.cos(th), math.sin(th); Lm = np.array([[cs, -sn], [sn, cs]]) @ Sk
    M = np.eye(3); M[:2, :2] = Lm; M[:2, 2] = A1 - Lm @ A0
    Mi = np.linalg.inv(M)
    return img.transform(img.size, Image.AFFINE, (Mi[0, 0], Mi[0, 1], Mi[0, 2], Mi[1, 0], Mi[1, 1], Mi[1, 2]), Image.BICUBIC)

def mover(img, d):
    return img.transform(img.size, Image.AFFINE, (1, 0, -d[0], 0, 1, -d[1]), Image.BICUBIC)

def main():
    modo, jf, saida = sys.argv[1], sys.argv[2], sys.argv[3]
    c = CONFIG[modo]; REST = c["rest"]
    if os.environ.get("DX"): c = dict(c, desloca_x=float(os.environ["DX"]))
    J = json.load(open(jf))[c["vista"]]
    rest, frames = J["rest"], J["frames"]
    s = 850.0 / (rest["feet0"][1] - rest["top"][1])
    P = pecas(carregar(c["img"]), c)
    mv = lambda fr, a, b: (fr[b][0] - fr[a][0], fr[b][1] - fr[a][1])
    mvr = lambda a, b: (rest[b][0] - rest[a][0], rest[b][1] - rest[a][1])
    pos = lambda fr, n: (REST[n][0] + s * (fr[n][0] - rest[n][0]), REST[n][1] + s * (fr[n][1] - rest[n][1]))
    quadros = []
    ini = c.get("inicio", 0); frames = frames[ini:] + frames[:ini]
    if os.environ.get("REPOUSO"): frames = [dict(rest)] + frames[1:]
    for fr in frames:
        tela = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
        dp = (s * (fr["pelvis"][0] - rest["pelvis"][0]), s * (fr["pelvis"][1] - rest["pelvis"][1]))
        dn = (s * (fr["neck"][0] - rest["neck"][0]), s * (fr["neck"][1] - rest["neck"][1]))
        if c.get("cabeca_rigida"):
            q = 1024 / c.get("q_n", 62.0); dp = (round(dp[0] / q) * q, round(dp[1] / q) * q); dn = dp
        def fk(nomes, start, kl):
            J1 = [np.array(start, float)]; info = []
            for a_, b_ in zip(nomes[:-1], nomes[1:]):
                d = np.array(REST[b_], float) - np.array(REST[a_], float)
                m0 = np.array(mvr(a_, b_), float); m1 = np.array(mv(fr, a_, b_), float)
                if c.get("absoluto"):
                    th = math.atan2(m1[1], m1[0]) - math.atan2(d[1], d[0])
                else:
                    th = math.atan2(m1[1], m1[0]) - math.atan2(m0[1], m0[0])
                kmn, kmx = kl[len(info)]
                k = min(kmx, max(kmn, np.linalg.norm(m1) / np.linalg.norm(m0)))
                cs, sn = math.cos(th), math.sin(th)
                J1.append(J1[-1] + k * np.array([cs * d[0] - sn * d[1], sn * d[0] + cs * d[1]]))
                info.append((th, k))
            return J1, info
        for n in sorted(("R", "L"), key=lambda n: fr["ankle" + n][2]):
            ch = ["hip" + n, "knee" + n, "ankle" + n, "toe" + n]
            J1, inf = fk(ch, pos(fr, ch[0]), c.get("kl", KL_PERNA))
            tela.alpha_composite(afim(P["thigh" + n], REST[ch[0]], REST[ch[1]], J1[0], *inf[0]))
            fi = (inf[1][0], 1.0) if c.get("pe_rigido") else inf[2]
            tela.alpha_composite(afim(P["foot" + n], REST[ch[2]], REST[ch[3]], J1[2], *fi))
            tela.alpha_composite(afim(P["shin" + n], REST[ch[1]], REST[ch[2]], J1[1], *inf[1]))
        tela.alpha_composite(mover(P["base"], dp))
        if c.get("arm_poly"):
            ch = ["shR", "elR", "wrR"]; J1, inf = fk(ch, pos(fr, "shR"), KL_BRACO)
            tela.alpha_composite(afim(P["foreR"], REST["elR"], REST["wrR"], J1[1], *inf[1]))
            tela.alpha_composite(afim(P["upperR"], REST["shR"], REST["elR"], J1[0], *inf[0]))
        tela.alpha_composite(mover(P["head"], dn))
        if c.get('desloca_x'):
            tela = mover(tela, (c['desloca_x'], 0))
        quadros.append(tela)
    cols = 4; rows = (len(quadros) + cols - 1) // cols
    folha = Image.new("RGB", (1024 * cols, 1024 * rows), (255, 0, 255))
    for i, q in enumerate(quadros):
        bg = Image.new("RGBA", (1024, 1024), (255, 0, 255, 255)); bg.alpha_composite(q)
        folha.paste(bg.convert("RGB"), ((i % cols) * 1024, (i // cols) * 1024))
    folha.save(saida)
    print("salvo", saida, len(quadros), "quadros, escala", round(s, 3))

if __name__ == "__main__":
    main()
