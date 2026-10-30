#!/bin/bash
# uso: proc_sombra.sh slug ysaia yfiapo yfim bsaia bfiapo bfim
set -e
# Caminhos relativos ao projeto; Python e o do kit (ferramentas/python), ou PY=... para outro.
S=$1; R=$(cd "$(dirname "$0")/.." && pwd); export LETE_R=$R; PY=${PY:-$R/../ferramentas/python/Scripts/python.exe}; [ -x "$PY" ] || PY=python3; T=$R/tools; D=$R/art/concept/gemini/sombras; O=$R/art/sprites/sombras/$S; W=/tmp/work/$S; rm -rf $W; mkdir -p $W; cd $W
$PY - $S <<'P'
import sys,numpy as np
from PIL import Image
s=sys.argv[1]; import os; D=os.environ['LETE_R']+'/art/concept/gemini/sombras/'
for v in ['f','c','pd']:
    a=np.array(Image.open(D+f'{s}_sim/sim_{v}.png').convert('RGB')).astype(int)
    m=((a[...,0]-a[...,1])>45)&((a[...,2]-a[...,1])>45)
    import cv2
    bg=((a[...,0]>180)&(a[...,2]>180)&(a[...,1]<110)).astype('uint8')
    bg=cv2.dilate(bg,np.ones((7,7),np.uint8))>0
    a[m&bg]=(255,0,255)
    Image.fromarray(a.astype('uint8')).save(f'lim_{v}.png')
P
for v in f:frente c:costas pd:lado; do k=${v%%:*}; n=${v##*:}; $PY $T/andar_sombra.py lim_$k.png --saida folha_$k.png --vista $n --y-saia $2 --y-fiapo $3 --y-fim $4 >/dev/null; done
$PY $T/andar_sombra.py $D/${S}_batalha.png --saida folha_bat.png --vista batalha --canvas $($PY -c "from PIL import Image;i=Image.open(\"$D/${S}_batalha.png\");print(f\"{i.width}x{i.height}\")") --y-saia $5 --y-fiapo $6 --y-fim $7 --bob 10 --onda 12 --balanco 6 >/dev/null
for v in f:frente c:costas pd:direita; do k=${v%%:*}; n=${v##*:}; $PY $T/sprite_andar.py folha_$k.png --colunas 4 --linhas 3 --altura 56 --cores 56 --celula-fixa --flutuar --saida out_$n --nome ${S}_$n >/dev/null; done
# Só a arte de 160 px entra no jogo (pixel_160, decidida); prévias (GIF, tira) vão para art/concept/conferencia.
$PY $T/sprite_andar.py folha_bat.png --colunas 3 --linhas 2 --altura 160 --cores 56 --celula-fixa --flutuar --saida out_bat160 --nome ${S}_flutuar >/dev/null
$PY $T/pixelizar.py $D/${S}_batalha.png --alturas 160 --cores 56 --saida pix >/dev/null
C=$R/art/concept/conferencia/sombras/$S; rm -rf $O $C; mkdir -p $O/andar_56px/{frente,costas,direita,esquerda} $O/parado $O/batalha_flutuar_160 $C/andar_56px $C/batalha_flutuar_160
for n in frente costas direita; do cp out_$n/${S}_${n}_[0-9]*.png $O/andar_56px/$n/; done
cp out_bat160/${S}_flutuar_[0-9]*.png $O/batalha_flutuar_160/; cp out_bat160/*.gif out_bat160/*_tira.png $C/batalha_flutuar_160/ 2>/dev/null || true
cp pix/${S}_batalha_160px.png $R/art/sprites/batalha/pixel_160/${S}_34.png
$PY - $S <<'P'
import sys,glob
from PIL import Image, ImageOps
s=sys.argv[1]; import os; O=os.environ['LETE_R']+f'/art/sprites/sombras/{s}/'
for f in sorted(glob.glob(O+f'andar_56px/direita/{s}_direita_*.png')):
    ImageOps.mirror(Image.open(f)).save(f.replace('direita','esquerda'))
for n in ['frente','costas','direita','esquerda']:
    Image.open(O+f'andar_56px/{n}/{s}_{n}_01.png').save(O+f'parado/{s}_parado_{n}.png')
ds=['frente','costas','direita','esquerda']
fr=[[Image.open(O+f'andar_56px/{d}/{s}_{d}_{k:02d}.png').convert('RGBA') for k in range(1,13)] for d in ds]
w=max(i.width for r in fr for i in r);h=max(i.height for r in fr for i in r);out=[]
for k in range(12):
    c=Image.new('RGBA',(w*4,h),(40,40,48,255))
    for j in range(4): c.alpha_composite(fr[j][k],(j*w+(w-fr[j][k].width)//2,h-fr[j][k].height))
    out.append(c.convert('P'))
out[0].save(os.environ['LETE_R']+f'/art/concept/conferencia/sombras/{s}/andar_56px/{s}_andar_4direcoes_8fps.gif',save_all=True,append_images=out[1:],duration=125,loop=0,disposal=2)
# preview
t=[Image.open(f'out_{n}/{s}_{n}_tira.png' if False else O+f'andar_56px/{n}/{s}_{n}_01.png') for n in ds]
P
echo feito $S
