using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class MoveObjectFromHandProjection : MonoBehaviour
{
    public LeapServiceProvider _provider;
    public Hand2DProjection _projectedHand;
    public Transform targetObject; //object to rotate
    public Transform targetObjectCamera; //the container holding the Main Camera

    public Transform virtualScreen;

    Quaternion startRotation = Quaternion.identity;
    Quaternion startRotation1 = Quaternion.identity;

    Vector2 startPoint = Vector2.zero;
    Vector3 startCameraPoint = Vector3.zero;

    float startDistance = 0; //distance between palms
    Vector3 startScale;

    bool isRotating = false;
    bool isTranslating = false;


    float minVelocityToLockon = 0.2f; //start tracking hand when velocity dips below this threshold

    public float maxRotation = 90; //in either direction
    // Start is called before the first frame update

    void Start()
    {
        //startScale = virtualScreen.localScale;
        minVelocityToLockon *= _provider.transform.lossyScale.x;
    }

    // Update is called once per frame
    void Update()
    {

        Leap.Hand h  = _provider.Get(Chirality.Right);
        Leap.Hand h2 = _provider.Get(Chirality.Left);
        
        
            
        if(h!=null && h2!=null){

            if(CheckHandsFacingEachOther(h,h2)){
                //If both hands active and facing each other,
                // move model, scale window:
                TwoHandTransform(h,h2);
            } else{
                OneHandRotate(h);
            }

        }else if(h!=null){
            OneHandRotate(h);
        }

    }

    bool CheckHandsFacingEachOther(Leap.Hand hand1, Leap.Hand hand2){

        float directioness = Vector3.Dot(hand1.PalmNormal.ToVector3(),-hand2.PalmNormal.ToVector3());
        float horizontalness = Mathf.Abs(Vector3.Dot(hand1.PalmNormal.ToVector3(),_provider.transform.right));

        bool makingFist = (hand1.GrabStrength > 0.6f || hand2.GrabStrength > 0.6f);
        //Debug.Log(directioness);
        if(!makingFist && directioness > .8f && horizontalness > 0.8f) return true;

        return false;
    }

    void TwoHandTransform(Leap.Hand hand1, Leap.Hand hand2){

        //shift camera relative to panel, and scale panel to zoom in/out

        Vector3 middlePoint    = (hand1.PalmPosition.ToVector3() + hand2.PalmPosition.ToVector3())/2f;
        Vector2 projectedPoint = _projectedHand.MapPointToScreen(middlePoint);

        if(!isTranslating){
            isTranslating = true;
            startPoint = projectedPoint;
            startCameraPoint = targetObjectCamera.localPosition;//Camera.main.transform.localPosition;
            startDistance = Vector3.Distance(hand1.WristPosition.ToVector3(),hand2.WristPosition.ToVector3());
            //startScale = virtualScreen.localScale;
        }

        float scaleFromMeter = _provider.transform.localScale.x;

        float horzRange = .3f*scaleFromMeter;
        float vertRange = .2f*scaleFromMeter;

        //translate by the delta:
        float horzMove = ((projectedPoint.x - 0.5f) - (startPoint.x - 0.5f )) * 2 * horzRange;
        float vertMove = ((projectedPoint.y - 0.5f) - (startPoint.y - 0.5f )) * 2 * vertRange;

        //Camera.main.transform.localPosition = startCameraPoint + new Vector3(horzMove, vertMove, 0);
        targetObjectCamera.localPosition = startCameraPoint + new Vector3(horzMove, 0 , vertMove);

        //scale to move in-out
        float minScale = 0.2f;
        float maxScale = 2;
        Vector3 hand1Pos = hand1.WristPosition.ToVector3();
        Vector3 hand2Pos = hand2.WristPosition.ToVector3();
        float scaleBy = Mathf.Clamp(
                                    Vector3.Distance(hand1Pos,hand2Pos) / startDistance,
                                    minScale,
                                    maxScale);
        float invertScaleBy = (1-((scaleBy - minScale) / (maxScale - minScale))) * (maxScale - minScale);// + minScale;
        //virtualScreen.localScale = startScale * Mathf.Clamp(invertScaleBy,minScale,maxScale);

    }

    void OneHandRotate(Leap.Hand h){
        isTranslating = false;
        bool handPalmSideways = h==null ? false : Vector3.Dot(h.PalmNormal.ToVector3(), Vector3.right) > .8f;

        if(h.GrabStrength > 0.6f || handPalmSideways){
                isRotating = false;
                return;
            } 

            if(!isRotating && h.PalmVelocity.Magnitude < minVelocityToLockon && h.TimeVisible > 0.25f){
                isRotating = true;
                startRotation  = targetObject.rotation;
                startRotation1 = targetObjectCamera.rotation;
                startPoint = _projectedHand.screenPoint;
            }

            if(isRotating){
                //Yaw is delta hand moved in the [0,1] range, converted to a -90 to 90 degree range
                float yaw   = ((_projectedHand.screenPoint.x - 0.5f) - (startPoint.x - 0.5f )) * 2 * maxRotation;
                float pitch = ((_projectedHand.screenPoint.y - 0.5f) - (startPoint.y - 0.5f )) * 2 * maxRotation/2;
                targetObject.rotation       = Quaternion.Euler(0,    -yaw,  0) * startRotation;
                //targetObjectCamera.rotation = Quaternion.Euler(-pitch, 0  ,  0) * startRotation1;
            }
    }
}
