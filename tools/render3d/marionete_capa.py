#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
marionete_capa.py: perfil de personagem cuja capa/vestido cobre as coxas (so canela e pe aparecem).
A capa (corpo) fica na peca base e so sobe e desce; as pernas (canela + pe) seguem o esqueleto 3D por
BAIXO da capa. O joelho fica escondido sob a barra. Ha so uma perna pintada: a outra e copia mais escura.
Uso: python marionete_capa.py calias_direita joints.json saida.png
"""
import sys, json, math, os
import numpy as np, cv2
from PIL import Image
from PIL import ImageFilter
from marionete import carregar, recorte, ret, afim, mover, H, KL_PERNA, estender_topo

CFG = {
 "calias_frente": dict(modo="fc", img="../../art/concept/gemini/companheiros/calias_sim/pad_f.png", vista="f0", q_n=58.0,
    rest={"hipR": (478, 672), "kneeR": (478, 790), "ankleR": (490, 905), "toeR": (490, 955),
          "hipL": (572, 672), "kneeL": (572, 790), "ankleL": (565, 905), "toeL": (565, 955)},
    caixas={"R": (405, 686, 532, 1022), "L": (532, 686, 645, 1022)}, thigh_y0=690, thigh_ymax=815, shin_y0=808, foot_y=895, foot_y1=1022,
    thigh_ref=(712, 672), kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)], preenche_y=862, interior=(49, 41, 42), recorta_y=0),
 "calias_costas": dict(modo="fc", img="../../art/concept/gemini/companheiros/calias_sim/pad_c.png", vista="c0", q_n=59.0,
    rest={"hipL": (478, 672), "kneeL": (478, 790), "ankleL": (478, 905), "toeL": (478, 955),
          "hipR": (572, 672), "kneeR": (572, 790), "ankleR": (562, 905), "toeR": (562, 955)},
    caixas={"L": (425, 826, 522, 1022), "R": (522, 826, 620, 1022)}, thigh_y0=826, thigh_ymax=826, shin_y0=826, foot_y=895, foot_y1=1022,
    sem_coxa=True, ext_shin=(850, 740), kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)], preenche_y=0, interior=(49, 41, 42), recorta_y=0),
 "lia_frente": dict(modo="fc", img="../../art/concept/gemini/companheiros/lia_sim/pad_f.png", vista="f0", q_n=57.0,
    rest={"hipR": (436, 672), "kneeR": (436, 790), "ankleR": (434, 905), "toeR": (429, 955),
          "hipL": (586, 672), "kneeL": (586, 790), "ankleL": (588, 905), "toeL": (594, 955)},
    caixas={"R": (330, 808, 512, 1022), "L": (512, 808, 700, 1022)}, thigh_y0=808, thigh_ymax=808, shin_y0=808, foot_y=905, foot_y1=1022,
    sem_coxa=True, ext_shin=(850, 740), kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)], preenche_y=0, interior=(49, 41, 42), recorta_y=59),
 "lia_costas": dict(modo="fc", img="../../art/concept/gemini/companheiros/lia_sim/pad_c.png", vista="c0", q_n=57.0,
    rest={"hipL": (436, 672), "kneeL": (436, 790), "ankleL": (434, 905), "toeL": (429, 955),
          "hipR": (586, 672), "kneeR": (586, 790), "ankleR": (588, 905), "toeR": (594, 955)},
    caixas={"L": (330, 808, 512, 1022), "R": (512, 808, 700, 1022)}, thigh_y0=808, thigh_ymax=808, shin_y0=808, foot_y=905, foot_y1=1022,
    sem_coxa=True, ext_shin=(850, 740), kl=[(0.6, 1.05), (0.6, 1.05), (1.0, 1.0)], preenche_y=0, interior=(49, 41, 42), recorta_y=59),
 "lia_direita": dict(modo="duas", img="../../art/concept/gemini/companheiros/lia_sim/pad_pd.png", vista="d", inicio=6, q_n=57.0,
    rest_far={"hip": (480, 640), "knee": (480, 790), "ankle": (480, 920), "toe": (572, 982)},
    rest_near={"hip": (541, 640), "knee": (541, 790), "ankle": (541, 928), "toe": (633, 990)},
    y_top=808, canela_y1_far=918, pe_far_do_perto=(-61, -8), canela_y1_near=926, pe_y0_far=912, pe_y0_near=924, ref_topo=(818, 780), amp=0.5,
    divisor=(498, 806, 0.17, 925, 516), recorta_y=59, y_emp0=540, y_emp1=812, empurra=0.9, escurece=0.85),
 "calias_direita": dict(
    img="../../art/concept/gemini/companheiros/calias_sim/pad_pd.png", vista="d", inicio=6, q_n=59.0,
    rest={"hip": (520, 640), "knee": (505, 780), "ankle": (500, 903), "toe": (592, 962)},
    caixa_canela=(466, 846, 536), caixa_pe=(455, 893, 610), canela_y1=905, pe_y0=897,
    ref_topo=(856, 700), escurece=0.80, empurra=0.9, y_emp0=540, y_emp1=850),
}

def main_fc(c, jf, saida):
    REST = c["rest"]
    J = json.load(open(jf))[c["vista"]]; rest, frames = J["rest"], J["frames"]
    s = 850.0 / (rest["feet0"][1] - rest["top"][1])
    rgba = carregar(c["img"])
    r_, g_ = rgba[:, :, 0].astype(int), rgba[:, :, 1].astype(int)
    naoverde = (rgba[:, :, 3] > 0) & (r_ > g_ + 14)
    def dil(m, k):
        return np.array(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * k + 1))) > 0
    anel = dil(naoverde, c.get("anel", 5))
    P = {}; mask_total = np.zeros((H, 1024), bool)
    for n, (x0, y0, x1, y1) in c["caixas"].items():
        leg = anel & ret(x0, y0, x1, y1) & (rgba[:, :, 3] > 0)
        mask_total |= leg
        if c.get("sem_coxa"):
            P["thigh" + n] = Image.fromarray(np.zeros((H, 1024, 4), np.uint8), "RGBA")
        else:
            P["thigh" + n] = estender_topo(recorte(rgba, leg & ret(0, c["thigh_y0"], 1024, c["thigh_ymax"])), *c["thigh_ref"])
        sh = np.array(recorte(rgba, leg & ret(0, c["shin_y0"], 1024, c["foot_y"] + 8)))
        if c.get("ext_shin"):
            yr, yi = c["ext_shin"]
            for y in range(yi, yr): sh[y] = sh[yr]
        P["shin" + n] = Image.fromarray(sh, "RGBA")
        P["foot" + n] = recorte(rgba, leg & ret(0, c["foot_y"], 1024, c["foot_y1"]))
    base = rgba.copy(); base[:, :, 3] = np.where(mask_total, 0, rgba[:, :, 3])
    if c["preenche_y"]:
        pre = mask_total & (np.arange(H)[:, None] < c["preenche_y"]) & (rgba[:, :, 3] > 0)
        base[pre, :3] = c["interior"]; base[pre, 3] = 255
    base = Image.fromarray(base, "RGBA")
    mv = lambda fr, a_, b_: (fr[b_][0] - fr[a_][0], fr[b_][1] - fr[a_][1])
    mvr = lambda a_, b_: (rest[b_][0] - rest[a_][0], rest[b_][1] - rest[a_][1])
    ini = c.get("inicio", 0); frames = frames[ini:] + frames[:ini]
    quadros = []
    for fr in frames:
        tela = Image.new("RGBA", (1024, H), (0, 0, 0, 0))
        dp = (s * (fr["pelvis"][0] - rest["pelvis"][0]), s * (fr["pelvis"][1] - rest["pelvis"][1]))
        q = 1024 / c["q_n"]; dp = (round(dp[0] / q) * q, round(dp[1] / q) * q)
        for n in sorted(("R", "L"), key=lambda n: fr["ankle" + n][2]):
            ch = ["hip" + n, "knee" + n, "ankle" + n, "toe" + n]
            J1 = [np.array((REST[ch[0]][0] + s * (fr[ch[0]][0] - rest[ch[0]][0]), REST[ch[0]][1] + s * (fr[ch[0]][1] - rest[ch[0]][1])), float)]; info = []
            for i in range(3):
                d = np.array(REST[ch[i + 1]], float) - np.array(REST[ch[i]], float)
                m0 = np.array(mvr(ch[i], ch[i + 1]), float); m1 = np.array(mv(fr, ch[i], ch[i + 1]), float)
                th = math.atan2(m1[1], m1[0]) - math.atan2(m0[1], m0[0])
                kmn, kmx = c["kl"][i]; k = min(kmx, max(kmn, np.linalg.norm(m1) / np.linalg.norm(m0)))
                cs, sn = math.cos(th), math.sin(th)
                J1.append(J1[-1] + k * np.array([cs * d[0] - sn * d[1], sn * d[0] + cs * d[1]])); info.append((th, k))
            if not c.get("sem_coxa"):
                tela.alpha_composite(afim(P["thigh" + n], REST[ch[0]], REST[ch[1]], J1[0], *info[0]))
            tela.alpha_composite(afim(P["foot" + n], REST[ch[2]], REST[ch[3]], J1[2], info[1][0], 1.0))
            tela.alpha_composite(afim(P["shin" + n], REST[ch[1]], REST[ch[2]], J1[1], *info[1]))
        tela2 = Image.new("RGBA", (1024, H), (0, 0, 0, 0)); tela2.alpha_composite(mover(base, dp)); tela2.alpha_composite(tela)
        if c["preenche_y"]:   # a barra da tunica volta por cima do topo das coxas
            topo = np.array(base); topo[c["thigh_y0"] + 4:, :, 3] = 0
            tela2.alpha_composite(mover(Image.fromarray(topo, "RGBA"), dp))
        else:
            tela2 = tela; tela2.alpha_composite(mover(base, dp))
        tela = tela2
        ry = c.get("recorta_y", 0); quadros.append(tela.crop((0, ry, 1024, ry + 1024)))
    cols = 4; rows = (len(quadros) + cols - 1) // cols
    folha = Image.new("RGB", (1024 * cols, 1024 * rows), (255, 0, 255))
    for i, qd in enumerate(quadros):
        bg = Image.new("RGBA", (1024, 1024), (255, 0, 255, 255)); bg.alpha_composite(qd)
        folha.paste(bg.convert("RGB"), ((i % cols) * 1024, (i // cols) * 1024))
    folha.save(saida); print("salvo", saida, len(quadros), "quadros")

def main_duas(c, jf, saida):
    J = json.load(open(jf))[c["vista"]]; rest, frames = J["rest"], J["frames"]
    s = 850.0 / (rest["feet0"][1] - rest["top"][1])
    rgba = carregar(c["img"])
    r_, g_ = rgba[:, :, 0].astype(int), rgba[:, :, 1].astype(int)
    verde = (rgba[:, :, 3] > 0) & (g_ >= r_ - 6) & (g_ > 90)
    yy0, xx0 = np.mgrid[0:H, 0:1024]
    bx0, by0, bk, by1, bxf = c["divisor"]
    xb = np.where(yy0 < by1, bx0 + (yy0 - by0) * bk, bxf)
    legs = (yy0 >= c["y_top"]) & (rgba[:, :, 3] > 0) & ~verde & (xx0 > 400) & (xx0 < 700) & (yy0 < 1020)
    far = legs & (xx0 < xb); near = legs & (xx0 >= xb)
    def pieces(mask, y1, y0f, ref):
        can = np.array(recorte(rgba, mask & (yy0 < y1)))
        yr, yi = ref
        for y in range(yi, yr): can[y] = can[yr]
        return Image.fromarray(can, "RGBA"), recorte(rgba, mask & (yy0 >= y0f))
    def escurecer(img, f):
        b = np.array(img).astype(float); b[:, :, :3] *= f; return Image.fromarray(b.clip(0, 255).astype(np.uint8), "RGBA")
    cf, pf = pieces(far, c["canela_y1_far"], c["pe_y0_far"], c["ref_topo"])
    cn, pn = pieces(near, c["canela_y1_near"], c["pe_y0_near"], c["ref_topo"])
    if c.get("pe_far_do_perto"):   # pe de tras copiado do pe da frente (o original estava escondido atras dele)
        dx, dy = c["pe_far_do_perto"]
        pf = Image.fromarray(np.roll(np.roll(np.array(pn), dy, axis=0), dx, axis=1), "RGBA")
    PEC = {0: (escurecer(cf, c["escurece"]), escurecer(pf, c["escurece"]), c["rest_far"]), 1: (cn, pn, c["rest_near"])}
    base = rgba.copy(); base[:, :, 3] = np.where(legs, 0, rgba[:, :, 3]); base = Image.fromarray(base, "RGBA")
    mv = lambda fr, a_, b_: (fr[b_][0] - fr[a_][0], fr[b_][1] - fr[a_][1])
    mvr = lambda a_, b_: (rest[b_][0] - rest[a_][0], rest[b_][1] - rest[a_][1])
    ini = c.get("inicio", 0); frames = frames[ini:] + frames[:ini]
    quadros = []
    for fr in frames:
        tela = Image.new("RGBA", (1024, H), (0, 0, 0, 0))
        dp = (s * (fr["pelvis"][0] - rest["pelvis"][0]), s * (fr["pelvis"][1] - rest["pelvis"][1]))
        q = 1024 / c["q_n"]; dp = (round(dp[0] / q) * q, round(dp[1] / q) * q)
        ordem = sorted(("R", "L"), key=lambda n: fr["ankle" + n][2]); kn = []
        for idx, n in enumerate(ordem):
            can_p, pe_p, REST = PEC[idx]
            nomes = ["hip" + n, "knee" + n, "ankle" + n, "toe" + n]; chave = ["hip", "knee", "ankle", "toe"]
            start = (REST["hip"][0] + s * (fr[nomes[0]][0] - rest[nomes[0]][0]), REST["hip"][1] + s * (fr[nomes[0]][1] - rest[nomes[0]][1]))
            J1 = [np.array(start, float)]; info = []
            for i in range(3):
                d = np.array(REST[chave[i + 1]], float) - np.array(REST[chave[i]], float)
                m0 = np.array(mvr(nomes[i], nomes[i + 1]), float); m1 = np.array(mv(fr, nomes[i], nomes[i + 1]), float)
                th = c.get("amp", 1.0) * (math.atan2(m1[1], m1[0]) - math.atan2(d[1], d[0]))
                kmn, kmx = KL_PERNA[i]; k = min(kmx, max(kmn, np.linalg.norm(m1) / np.linalg.norm(m0)))
                cs, sn = math.cos(th), math.sin(th)
                J1.append(J1[-1] + k * np.array([cs * d[0] - sn * d[1], sn * d[0] + cs * d[1]])); info.append((th, k))
            kn.append(J1[1][0] - REST["knee"][0])
            tela.alpha_composite(afim(pe_p, REST["ankle"], REST["toe"], J1[2], info[1][0], 1.0))
            tela.alpha_composite(afim(can_p, REST["knee"], REST["ankle"], J1[1], *info[1]))
        emp = c["empurra"] * max(0.0, max(kn))
        yy, xx = np.mgrid[0:H, 0:1024].astype(np.float32)
        t_ = np.clip((yy - c["y_emp0"]) / (c["y_emp1"] - c["y_emp0"]), 0, 1); w_ = t_ * t_ * (3 - 2 * t_)
        deform = cv2.remap(np.array(base), (xx - emp * w_).astype(np.float32), yy, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        tela.alpha_composite(mover(Image.fromarray(deform, "RGBA"), dp))
        ry = c.get("recorta_y", 0); quadros.append(tela.crop((0, ry, 1024, ry + 1024)))
    cols = 4; rows = (len(quadros) + cols - 1) // cols
    folha = Image.new("RGB", (1024 * cols, 1024 * rows), (255, 0, 255))
    for i, qd in enumerate(quadros):
        bg = Image.new("RGBA", (1024, 1024), (255, 0, 255, 255)); bg.alpha_composite(qd)
        folha.paste(bg.convert("RGB"), ((i % cols) * 1024, (i // cols) * 1024))
    folha.save(saida); print("salvo", saida, len(quadros), "quadros")

def main():
    modo, jf, saida = sys.argv[1], sys.argv[2], sys.argv[3]
    c = CFG[modo]; REST = c.get("rest")
    if c.get("modo") == "fc": return main_fc(c, jf, saida)
    if c.get("modo") == "duas": return main_duas(c, jf, saida)
    J = json.load(open(jf))[c["vista"]]; rest, frames = J["rest"], J["frames"]
    s = 850.0 / (rest["feet0"][1] - rest["top"][1])
    rgba = carregar(c["img"])
    r_, g_ = rgba[:, :, 0].astype(int), rgba[:, :, 1].astype(int)
    verde = (rgba[:, :, 3] > 0) & (g_ >= r_ - 6) & (g_ > 90)
    x0, y0, x1 = c["caixa_canela"]; px0, py0, px1 = c["caixa_pe"]
    legs = (ret(x0, y0, x1, H) | ret(px0, py0, px1, H)) & ~verde
    canela = recorte(rgba, legs & ret(0, 0, 1024, c["canela_y1"]))
    a = np.array(canela); yr, yi = c["ref_topo"]
    for y in range(yi, yr): a[y] = a[yr]
    canela = Image.fromarray(a, "RGBA")
    pe = recorte(rgba, legs & ret(0, c["pe_y0"], 1024, H))
    base = rgba.copy(); base[:, :, 3] = np.where(legs, 0, rgba[:, :, 3]); base = Image.fromarray(base, "RGBA")
    def escurecer(img, f):
        b = np.array(img).astype(float); b[:, :, :3] *= f; return Image.fromarray(b.clip(0, 255).astype(np.uint8), "RGBA")
    pecas = {"far": (escurecer(canela, c["escurece"]), escurecer(pe, c["escurece"])), "near": (canela, pe)}
    mv = lambda fr, a_, b_: (fr[b_][0] - fr[a_][0], fr[b_][1] - fr[a_][1])
    mvr = lambda a_, b_: (rest[b_][0] - rest[a_][0], rest[b_][1] - rest[a_][1])
    ini = c.get("inicio", 0); frames = frames[ini:] + frames[:ini]
    quadros = []
    for fr in frames:
        tela = Image.new("RGBA", (1024, H), (0, 0, 0, 0))
        dp = (s * (fr["pelvis"][0] - rest["pelvis"][0]), s * (fr["pelvis"][1] - rest["pelvis"][1]))
        q = 1024 / c["q_n"]; dp = (round(dp[0] / q) * q, round(dp[1] / q) * q)
        ordem = sorted(("R", "L"), key=lambda n: fr["ankle" + n][2]); joelhos = []
        for idx, n in enumerate(ordem):
            nomes = ["hip" + n, "knee" + n, "ankle" + n, "toe" + n]; chave = ["hip", "knee", "ankle", "toe"]
            start = (REST["hip"][0] + s * (fr[nomes[0]][0] - rest[nomes[0]][0]), REST["hip"][1] + s * (fr[nomes[0]][1] - rest[nomes[0]][1]))
            J1 = [np.array(start, float)]; info = []
            for i in range(3):
                d = np.array(REST[chave[i + 1]], float) - np.array(REST[chave[i]], float)
                m0 = np.array(mvr(nomes[i], nomes[i + 1]), float); m1 = np.array(mv(fr, nomes[i], nomes[i + 1]), float)
                th = math.atan2(m1[1], m1[0]) - math.atan2(d[1], d[0])
                kmn, kmx = KL_PERNA[i]; k = min(kmx, max(kmn, np.linalg.norm(m1) / np.linalg.norm(m0)))
                cs, sn = math.cos(th), math.sin(th)
                J1.append(J1[-1] + k * np.array([cs * d[0] - sn * d[1], sn * d[0] + cs * d[1]])); info.append((th, k))
            joelhos.append(J1)
            canela_p, pe_p = pecas["far" if idx == 0 else "near"]
            tela.alpha_composite(mover(afim(pe_p, REST["ankle"], REST["toe"], J1[2], info[1][0], 1.0), (0, 0)))
            tela.alpha_composite(mover(afim(canela_p, REST["knee"], REST["ankle"], J1[1], *info[1]), (0, 0)))
        joe = [(J_[1][0] - REST["knee"][0]) for J_ in joelhos]
        fwd = max(0.0, max(joe)); emp = c["empurra"] * fwd
        yy, xx = np.mgrid[0:H, 0:1024].astype(np.float32)
        t_ = np.clip((yy - c["y_emp0"]) / (c["y_emp1"] - c["y_emp0"]), 0, 1); w_ = t_ * t_ * (3 - 2 * t_)
        deform = cv2.remap(np.array(base), (xx - emp * w_).astype(np.float32), yy, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        tela.alpha_composite(mover(Image.fromarray(deform, "RGBA"), dp))
        quadros.append(tela.crop((0, c.get("recorta_y", 0), 1024, c.get("recorta_y", 0) + 1024)))
    cols = 4; rows = (len(quadros) + cols - 1) // cols
    folha = Image.new("RGB", (1024 * cols, 1024 * rows), (255, 0, 255))
    for i, qd in enumerate(quadros):
        bg = Image.new("RGBA", (1024, 1024), (255, 0, 255, 255)); bg.alpha_composite(qd)
        folha.paste(bg.convert("RGB"), ((i % cols) * 1024, (i // cols) * 1024))
    folha.save(saida); print("salvo", saida, len(quadros), "quadros")

if __name__ == "__main__": main()
