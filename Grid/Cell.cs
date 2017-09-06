using UnityEngine;

namespace FullSailTactics
{
    /// <summary>
    /// Clase que representa una celda en el tablero
    /// </summary>
    public class Cell : MonoBehaviour
    {
        public bool IsEmpty { get; set; }
        public bool IsSelectable { get; set; }
        public Ship ShipCell { get; set; }
        public PositionGrid GridPosition { get; set; }
        public Vector3 WorldPosition { get; set; }
        public MeshRenderer Renderer_Player_1 { get; set; }
        public MeshRenderer Renderer_Player_2 { get; set; }

        /// <summary>
        /// Metodo para asociar un barco a una celda. Indica tambien si esta llena la casilla
        /// </summary>
        /// <param name="ship"></param>
        public void SetShipCell(Ship ship)
        {
            this.ShipCell = ship;
            this.IsEmpty = (ship) ? false : true;
        }

        /// <summary>
        /// Metodo para devolver el mesh renderer de la celda dependiendo del jugador
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public MeshRenderer GetCellMesh(PlayerManager player)
        {
            return (player.name == "Player_1") ? this.Renderer_Player_1 : this.Renderer_Player_2;
        }
    }
}