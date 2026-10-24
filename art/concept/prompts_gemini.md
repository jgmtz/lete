# Prompts do Gemini (teste de tamanho do M0)

Modelo em `docs/ARTE.md` 7.2. Uma geração por personagem; se a primeira sair boa,
use-a como referência de estilo na segunda ("same style as this image").
Salve as imagens aqui como `aulo.png` e `ismena.png` e rode:

```
python tools/pixelizar.py art/concept/aulo.png art/concept/ismena.png --alturas 32 48 64
```

## Aulo, o Soldado (muito detalhe)

```
Pixel art character concept, full body, standing, 3/4 view facing front-right,
on a flat solid magenta (#FF00FF) background, no shadow, no ground.
Sober Greco-Roman dark fantasy. Slightly stylized proportions (head about
1/4 of body height), simple readable silhouette, hard pixel edges,
no anti-aliasing, no gradients, no dithering, maximum 16 colors.
Palette: bone #D9CBB0, terracotta #B5573B, pomegranate #8C1F2E,
charcoal #25222B, Lethe water #4E7F86, gold #C8A24A.
Character: weathered middle-aged legionary, upright posture, old dented iron
armor over a faded, desaturated red tunic (dull brick red, never a strong red),
helmet held under one arm, short graying hair, short sword at the belt.
```

## Ismena, a Carpideira (silhueta simples)

```
Pixel art character concept, full body, standing, 3/4 view facing front-right,
on a flat solid magenta (#FF00FF) background, no shadow, no ground.
Sober Greco-Roman dark fantasy. Slightly stylized proportions (head about
1/4 of body height), simple readable silhouette, hard pixel edges,
no anti-aliasing, no gradients, no dithering, maximum 16 colors.
Palette: bone #D9CBB0, terracotta #B5573B, pomegranate #8C1F2E,
charcoal #25222B, Lethe water #4E7F86, gold #C8A24A.
Character: mourner with a long black veil, dark dress with ice-blue trim,
drooping shoulders, open hands, small cloth in one hand.
```

## Observações

- No Aulo acrescentei "faded, desaturated red": a romã deve continuar sendo o único
  vermelho forte (`PENDENCIAS.md`, seção 4).
- Se sobrar borda magenta, suba `--tolerancia`; se sumir parte do personagem, desça.
