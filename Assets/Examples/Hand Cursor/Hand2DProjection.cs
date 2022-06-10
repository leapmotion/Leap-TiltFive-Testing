using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;


public class Hand2DProjection : MonoBehaviour
{

    public LeapServiceProvider _provider;

    Vector2 _screenPoint;
    public Vector2 screenPoint {get{return _screenPoint;}}

    RaycastHit _raycastResult;
    public RaycastHit raycastResult {get{return _raycastResult;}}

    public UnityEngine.UI.Image _handCursor;
    public UnityEngine.UI.Image _handCursor2;
    public Transform _shoulderRight;
    public LayerMask layerMask; //raycast against

    public UnityEngine.UI.Text _pinchText;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnDrawGizmos() {
     //   if(!Application.IsPlaying(transform.gameObject)) return;

        
    }

    // Update is called once per frame
    void Update()
    {
        Leap.Hand h = _provider.Get(Chirality.Right);
        if(h==null) return;

        _screenPoint  = MapHandToScreen(h);
        _raycastResult = RaycastHandToWorld(h);
        
        //Show hand on UI:
        if(_handCursor!=null){
            _handCursor.transform.localPosition = new Vector3(screenPoint.x-0.5f,
                                                            screenPoint.y-0.5f,
                                                            _handCursor.transform.localPosition.z);
        }

        if(_handCursor2!=null){
            _handCursor2.transform.position = _raycastResult.point + _raycastResult.normal*0.001f;
        }

        if(_pinchText!=null){
            _pinchText.text = h.PinchStrength.ToString("F2");
        }
    }

    public RaycastHit RaycastHandToWorld(Leap.Hand h){

        //Use a virtual shoulder estimated from the HMD pose to determine where in virtual world user is pointing

        RaycastHit hit;
        //use the knuckle so that the raycast point is more stable to hand pose, especially during pinch
        Vector3 indexKnuckle = h.GetIndex().bones[0].NextJoint.ToVector3();
        Vector3 direction = (indexKnuckle - _shoulderRight.position).normalized;
        float maxDistance = 100;
        if (Physics.Raycast(_shoulderRight.position,direction, out hit, maxDistance,layerMask)){
            Debug.DrawLine(_shoulderRight.position,hit.point,Color.green);
        } else {
            Debug.DrawLine(_shoulderRight.position,_shoulderRight.position + (direction*maxDistance),Color.red);
        }
        return hit;
    }

    public Vector2 MapHandToScreen(Leap.Hand h){

        if(h==null) return -Vector2.one;

        Vector2 handPos   = h.PalmPosition.ToVector3();//h.WristPosition.ToVector3();

        return MapPointToScreen(handPos);
    }

    public Vector2 MapPointToScreen(Vector3 handPoint){

        //Defines an interaction box by width and height around the hand tracking device, and normalized the position to Vector2 in screenspace, clamped 0-1
        //This provides the most "resolution" between real hand and virtual units.

        Vector2 screenPos = Vector2.zero;

        float scale = transform.localScale.x;
        float interactionWidth  = 0.2f*scale; //half the horizontal interaction volume in meters. It's comfortable range when sitting at desk
        float interactionStartHeight = .1f*scale; //height above device tracking starts
        float interactionEndHeight = .3f*scale; //max height above device tracked

        //TUNED FOR LEFT HAND and SIR170 volume:
        screenPos.x = Mathf.Clamp(Remap(handPoint.x, -interactionWidth,      interactionWidth,     0, Screen.width ), 0 , Screen.width ) / Screen.width;
        screenPos.y = Mathf.Clamp(Remap(handPoint.y, interactionStartHeight, interactionEndHeight, 0, Screen.height), 0 , Screen.height) / Screen.height;

        return screenPos;

    }

    public static float Remap (float from, float fromMin, float fromMax, float toMin,  float toMax)
    {
        var fromAbs  =  from - fromMin;
        var fromMaxAbs = fromMax - fromMin;      
       
        var normal = fromAbs / fromMaxAbs;
 
        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;
 
        var to = toAbs + toMin;
       
        return to;
    }
}
