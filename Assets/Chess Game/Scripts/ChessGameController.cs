using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessGameController : MonoBehaviour
{
    public ObjectGridInteractionController _boardController;
    public Transform gamepieceContainer;

    // Start is called before the first frame update
    void OnEnable()
    {
        SetupGame();
        _boardController.onMoveObjectTo += TryMovePiece;
    }

    void SetupGame(){
        GameObject prevOccupier;
        //Assign gamepieces to board locations based on their order under gamepieceContainer;

        List<string> alphabet = _boardController.objectGrid.rowIdentifiers;
        int squareDimension = _boardController.objectGrid.gridSize;

        int pieceCount = 0;

        for(int i = 0; i < squareDimension; i++){
            for(int j = 0; j < squareDimension; j++){
                if(i < 2 || i > 5){
                    int row = i < 2 ? i : i - 6;
                    int childIndex            = j + ((row) % 2 * squareDimension) + (i < 2 ? 0 : 16);
                    string pieceCoordinate      = alphabet[j]+(i+1);
                    GameObject gamePiece      = gamepieceContainer.GetChild(childIndex).gameObject;
                    //Debug.Log(pieceCoordinate + " " +gamePiece.name);
                    _boardController.objectGrid.SetGridOccupier(pieceCoordinate,pieceCoordinate, gamePiece, out prevOccupier);
                    gamePiece.SendMessage("ExitHover", SendMessageOptions.DontRequireReceiver); //set no outline
                    pieceCount++;
                    //reset each position too so it works when resetting a game
                    gamePiece.transform.position = _boardController.objectGrid.transform.TransformPoint( _boardController.board[pieceCoordinate].localPosition);
                }
            }
        }

        // foreach(KeyValuePair<string,GridLocation> piece in _boardController.board)
        // {
        //     if(piece.Value.occupier != null)
        //         Debug.Log(piece.Key + " " +piece.Value.occupier.name);
        // }
        Debug.Log("Finished setting up "+ pieceCount +"chess game pieces.");
    }

    void TryMovePiece(string coord, GameObject go){

        //TODO: implement chess rules here
        Debug.Log("Trying " + go.name +" to: " + coord +"...");

    }
}
