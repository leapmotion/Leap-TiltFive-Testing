using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGrid : MonoBehaviour
{

    public Dictionary<string, GridLocation> board;

    [Range(0, 26)] //TODO: allow more than alphabet
    public int gridSize = 8; //square dimension, chess board is 8x8
    public float boardWidth = 10; //in world units, i.e. total width of the gameboard

    public bool Debug_ShowGridCenters = true;

    List<string> _rowIdentifiers;
    public List<string> rowIdentifiers { get{return _rowIdentifiers;}}

    // Start
    private void OnEnable() {
        board = CreateGridDictionary(gridSize);
        Debug.Log("Created new board of " + board.Count +" grid positions, shaped as an " +gridSize+"x"+gridSize +" grid."); 
    }

    private void OnDrawGizmos() {
        if(!Application.isPlaying || !Debug_ShowGridCenters) return;
        //List<Vector3> gridLocations = GetGridLocations();
        foreach(KeyValuePair<string,GridLocation> g in board){
            Gizmos.DrawSphere( transform.TransformPoint(g.Value.localPosition),.2f*transform.lossyScale.x);
        }
    }

    Dictionary<string,GridLocation> CreateGridDictionary(int squareDimension){

        Dictionary<string,GridLocation> grid = new Dictionary<string, GridLocation>();
        _rowIdentifiers = GenerateRowIdentifiers(gridSize); //List of letters;
        float gridWidth = boardWidth / squareDimension; //1 grid square width
        Vector3 originCenter = Vector3.one * gridWidth / 2f;
        //Debug.Log("Grid Square width: " + gridWidth);

        for(int i = 0; i < squareDimension; i++){
            for(int j = 0; j < squareDimension; j++){
                GridLocation newLocation = new GridLocation();
                newLocation.coordinate = _rowIdentifiers[j]+(i+1);
                newLocation.occupier = null;
                newLocation.localPosition = originCenter + new Vector3(j*gridWidth,0,i*gridWidth);

                grid.Add(newLocation.coordinate,newLocation);
            }
        }

        return grid;
    }

    public bool SetGridOccupier(string fromcoord, string tocoord, GameObject newOccupier, out GameObject prevOccupier){
        //sets a new reference to gameobject occupying space, returns true and ref to previous occuping gameobject if there was already an occupier
        prevOccupier = board[tocoord].occupier;
        bool isAlreadyOccupied = prevOccupier != null;

        //update the old position: 
        board[fromcoord].occupier = null;
        
        board[tocoord].occupier = newOccupier;

        return isAlreadyOccupied; 
    }

    public GameObject GetClosestOccupier(Vector3 point, float maxDistance){
        //Returns nearest gameobject occupying a grid location within the maxDistance to the point given in world space
        float nearestDistance = 10000000000;
        GameObject nearestPiece = null;
        foreach(KeyValuePair<string,GridLocation> gridposition in board)
        {
            //Now you can access the key and value both separately from this piece as:
            //Debug.Log(piece.Key);
            //Debug.Log(piece.Value);

            if(gridposition.Value.occupier != null){ //ignore if grid location is empty, else:
                Vector3 worldOccupierPosition = transform.TransformPoint(gridposition.Value.localPosition);//piece.Value.occupier.transform.position;
                float dist = Vector3.Distance(worldOccupierPosition, point);
                if(dist < nearestDistance){
                    nearestPiece = gridposition.Value.occupier;
                    nearestDistance = dist;
                }

                // if(dist<=maxDistance){
                //     Debug.DrawLine(point, worldOccupierPosition,Color.green);
                // }else
                //     Debug.DrawLine(point, worldOccupierPosition,Color.red);

            }
        }

        if(nearestDistance <= maxDistance)
            return nearestPiece;
        else
            return null;
    }

    public Vector3 GetClosestGridPosition(Vector3 point){
        //Returns world space point of the closest grid location center
        Vector3 closestGridPoint = Vector3.zero;
        float closestDistance = 10000000000;
        foreach(KeyValuePair<string,GridLocation> gridloc in board){
            Vector3 worldGridCenterPos = transform.TransformPoint(gridloc.Value.localPosition);
            float d = Vector3.Distance(worldGridCenterPos,point);
            if(d < closestDistance){
                closestDistance = d;
                closestGridPoint = worldGridCenterPos;
            }
        }

        return closestGridPoint;
    }

    public GridLocation GetClosestGridLocation(Vector3 point){
        GridLocation closestGridLoc = board["A1"];
        float closestDistance = 10000000000;
        foreach(KeyValuePair<string,GridLocation> gridloc in board){
            Vector3 worldGridCenterPos = transform.TransformPoint(gridloc.Value.localPosition);
            float d = Vector3.Distance(worldGridCenterPos,point);
            if(d < closestDistance){
                closestDistance = d;
                closestGridLoc = gridloc.Value;
            }
        }

        return closestGridLoc;
    }


    private List<string> GenerateRowIdentifiers(int num){
        //Returns a subset of the alphabet as uppercase strings
        //TODO: generate more than 26 letters for larger grids:

        List<string> ids = new List<string>();
        //Loop through the ASCII characters 65 to 90
        int count = 0;
        for (int i = 65; i <= 90; i++){
            if(count == num) break;
            //Convert the int to a char to get the actual character behind the ASCII code
            ids.Add(((char)i).ToString());
            count++;
        }

        return ids;
    }
}

public class GridLocation {
    public string coordinate;
    public Vector3 localPosition; //relative positioned to parent center
    public GameObject occupier; //reference to the gameobject occupying space
}
