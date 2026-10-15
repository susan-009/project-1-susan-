using UnityEngine;

public class SpiritLauncher : MonoBehaviour
{
    Rigidbody rb; Rigidbody2D rb2D; TrajectoryPredictor predictor; Vector3 startPosition; bool aiming; bool deployed;
    public float power = 5f; public string launchGroundName = "ground2"; public bool deployOnFirstClick = true;

    void Awake(){ EnsureComponents(); }
    void EnsureComponents(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        if(GetComponent<Collider>() == null) gameObject.AddComponent<SphereCollider>();
        predictor = GetComponent<TrajectoryPredictor>();
        if(predictor == null) predictor = gameObject.AddComponent<TrajectoryPredictor>();
        if(predictor != null) predictor.EnsureLine();
    }
    void Start(){ EnsureComponents(); if(rb==null){ enabled=false; return; } rb.isKinematic=true; startPosition=transform.position; deployed=!deployOnFirstClick; if(predictor!=null) predictor.Hide(); }
    void OnMouseDown(){ if(!deployed){ DeployToLaunchGround(); return; } aiming=true; startPosition=transform.position; if(predictor!=null) predictor.Show(); }
    bool TryGetMouseWorld(out Vector3 world){ world=default; Camera cam=Camera.main; if(cam==null)return false; Ray ray=cam.ScreenPointToRay(Input.mousePosition); Plane plane=new Plane(Vector3.forward, startPosition); if(!plane.Raycast(ray,out float distance))return false; world=ray.GetPoint(distance); return true; }
    void Update(){ if(aiming && Input.GetMouseButton(0) && rb!=null && rb.isKinematic && TryGetMouseWorld(out var mouse) && predictor!=null) predictor.DrawTrajectory((Vector2)((startPosition-mouse)*power)); }
    void OnMouseUp(){ if(!aiming)return; aiming=false; if(!TryGetMouseWorld(out var mouse)||rb==null)return; Vector3 direction=startPosition-mouse; if(predictor!=null) predictor.Hide(); rb.isKinematic=false; rb.AddForce(direction*power,ForceMode.Impulse); }
    void DeployToLaunchGround(){ GameObject ground=GameObject.Find(launchGroundName); if(ground==null)return; transform.position=ground.transform.position; EnsureComponents(); if(rb==null)return; rb.isKinematic=true; rb.velocity=Vector3.zero; rb.angularVelocity=Vector3.zero; deployed=true; startPosition=transform.position; }
}

