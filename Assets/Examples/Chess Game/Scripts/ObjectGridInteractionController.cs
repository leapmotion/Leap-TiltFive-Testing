using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class ObjectGridInteractionController : MonoBehaviour
{
    public ObjectGrid objectGrid;
    public LeapServiceProvider _provider;

    public Chirality _dominantHand = Chirality.Right;
    public float maxDistance = 0; //used to determine max proximity of hand to gamepiece

    public Dictionary<string,GridLocation> board { get{return objectGrid.board;}}

    public Transform gridHighlight;

    public float pinchThreshold = 0.5f; //0 is open hand, 1 is pinch from Leap.Hand.PinchStrength

//=============================
    bool isHoveringOverOccupier = false;
    GameObject lastClosest; //previous closest gamepiece hovering over
    GameObject grabbedPiece;
    bool stoppedGrab = false;
    string coordStartGrab = "A1";

//==============================

    public delegate void OnMoveObject(string toCoord, GameObject go);
    public OnMoveObject onMoveObjectTo;

//==============================

    private bool IsPinching(Leap.Hand h){
        return h.PinchStrength >= pinchThreshold;
    }

    // Update is called once per frame
    void Update()
    {

        //TODO: allow either hand

        Leap.Hand h = _provider.Get(_dominantHand);

        if(h != null){
            //Use hand position to highlight the nearest game piece
            GridLocation closestGrid = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition());
            GameObject gamepiece = objectGrid.GetClosestOccupier(h.GetPredictedPinchPosition(), maxDistance);
            if(!IsPinching(h)){
                DoHoverBehavior(gamepiece);
                gridHighlight.gameObject.SetActive(false);
            }
            
            //Move piece to nearest grid point:
            if(isHoveringOverOccupier && IsPinching(h)){ 
                if(grabbedPiece == null){
                    stoppedGrab = false;
                    grabbedPiece = gamepiece;
                    coordStartGrab = closestGrid.coordinate;         
                }
                
                //make grabbed piece follow hand:
                Vector3 pinchPos = h.GetPredictedPinchPosition();
                grabbedPiece.transform.position = new Vector3(pinchPos.x,pinchPos.y - (0.625f * transform.lossyScale.x),pinchPos.z);//GetClosestGridLocation(h.GetPredictedPinchPosition());
                gridHighlight.localPosition = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition()).localPosition - (Vector3.up*0.6f);
                gridHighlight.gameObject.SetActive(true);

            } else if(!h.IsPinching() && !stoppedGrab && grabbedPiece != null){
                //drop the grabbed piece onto the nearest grid position, invoke delegate to determine game rules/what happens
                GridLocation closestLocation = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition());
                onMoveObjectTo?.Invoke(closestLocation.coordinate,grabbedPiece);
                //========
                //TODO: move this to ChessGameController.TryMovePiece() that is delegate to onMoveObjectTo. Make it check for chess rules before moving
                grabbedPiece.transform.position = objectGrid.transform.TransformPoint(closestLocation.localPosition);
                GameObject prevOccupier;
                objectGrid.SetGridOccupier(coordStartGrab, closestLocation.coordinate, grabbedPiece, out prevOccupier);
                //========
                grabbedPiece = null;
                stoppedGrab = true;
            }

            //Debug.Log(isHoveringOverPiece);

        } else { //no hands visible, disable outlines:
            if(lastClosest != null){
                lastClosest.SendMessage("ExitHover",SendMessageOptions.DontRequireReceiver);
                lastClosest = null;
                isHoveringOverOccupier = false;
            }
        }
    }

    void DoHoverBehavior(GameObject gridobject){


        //Keeps track of what gameobject is being hovered over, informs the applicable gameobjects of their hover states: EnterHover and ExitHover
        if(gridobject==null){
            if(lastClosest != null)
                lastClosest.SendMessage("ExitHover",SendMessageOptions.DontRequireReceiver);
                lastClosest = null;
                isHoveringOverOccupier = false;
            return;
        } else
            Debug.DrawLine(gridobject.transform.position,_provider.Get(Chirality.Right).GetPredictedPinchPosition(),Color.blue);


        if(gridobject != lastClosest){
            //gridobject.GetComponent<Outline>().enabled = true;
            gridobject.SendMessage("EnterHover",SendMessageOptions.DontRequireReceiver);
            if(lastClosest != null)
                lastClosest.SendMessage("ExitHover",SendMessageOptions.DontRequireReceiver);
            lastClosest = gridobject;
            isHoveringOverOccupier = true;
        }
    }
}
