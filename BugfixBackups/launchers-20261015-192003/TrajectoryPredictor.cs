using UnityEngine;
public class TrajectoryPredictor : MonoBehaviour
{
 public LineRenderer line; public int points=30; public float timeStep=.1f;
 Color PreviewColor(){ GameObject fire=GameObject.Find("fire"); if(fire!=null){TrajectoryPredictor p=fire.GetComponent<TrajectoryPredictor>(); if(p!=null && p.line!=null)return p.line.startColor;} return Color.yellow; }
 public void EnsureLine(){ if(line==null){ GameObject o=new GameObject("TrajectoryLine"); o.transform.SetParent(transform,false); line=o.AddComponent<LineRenderer>(); line.startWidth=.08f; line.endWidth=.03f; line.positionCount=points; Color c=PreviewColor(); line.startColor=c; line.endColor=c; line.material=new Material(Shader.Find("Sprites/Default")); line.sortingOrder=100; line.enabled=false; } }
 public void Show(){EnsureLine();line.enabled=true;} public void Hide(){if(line!=null)line.enabled=false;}
 public void DrawTrajectory(Vector2 v){EnsureLine();Color c=PreviewColor();line.startColor=c;line.endColor=c;line.enabled=true;line.positionCount=points;Vector2 s=transform.position;for(int i=0;i<points;i++){float t=i*timeStep;line.SetPosition(i,s+v*t+.5f*Physics2D.gravity*t*t);}}
}
