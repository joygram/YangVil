# YangVillage(Godot) 정본 테이블·그림 → Unity Resources/YangVillage 동기화
# - 테이블: game/data/tables/*.tsv → Tables/<name>.txt (Unity TextAsset)
# - 그림: sprite.tsv 의 res:// 경로 → 같은 상대 경로
import hashlib, os, re, shutil
G = 'd:/workspace/YangVillage/game/'
R = 'd:/workspace/_study/YangVil/Assets/Resources/YangVillage/'
os.makedirs(R + 'Tables', exist_ok=True)
for f in os.listdir(G + 'data/tables'):
    if f.endswith('.tsv'):
        shutil.copyfile(G + 'data/tables/' + f, R + 'Tables/' + f[:-4] + '.txt')
paths = set()
for line in open(G + 'data/tables/sprite.tsv', encoding='utf-8').read().splitlines()[1:]:
    if line:
        paths.add(line.split('\t')[1].replace('res://', ''))
meta = open('d:/workspace/_study/YangVil/Assets/Art/ThirdParty/CuteKitty/kitty_idle_01.png.meta', encoding='utf-8').read()
meta = meta.replace('isReadable: 1', 'isReadable: 0').replace('filterMode: 0', 'filterMode: 1')
for p in sorted(paths):
    os.makedirs(os.path.dirname(R + p), exist_ok=True)
    shutil.copyfile(G + p, R + p)
    g = hashlib.md5(('yangvillage-res-' + p).encode()).hexdigest()
    open(R + p + '.meta', 'w', encoding='utf-8', newline='\n').write(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + g, meta, count=1))

def guid(key):
    return hashlib.md5(('yangvillage-res-' + key).encode()).hexdigest()

def write_meta(path, body):
    if not os.path.exists(path + '.meta'):
        open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(f'fileFormatVersion: 2\nguid: {guid(path)}\n' + body)

for f in os.listdir(R + 'Tables'):
    if f.endswith('.txt'):
        write_meta(R + 'Tables/' + f, 'TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
folder = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
for d, _, _ in os.walk(R):
    write_meta(d.rstrip('/\\').replace('\\', '/'), folder)
print(len(os.listdir(R + 'Tables')), 'tables,', len(paths), 'images')
