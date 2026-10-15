from pathlib import Path
import re
p=Path(r'D:\My project V1.0\Assets\Scenes\SampleScene.unity')
s=p.read_text(encoding='utf-8-sig')
go=2136211728; cid=2140000030
start=s.index(f'--- !u!1 &{go}'); end=s.index('--- !u!',start+8); block=s[start:end]
if str(cid) not in block:
 block=block.replace('  - component: {fileID: 2136211729}\n','  - component: {fileID: 2136211729}\n  - component: {fileID: %d}\n'%cid)
 s=s[:start]+block+s[end:]
guid=re.search(r'guid: (\w+)',Path(r'D:\My project V1.0\Assets\WindmillTrigger.cs.meta').read_text()).group(1)
comp=f'''--- !u!114 &{cid}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_GameObject: {{fileID: {go}}}\n  m_Enabled: 1\n  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n  windName: wind\n  barrierName: Barrier\n'''
pos=s.index('--- !u!1660057539'); s=s[:pos]+comp+s[pos:]; p.write_text(s,encoding='utf-8')
print('WindmillTrigger attached')
