from pathlib import Path
p=Path(r'D:\My project V1.0\Assets\Scenes\SampleScene.unity')
b=p.with_name('SampleScene.unity.preview-backup')
s=b.read_text(encoding='utf-8-sig')
marker='--- !u!1 &1027615234'
a=s.index(marker); end=s.index('--- !u!',a+8); block=s[a:end]
block=block.replace('  - component: {fileID: 1027615235}\n','  - component: {fileID: 1027615235}\n  - component: {fileID: 2140000002}\n')
s=s[:a]+block+s[end:]
insert='''--- !u!114 &2140000002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_GameObject: {fileID: 1027615234}
  m_Enabled: 1
  m_Script: {fileID: 11500000, guid: de33de2333de92c4b99c4cc05d12a893, type: 3}
  power: 5
  launchGroundName: ground2
  deployOnFirstClick: 1
'''
pos=s.index('--- !u!1660057539')
s=s[:pos]+insert+s[pos:]
p.write_text(s,encoding='utf-8')
print('scene repaired')
