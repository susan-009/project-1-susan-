from pathlib import Path
import re
p=Path(r'D:\My project V1.0\Assets\Scenes\SampleScene.unity')
s=p.read_text(encoding='utf-8-sig')
# IDs for fire, wing, stone from scene
objects={'fire':1374424121,'wing':610879402,'stone':1027615234}
ids={'fire':2140000010,'wing':2140000011,'stone':2140000012}
for name,goid in objects.items():
    start=s.index(f'--- !u!1 &{goid}')
    end=s.find('--- !u!',start+8)
    if end<0:end=len(s)
    block=s[start:end]
    if str(ids[name]) not in block:
        block=block.replace('  m_Layer: 0\n',f'  - component: {{fileID: {ids[name]}}}\n  m_Layer: 0\n',1)
        s=s[:start]+block+s[end:]
meta=Path(r'D:\My project V1.0\Assets\SpiritLauncher.cs.meta').read_text()
guid=re.search(r'guid: (\w+)',meta).group(1)
blocks=''
for name,goid in objects.items():
    blocks+=f'''--- !u!114 &{ids[name]}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_GameObject: {{fileID: {goid}}}\n  m_Enabled: 1\n  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n  power: 5\n  launchGroundName: ground2\n  deployOnFirstClick: 1\n'''
pos=s.index('--- !u!1660057539')
s=s[:pos]+blocks+s[pos:]
p.write_text(s,encoding='utf-8')
print('attached launchers to fire, stone, wing')
