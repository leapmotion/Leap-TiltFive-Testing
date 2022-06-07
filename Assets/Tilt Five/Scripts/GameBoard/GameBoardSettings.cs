using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltFive
{
    [System.Serializable]
    public class GameBoardSettings
    {
        /// <summary>
        /// The game board is the window into the game world, as well as the origin about which the glasses/wand are tracked.
        /// </summary>
        /// <remarks>
        /// It can be useful to modify the game board's location for cinematic purposes,
        /// such as following an object (such as a player's avatar) in the scene.
        /// This avoids the need to directly modify the positions/orientations of the glasses or wand,
        /// which track the player's movements relative to the game board.
        /// </remarks>
        public GameBoard currentGameBoard;

        /// <summary>
        /// Overrides which gameboard variant is displayed via the gameboard gizmo in the Unity Editor.
        /// If GameboardType_None is selected, no override is applied, and the gizmo will update automatically
        /// according to the gameboard type detected by the glasses.
        /// </summary>
        /// <remarks>This option can be useful for mocking up how the scene or UI will look when different gameboard variants are used.</remarks>
        public GameboardType gameboardTypeOverride = GameboardType.GameboardType_None;

        /// <summary>
        /// The game board's scale multiplies the perceived size of objects in the scene.
        /// </summary>
        /// <remarks>
        /// When scaling the world to fit on the game board, it can be useful to think in terms of zoom (e.g. 2x, 10x, etc) rather than fussing with absolute units using <see cref="contentScaleRatio"> and <see cref="contentScaleUnit">.
        /// Directly modifying the game board's scale is convenient for cinematics, tweening/animation, and other use cases in which zooming in/out may be desirable.
        /// </remarks>
        public float gameBoardScale => currentGameBoard != null ? currentGameBoard.localScale : 1f;

        /// <summary>
        /// The game board position or focal position offset.
        /// </summary>
        public Vector3 gameBoardCenter => currentGameBoard != null ? currentGameBoard.position : Vector3.zero;

        /// <summary>
        /// The game board rotation or focal rotational offset.
        /// </summary>
        public Vector3 gameBoardRotation => currentGameBoard != null ? currentGameBoard.rotation.eulerAngles : Vector3.zero;

        /// <summary>
        /// The gameboard configuration, such as LE, XE, or folded XE.
        /// </summary>
        public GameboardType gameboardType => currentGameBoard != null ? currentGameBoard.GameboardType : GameboardType.GameboardType_None;
    }
}
