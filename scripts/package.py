"""Public source allowlist and project-bound portable packaging. No installation/private scans."""
import pathlib,json,zipfile,hashlib,re
from release_privacy import scan
root=pathlib.Path(__file__).resolve().parent.parent
assert root.name=='wispr-scientific'
public=json.loads((root/'public-files.json').read_text())['files']
if len(public)!=len(set(public)):raise RuntimeError('Duplicate public path')
for relative in public:
 p=root/relative
 if not p.is_file() or p.is_symlink() or p.is_junction() or not p.resolve().is_relative_to(root) or p.name.endswith('.private.json'):raise RuntimeError('Invalid public source input: '+relative)
 if p.suffix in {'.cs','.csproj','.json','.md','.html','.ps1','.py','.cjs','.txt'}:
  text=p.read_bytes().lower()
  if re.search(rb'[a-z]:[/\\]+users[/\\]+',text):raise RuntimeError('Local user profile path in public text: '+relative)
out=root/'releases';out.mkdir(exist_ok=True)
folder=out/'Scientific-Dictation-v0.1.1-win-x64'
if not (folder/'scientific.exe').is_file() or not (folder/'scientific-cli.exe').is_file():raise RuntimeError('Publish both executables first')
privacy=scan(folder,root)
if privacy['matches']:raise RuntimeError('Private build paths remain: '+str(privacy['matches']))
assets=[]
target=out/'Scientific-Dictation-v0.1.1-win-x64.zip'
if target.exists():raise RuntimeError('Refusing to overwrite existing release archive: '+target.name)
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(folder.rglob('*')):
  if p.is_symlink() or p.is_junction() or not p.resolve().is_relative_to(folder):raise RuntimeError('Invalid binary input')
  if p.is_file() and p.suffix!='.pdb':z.write(p,str(pathlib.Path(folder.name)/p.relative_to(folder)))
assets.append(target)
target=out/'Scientific-Dictation-v0.1.1-source.zip'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for relative in public:z.write(root/relative,relative)
assets.append(target)
manifest=[{'file':p.name,'bytes':p.stat().st_size,'sha256':hashlib.file_digest(p.open('rb'),'sha256').hexdigest()} for p in assets]
(out/'release-assets-v0.1.1.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'SHA256SUMS-v0.1.1.txt').write_text(''.join(f"{p['sha256']}  {p['file']}\n" for p in manifest),encoding='ascii')
print(json.dumps(manifest,indent=2))
