# YangVillage(Godot) cat_professions.png → Unity Art/Cats 16장 + Addressables 등록 + CatData 연결
import hashlib, re
from PIL import Image
U = 'd:/workspace/_study/YangVil/Assets/'
src = Image.open('d:/workspace/YangVillage/game/assets/shared/cats/cat_professions.png').convert('RGBA')
W, H = src.size
coats = ['cheese', 'tuxedo', 'mackerel', 'calico']
jobs = ['sword', 'shield', 'bow', 'staff']
meta = open(U + 'Art/ThirdParty/CuteKitty/kitty_idle_01.png.meta', encoding='utf-8').read()
meta = meta.replace('isReadable: 1', 'isReadable: 0').replace('filterMode: 0', 'filterMode: 1')
guids = {}
for i in range(16):
    name = f'cat_{coats[i // 4]}_{jobs[i % 4]}'
    c, r = i % 4, i // 4
    img = src.crop((round(c * W / 4), round(r * H / 4), round((c + 1) * W / 4), round((r + 1) * H / 4)))
    bb = img.getbbox()
    if bb:  # 바닥 피벗: 아래 여백 제거 후 정사각 하단 정렬
        w = img.width
        img = img.crop((0, 0, w, bb[3]))
        sq = Image.new('RGBA', (w, w))
        sq.paste(img, (0, w - img.height))
        img = sq
    path = f'Art/Cats/{name}.png'
    img.save(U + path)
    g = hashlib.md5(('yangvil-' + name).encode()).hexdigest()
    guids[name] = g
    open(U + path + '.meta', 'w', encoding='utf-8', newline='\n').write(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + g, meta, count=1))

grp = U + 'AddressableAssetsData/AssetGroups/Characters.asset'
t = open(grp, encoding='utf-8').read()
add = ''.join(f"  - m_GUID: {g}\n    m_Address: Assets/Art/Cats/{n}.png\n    m_ReadOnly: 0\n    m_SerializedLabels: []\n    FlaggedDuringContentUpdateRestriction: 0\n"
              for n, g in guids.items() if g not in t)
i = t.index('  m_ReadOnly: 0\n  m_Settings')
open(grp, 'w', encoding='utf-8', newline='\n').write(t[:i] + add + t[i:])

# 원소별 대표 외형 (cat.tsv 원소별 다빈도 기준)
pick = {'sun': 'calico_shield', 'wind': 'cheese_shield', 'water': 'tuxedo_bow',
        'leaf': 'tuxedo_shield', 'fire': 'calico_bow', 'moon': 'mackerel_bow'}
ref = lambda g: f"    - m_AssetGUID: {g}\n      m_SubObjectName: \n      m_SubObjectType: \n      m_SubObjectGUID: \n      m_EditorAssetChanged: 0\n"
for el, n in pick.items():
    p = U + f'Data/Cats/cat_{el}.asset'
    t = open(p, encoding='utf-8').read()
    g = guids['cat_' + n]
    t = re.sub(r'  sprites:\n    idle:.*?    sheet:\n',
               lambda m: '  sprites:\n' + ''.join(f'    {k}:\n' + ref(g) for k in ['idle', 'walk', 'attack', 'hurt']) + '    sheet:\n',
               t, flags=re.S)
    t = re.sub(r'(    sheet:\n      m_AssetGUID:)[ \w]*\n', r'\1 \n', t)
    open(p, 'w', encoding='utf-8', newline='\n').write(t)
    print(el, n, g)
