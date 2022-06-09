using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;


public class Hand2DProjection : MonoBehaviour
{

    public LeapServiceProvider _provider;

    Vector2 _screenPoint;
    public Vector2 screenPoint {get{return _screenPoint;}}

    public UnityEngine.UI.Image _handCursor;


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
        _screenPoint = MapHandToScreen(_provider.Get(Chirality.Right));
        
        //Show hand on UI:
        if(_handCursor!=null){
            _handCursor.transform.localPosition = new Vector3(screenPoint.x-0.5f,
                                                            screenPoint.y-0.5f,
                                                            _handCursor.transform.localPosition.z);
        }
    }

    public Vector2 MapHandToScreen(Leap.Hand h){

        if(h==null) return -Vector2.one;

        Vector2 handPos   = h.PalmPosition.ToVector3();//h.WristPosition.ToVector3();

        return MapPointToScreen(handPos);
    }

    public Vector2 MapPointToScreen(Vector3 handPoint){

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
