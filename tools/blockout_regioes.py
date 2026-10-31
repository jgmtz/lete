"""Gera as cenas de blockout das regiões (Tênaro e Aqueronte) com os materiais da sala de teste.

Uso (da raiz do projeto): ../ferramentas/python/Scripts/python.exe tools/blockout_regioes.py .
Escreve src/Lete.Game/Regions/*.tscn do zero: depois de ajustar uma cena no editor do Godot, não rode de novo
(ou passe o ajuste para cá). Blocos agora, arte depois (decisão do João, 30/out/2026).
"""
import sys, os

root = sys.argv[1]
out_dir = os.path.join(root, 'src', 'Lete.Game', 'Regions')
os.makedirs(out_dir, exist_ok=True)

HEADER_EXT = [
    ('Script', 'res://src/Lete.Game/World/CameraRig.cs', '1_cam'),
    ('PackedScene', 'res://src/Lete.Game/World/Player.tscn', '2_player'),
    ('Script', 'res://src/Lete.Game/World/Interactable.cs', '3_inter'),
    ('Texture2D', 'res://art/tiles/piso_pedra.png', '4_piso'),
    ('Texture2D', 'res://art/tiles/parede_pedra.png', '5_parede'),
    ('Texture2D', 'res://art/tiles/terracota.png', '6_terracota'),
    ('Texture2D', 'res://art/tiles/agua_lete.png', '7_agua'),
    ('Script', 'res://src/Lete.Game/World/RegionExit.cs', '8_saida'),
    ('Script', 'res://src/Lete.Game/World/TriviaRest.cs', '9_trivia'),
    ('Script', 'res://src/Lete.Game/World/ShadeEncounter.cs', '10_encontro'),
    ('Script', 'res://src/Lete.Game/World/MapSprite.cs', '11_sprite'),
    ('Texture2D', 'res://art/sprites/sombras/remador/parado/remador_parado_frente.png', '12_remador'),
    ('Script', 'res://src/Lete.Game/World/OfferingAltar.cs', '13_altar'),
]


def fmt(v):
    return ('%.3f' % v).rstrip('0').rstrip('.') if isinstance(v, float) else str(v)


class Scene:
    def __init__(self, name, ambient, fog_color, moon_energy):
        self.name = name
        self.nodes = []
        self.ambient = ambient
        self.fog_color = fog_color
        self.moon_energy = moon_energy

    def node(self, text):
        self.nodes.append(text)

    def box(self, name, size, pos, mat='Mat_parede', collide=True, parent='.'):
        self.node(f'''[node name="{name}" type="CSGBox3D" parent="{parent}" groups=["pixel_surface"]]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
use_collision = {'true' if collide else 'false'}
size = Vector3({fmt(size[0])}, {fmt(size[1])}, {fmt(size[2])})
material = SubResource("{mat}")
''')

    def blocker(self, name, size, pos):
        """Parede invisível (a água não se atravessa)."""
        self.node(f'''[node name="{name}" type="StaticBody3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})

[node name="Shape" type="CollisionShape3D" parent="{name}"]
shape = SubResource("{self.box_shape(size)}")
''')

    _shapes = None

    def box_shape(self, size):
        if self._shapes is None:
            self._shapes = {}
        key = tuple(size)
        if key not in self._shapes:
            self._shapes[key] = f'Shape_box{len(self._shapes) + 1}'
        return self._shapes[key]

    def column(self, name, pos, height=2.4, radius=0.35, parent='.'):
        self.node(f'''[node name="{name}" type="CSGCylinder3D" parent="{parent}" groups=["pixel_surface"]]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(height / 2)}, {fmt(pos[2])})
use_collision = true
radius = {fmt(radius)}
height = {fmt(height)}
sides = 8
material = SubResource("Mat_parede")
''')

    def light(self, name, pos, color, energy, rng):
        self.node(f'''[node name="{name}" type="OmniLight3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
light_color = Color({color})
light_energy = {fmt(energy)}
omni_range = {fmt(rng)}
''')

    def water(self, name, size, pos):
        self.box(name, size, pos, mat='Mat_agua', collide=False)

    def examinable(self, name, pos, key):
        self.node(f'''[node name="{name}" type="Area3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
script = ExtResource("3_inter")
JournalKey = "{key}"

[node name="Reach" type="CollisionShape3D" parent="{name}"]
shape = SubResource("Shape_alcance")
''')

    def spring(self, name, pos, kind, amount, key, taken_key):
        """Fonte de oferenda (OfferingAltar): recolher dá o tipo, a frase vai para o Diário."""
        self.node(f'''[node name="{name}" type="Area3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
script = ExtResource("13_altar")
JournalKey = "{key}"
Kind = "{kind}"
Amount = {amount}
TakenKey = "{taken_key}"

[node name="Reach" type="CollisionShape3D" parent="{name}"]
shape = SubResource("Shape_alcance")
''')

    def encounter(self, name, pos, encounter_id, texture='12_remador'):
        """Sombra de encontro (ShadeEncounter): chegar perto abre a luta (content/encounters)."""
        self.node(f'''[node name="{name}" type="Area3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
script = ExtResource("10_encontro")
EncounterId = "{encounter_id}"

[node name="Reach" type="CollisionShape3D" parent="{name}"]
shape = SubResource("Shape_alcance")

[node name="Sprite" type="Sprite3D" parent="{name}"]
texture = ExtResource("{texture}")
script = ExtResource("11_sprite")
''')

    def exit(self, name, pos, size, region, entry):
        self.node(f'''[node name="{name}" type="Area3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(pos[0])}, {fmt(pos[1])}, {fmt(pos[2])})
script = ExtResource("8_saida")
TargetRegion = "{region}"
TargetEntry = "{entry}"

[node name="Shape" type="CollisionShape3D" parent="{name}"]
shape = SubResource("{self.box_shape(size)}")
''')

    def trivia(self, name, pos, trivia_id):
        x, _, z = pos
        self.column(f'{name}Coluna', (x, 0, z), height=2.2, radius=0.3)
        self.node(f'''[node name="{name}Chama" type="OmniLight3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(x)}, 2.6, {fmt(z)})
light_color = Color(1, 0.62, 0.3, 1)
light_energy = 1.6
omni_range = 7.0

[node name="{name}" type="Area3D" parent="."]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(x)}, 0, {fmt(z)})
script = ExtResource("9_trivia")
JournalKey = "obj.trivia.examinar"
TriviaId = "{trivia_id}"

[node name="Reach" type="CollisionShape3D" parent="{name}"]
shape = SubResource("Shape_alcance")
''')

    def markers(self, points, player):
        self.node(f'''[node name="Player" parent="." instance=ExtResource("2_player")]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(player[0])}, 0, {fmt(player[2])})

[node name="CameraRig" type="Node3D" parent="." node_paths=PackedStringArray("Target")]
script = ExtResource("1_cam")
Target = NodePath("../Player")

[node name="Pitch" type="Node3D" parent="CameraRig"]

[node name="Camera3D" type="Camera3D" parent="CameraRig/Pitch"]
projection = 1
current = true
size = 10.0
far = 100.0

[node name="TeleportPoints" type="Node3D" parent="."]
''')
        for name, (x, y, z) in points.items():
            self.node(f'''[node name="{name}" type="Marker3D" parent="TeleportPoints" groups=["teleport_points"]]
transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, {fmt(x)}, {fmt(y)}, {fmt(z)})
''')

    def write(self, file_name):
        ext = '\n'.join(f'[ext_resource type="{t}" path="{p}" id="{i}"]' for t, p, i in HEADER_EXT)
        shapes = ''.join(f'''[sub_resource type="BoxShape3D" id="{sid}"]
size = Vector3({fmt(k[0])}, {fmt(k[1])}, {fmt(k[2])})

''' for k, sid in (self._shapes or {}).items())
        mats = ''.join(f'''[sub_resource type="StandardMaterial3D" id="{mid}"]
albedo_texture = ExtResource("{tex}")
roughness = 1.0
uv1_triplanar = true
uv1_world_triplanar = true
texture_filter = 0

''' for mid, tex in [('Mat_piso', '4_piso'), ('Mat_parede', '5_parede'), ('Mat_terracota', '6_terracota'), ('Mat_agua', '7_agua')])
        body = f'''[gd_scene format=3]

{ext}

[sub_resource type="Environment" id="Environment_regiao"]
background_mode = 1
background_color = Color(0.05, 0.047, 0.06, 1)
ambient_light_source = 2
ambient_light_color = Color({self.ambient})
ambient_light_energy = 0.55
fog_enabled = true
fog_light_color = Color({self.fog_color})
fog_light_energy = 0.35
fog_density = 0.004
fog_sky_affect = 0.0
fog_height = 0.4
fog_height_density = 0.3

{mats}[sub_resource type="SphereShape3D" id="Shape_alcance"]
radius = 1.4

{shapes}[node name="{self.name}" type="Node3D"]

[node name="WorldEnvironment" type="WorldEnvironment" parent="."]
environment = SubResource("Environment_regiao")

[node name="Moon" type="DirectionalLight3D" parent="."]
transform = Transform3D(0.939693, 0, 0.34202, 0.280166, 0.573576, -0.769751, -0.196175, 0.819152, 0.538986, 0, 10, 0)
light_color = Color(0.72, 0.8, 0.88, 1)
light_energy = {fmt(self.moon_energy)}
shadow_enabled = true
directional_shadow_max_distance = 60.0

''' + '\n'.join(self.nodes)
        with open(os.path.join(out_dir, file_name), 'w', encoding='utf-8', newline='\n') as f:
            f.write(body)


# ---- Cabo Tênaro ----
# Como na sala de teste, só os lados do fundo (norte e oeste) têm parede alta; os de perto da câmera (sul e leste)
# têm borda baixa, para não esconder o jogador. Sul (z positivo) é o mar; a gruta desce para o norte (z negativo). O chão é plano (o blockout não tem rampas):
# a descida se lê pelas paredes mais altas e pela luz que some.
t = Scene('Tenaro', '0.45, 0.5, 0.58, 1', '0.306, 0.498, 0.525, 1', 0.8)
# A: a saliência fora da gruta, sobre o mar.
t.box('ChaoSaliencia', (14, 1, 12), (0, -0.5, 4), 'Mat_piso')
t.water('Mar', (40, 0.2, 24), (0, -2.5, 22))
t.box('BordaMar', (14, 0.5, 1), (0, 0.25, 10.5))
t.box('PenhascoOeste', (1, 5, 13), (-7.5, 2.5, 4))
t.box('PenhascoLeste', (1, 0.5, 13), (7.5, 0.25, 4))
t.box('PenhascoNorteA', (5, 2.5, 1), (-4.5, 1.25, -2.5))
t.box('PenhascoNorteB', (5, 1, 1), (4.5, 0.5, -2.5))
t.examinable('OlharMar', (0, 0, 9), 'obj.tenaro.mar')
t.examinable('Boca', (0, 0, -1.2), 'obj.tenaro.boca')
# B: o corredor da descida.
t.box('ChaoCorredor', (6, 1, 24), (0, -0.5, -14.5), 'Mat_piso')
t.box('CorredorOeste', (1, 4, 24), (-3.5, 2, -14.5))
t.box('CorredorLeste', (1, 0.5, 24), (3.5, 0.25, -14.5))
t.box('PedraCaida', (1.6, 0.8, 1.2), (-1.8, 0.4, -9))
t.box('Estreito', (2, 1, 1), (2, 0.5, -19))
# A poça dá a água que o Remador aceita (o encontro das marcas pede a oferenda).
t.spring('Poca', (1.6, 0, -12.5), 'agua', 3, 'obj.tenaro.poca', 'obj.tenaro.poca.recolheu')
t.water('PocaAgua', (1.6, 0.04, 1.6), (1.6, 0.02, -12.5))
t.examinable('Marcas', (-2.5, 0, -22), 'obj.tenaro.marcas')
t.light('LuzBoca', (0, 2.5, -5), '0.55, 0.65, 0.8, 1', 0.8, 8)
# C: a câmara do rio, com a primeira Trívia e a passagem para o Aqueronte.
t.box('ChaoCamara', (18, 1, 16), (0, -0.5, -34.5), 'Mat_piso')
t.box('CamaraOeste', (1, 4, 17), (-9.5, 2, -34.5))
t.box('CamaraLeste', (1, 0.5, 17), (9.5, 0.25, -34.5))
t.box('CamaraSulA', (5.5, 0.5, 1), (-6.25, 0.25, -26.5))
t.box('CamaraSulB', (5.5, 0.5, 1), (6.25, 0.25, -26.5))
t.box('CamaraNorteA', (14, 4, 1), (-2, 2, -43))
t.box('CamaraNorteB', (1, 4, 1), (8.5, 2, -43))
t.water('RioSubterraneo', (12, 0.04, 3), (-3, 0.02, -38.5))
t.blocker('RioBarreira', (12, 2, 3), (-3, 1, -38.5))
t.trivia('Trivia', (-5, 0, -30.5), 'trivia_tenaro')
t.light('LuzCamara', (2, 3, -35), '0.35, 0.55, 0.6, 1', 1.0, 12)
# O Remador-tutorial, uma coisa por luta (decisão do João, 30-31/out/2026): cada encontro aparece depois do anterior.
t.encounter('RemadorBoca', (0, 0, -3), 'remador_boca')
t.encounter('RemadorPoca', (-0.8, 0, -16), 'remador_poca')
t.encounter('RemadorMarcas', (0.5, 0, -24), 'remador_marcas')
t.encounter('RemadorCamara', (3, 0, -36), 'remador_camara')
t.exit('ParaAqueronte', (6.5, 1, -43.6), (3, 2, 1.2), 'aqueronte', 'ChegadaTenaro')
t.markers({
    'Entrada': (0, 0, 6),
    'Gruta': (0, 0, -5),
    'Poca': (0, 0, -12.5),
    'Camara': (2, 0, -30),
    'Trivia': (-3.5, 0, -30.5),
    'SaidaAqueronte': (6.5, 0, -40.5),
    # Dentro da passagem: teleportar para ele atravessa (conferência e debug).
    'AtravessarAqueronte': (6.5, 0, -43.6),
    # Em cima dos encontros: teleportar para eles começa a luta (se o encontro estiver disponível).
    'RemadorBoca': (0, 0, -3),
    'RemadorPoca': (-0.8, 0, -16),
    'RemadorMarcas': (0.5, 0, -24),
    'RemadorCamara': (3, 0, -36),
}, player=(0, 0, 6))
t.write('Tenaro.tscn')

# ---- Margem do Aqueronte ----
a = Scene('Aqueronte', '0.42, 0.46, 0.52, 1', '0.36, 0.42, 0.46, 1', 0.6)
a.box('ChaoMargem', (20, 1, 14), (0, -0.5, 0), 'Mat_piso')
a.box('Oeste', (1, 3, 15), (-10.5, 1.5, 0))
a.box('Leste', (1, 0.5, 15), (10.5, 0.25, 0))
a.box('SulA', (8.5, 0.5, 1), (-5.75, 0.25, 7.5))
a.box('SulB', (8.5, 0.5, 1), (5.75, 0.25, 7.5))
a.water('Rio', (40, 0.04, 14), (0, -0.3, -14))
a.blocker('RioBarreira', (22, 2, 1), (0, 1, -7.5))
a.box('Pier', (2, 0.3, 4), (4, -0.15, -9), 'Mat_terracota', collide=False)
a.box('Barca', (1.6, 0.5, 3), (6.2, -0.05, -10), 'Mat_terracota', collide=False)
a.column('PosteCaronte', (4, 0, -6.6), height=2.6, radius=0.15)
a.examinable('Margem', (-1, 0, -6), 'obj.aqueronte.margem')
a.examinable('BarcaOlhar', (4, 0, -5.8), 'obj.aqueronte.barca')
a.examinable('Insepultos', (-7, 0, -2), 'obj.aqueronte.insepultos')
a.light('LuzMargem', (0, 4, -2), '0.5, 0.55, 0.6, 1', 0.9, 14)
a.exit('ParaTenaro', (0, 1, 7.9), (3, 2, 1.2), 'tenaro', 'SaidaAqueronte')
a.markers({
    'ChegadaTenaro': (0, 0, 5),
    'Caronte': (4, 0, -4.5),
    'Insepultos': (-6, 0, -1),
    'AtravessarTenaro': (0, 0, 7.9),
}, player=(0, 0, 5))
a.write('Aqueronte.tscn')
print('ok')
