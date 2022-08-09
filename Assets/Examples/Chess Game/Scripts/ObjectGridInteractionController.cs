using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class ObjectGridInteractionController : MonoBehaviour
{
    public ObjectGrid objectGrid;
    public LeapServiceProvider _provider;

    //public Chirality _dominantHand = Chirality.Right;
    public float maxDistance = 0; //used to determine max proximity of hand to gamepiece

    public Dictionary<string,GridLocation> board { get{return objectGrid.board;}}

    public Transform[] gridHighlight = new Transform[2];

    public float pinchThreshold = 0.5f; //0 is open hand, 1 is pinch from Leap.Hand.PinchStrength

//=============================
    bool[] isHoveringOverOccupier = { false, false };
    GameObject[] lastClosest = new GameObject[2]; //previous closest gamepiece hovering over
    GameObject[] grabbedPiece = new GameObject[2];
    bool[] stoppedGrab = { false, false };
    string[] coordStartGrab = { "A1", "A1" };

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

        //Leap.Hand h = _provider.Get(_dominantHand);
        Leap.Hand h = _provider.Get(Chirality.Left);
        CheckInteraction(h,0);
        h = _provider.Get(Chirality.Right);
        CheckInteraction(h,1);

    }

    void CheckInteraction(Leap.Hand h, int index)
    {
        if (h != null)
        {
            //Use hand position to highlight the nearest game piece
            GridLocation closestGrid = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition());
            GameObject gamepiece = objectGrid.GetClosestOccupier(h.GetPredictedPinchPosition(), maxDistance);
            if (!IsPinching(h))
            {
                DoHoverBehavior(gamepiece,index);
                gridHighlight[index].gameObject.SetActive(false);
            }

            //Move piece to nearest grid point:
            if (isHoveringOverOccupier[index] && IsPinching(h))
            {
                if (grabbedPiece[index] == null)
                {
                    stoppedGrab[index] = false;
                    grabbedPiece[index] = gamepiece;
                    coordStartGrab[index] = closestGrid.coordinate;
                }

                //make grabbed piece follow hand:
                Vector3 pinchPos = h.GetPredictedPinchPosition();
                grabbedPiece[index].transform.position = new Vector3(pinchPos.x, pinchPos.y - (0.625f * transform.lossyScale.x), pinchPos.z);//GetClosestGridLocation(h.GetPredictedPinchPosition());
                gridHighlight[index].localPosition = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition()).localPosition - (Vector3.up * 0.6f);
                gridHighlight[index].gameObject.SetActive(true);

            }
            else if (!h.IsPinching() && !stoppedGrab[index] && grabbedPiece[index] != null)
            {
                //drop the grabbed piece onto the nearest grid position, invoke delegate to determine game rules/what happens
                GridLocation closestLocation = objectGrid.GetClosestGridLocation(h.GetPredictedPinchPosition());
                onMoveObjectTo?.Invoke(closestLocation.coordinate, grabbedPiece[index]);
                //========
                //TODO: move this to ChessGameController.TryMovePiece() that is delegate to onMoveObjectTo. Make it check for chess rules before moving
                grabbedPiece[index].transform.position = objectGrid.transform.TransformPoint(closestLocation.localPosition);
                GameObject prevOccupier;
                objectGrid.SetGridOccupier(coordStartGrab[index], closestLocation.coordinate, grabbedPiece[index], out prevOccupier);
                //========
                grabbedPiece[index] = null;
                stoppedGrab[index] = true;
            }

            //Debug.Log(isHoveringOverPiece);

        }
        else
        { //no hands visible, disable outlines:
            if (lastClosest[index] != null)
            {
                lastClosest[index].SendMessage("ExitHover", SendMessageOptions.DontRequireReceiver);
                lastClosest[index] = null;
                isHoveringOverOccupier[index] = false;
            }
        }
    }

    void DoHoverBehavior(GameObject gridobject, int index){


        //Keeps track of what gameobject is being hovered over, informs the applicable gameobjects of their hover states: EnterHover and ExitHover
        if(gridobject==null){
            if(lastClosest[index] != null)
                lastClosest[index].SendMessage("ExitHover",SendMessageOptions.DontRequireReceiver);
                lastClosest[index] = null;
                isHoveringOverOccupier[index] = false;
            return;
        } 
        //else
        //    Debug.DrawLine(gridobject.transform.position,_provider.Get(Chirality.Right).GetPredictedPinchPosition(),Color.blue);


        if(gridobject != lastClosest[index])
        {
            //gridobject.GetComponent<Outline>().enabled = true;
            gridobject.SendMessage("EnterHover",SendMessageOptions.DontRequireReceiver);
            if(lastClosest[index] != null)
                lastClosest[index].SendMessage("ExitHover",SendMessageOptions.DontRequireReceiver);
            lastClosest[index] = gridobject;
            isHoveringOverOccupier[index] = true;
        }
    }
}
