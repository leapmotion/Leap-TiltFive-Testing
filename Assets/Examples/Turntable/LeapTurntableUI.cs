using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeapTurntableUI : MonoBehaviour
{
    public LeapTurntable _turntable;
    public LineRenderer _upperLine;
    public LineRenderer _lowerLine;
    // Start is called before the first frame update

    public int circleVertNum = 16;

    void RenderLines(){
        Vector3[] circlePointsUpper = new Vector3[circleVertNum];
        Vector3[] circlePointsLower = new Vector3[circleVertNum];

        for(int i = 0; i< circleVertNum; i++){
            circlePointsUpper[i] = new Vector3(Mathf.Sin(i*(360/circleVertNum)*Mathf.PI/180f),0,Mathf.Cos(i*(360/circleVertNum)*Mathf.PI/180f));
            circlePointsLower[i] = circlePointsUpper[i];

            circlePointsUpper[i] *= _turntable.tableRadius;
            circlePointsLower[i] *= _turntable.lowerLevelRadius;
        }

        _upperLine.transform.position = transform.position + (Vector3.up*_turntable.tableHeight);
        _lowerLine.transform.position = transform.position + (Vector3.up*_turntable.lowerLevelHeight);

        _upperLine.positionCount = circleVertNum;
        _lowerLine.positionCount = circleVertNum;

        _upperLine.SetPositions(circlePointsUpper);
        _lowerLine.SetPositions(circlePointsLower);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RenderLines();

        if(_turntable.fingertipCount > 0)
            GetComponent<Renderer>().material.color = Color.yellow;
        else
            GetComponent<Renderer>().material.color = Color.white;
    }
}
