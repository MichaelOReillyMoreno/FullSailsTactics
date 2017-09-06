using UnityEngine;
using System.Collections.Generic;

namespace FullSailTactics
{
    /// <summary>
    /// Enumerado que indica las direcciones posibles de un movimiento
    /// </summary>
    public enum DIRECTIONS { UP, RIGHT, DOWN, LEFT };

    /// <summary>
    /// Clase que representa una posicion en el tablero
    /// </summary>
    [System.Serializable]
    public class PositionGrid
    {
        public int X;
        public int Y;
        public int Cost;

        public PositionGrid(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public PositionGrid(int x, int y, int cost)
        {
            this.X = x;
            this.Y = y;
            this.Cost = cost;
        }

        public override string ToString()
        {
            return "X: " + X + " Y: " + Y;
        }

        public static PositionGrid operator +(PositionGrid pos1, PositionGrid pos2)
        {
            return new PositionGrid(pos1.X + pos2.X, pos1.Y + pos2.Y);
        }

        public static PositionGrid operator -(PositionGrid pos1, PositionGrid pos2)
        {
            return new PositionGrid(pos1.X - pos2.X, pos1.Y - pos2.Y);
        }

        /// <summary>
        /// Metodo para invertir las coordenadas de la posicion del tablero
        /// </summary>
        /// <param name="flip"></param>
        /// <returns></returns>
        public PositionGrid Flip (bool flip)
        {
            if (flip)
                return new PositionGrid(this.Y, this.X);
            else
                return this;
        }
    }

    /// <summary>
    /// Clase que representa y gestiona el tablero del juego
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        //Materiales de las celdas
        public Material EmptyCellMaterial;
        public Material PathMaterial;
        public Material BlockedMaterial;
        public Material AttackMaterial;

        public GameObject AttackBoatParticle, AttackWaterParticle;

        //Tablero, filas, columnas
        public int ROWS { get; set; }
        public int COLS { get; set; }
        private Cell[,] grid;

        //Instancia
        public static GridManager Instance = null;

        void Awake()
        {
            Instance = this;

            if(!PlayerPrefs.HasKey("xCells") || !PlayerPrefs.HasKey("yCells"))
            {
                PlayerPrefs.SetInt("xCells", 17);
                PlayerPrefs.SetInt("yCells", 17);
            }

            ROWS = PlayerPrefs.GetInt("xCells");
            COLS = PlayerPrefs.GetInt("yCells");

            this.grid = new Cell[ROWS, COLS];

            //Asociacion del tablero creado al GridManager y configuracion de las celdas
            GameObject _grid = GameObject.Find("Grid");            
            for (int i = 0; i < ROWS; i++)
            {
                for (int j = 0; j < COLS; j++)
                {
                    Transform child = _grid.transform.GetChild(ROWS * i + j);
                    this.grid[i, j] = child.GetComponent<Cell>();
                    this.grid[i, j].GridPosition = new PositionGrid(i, j);
                    this.grid[i, j].WorldPosition = child.transform.position;
                    if(child.name == "CellNode(Clone)")
                    {
                        this.grid[i, j].Renderer_Player_1 = child.GetChild(0).GetComponent<MeshRenderer>();
                        this.grid[i, j].Renderer_Player_2 = child.GetChild(1).GetComponent<MeshRenderer>();
                    }                    
                    this.grid[i, j].IsEmpty = true;
                    this.grid[i, j].IsSelectable = false;
                }
            }
        }

        /// <summary>
        /// Metodo que devuelve una celda del tablero segun la posicion indicada
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Cell GetCell(PositionGrid pos)
        {
            return this.grid[pos.X, pos.Y];
        }

        /// <summary>
        /// Metodo que indica si la celda esta vacia
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsCellEmpty(PositionGrid pos)
        {
            return this.grid[pos.X, pos.Y].IsEmpty;
        }

        /// <summary>
        /// Metodo que indica si la celda es valida para un movimiento
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsCellValid(PositionGrid pos)
        {
            return pos.X > 0 && pos.X < this.ROWS - 1 && pos.Y > 0 && pos.Y < this.COLS - 1;
        }

        /// <summary>
        /// Metodo para pintar las celdas de accion seleccionadas con el material indicado
        /// </summary>
        /// <param name="player"></param>
        /// <param name="phase"></param>
        /// <param name="coloredPositions"></param>
        /// <param name="mat"></param>
        public void PaintCells(PlayerManager player, GameManager.PHASE phase, List<PositionGrid> coloredPositions)
        {
            //Asocio las celdas coloreadas al jugador
            player.ColoredPositions = coloredPositions;
            //Pinto las celdas del jugador
            foreach (PositionGrid pos in player.ColoredPositions)
            {                
                if(phase == GameManager.PHASE.MOVEMENT)
                {
                    if (this.GetCell(pos).IsEmpty) //Si la celda esta vacia
                    {
                        if(player.ActualSkillPoints >= pos.Cost) //Si tengo suficientes puntos
                        {
                            this.GetCell(pos).GetCellMesh(player).material = this.PathMaterial;
                            this.GetCell(pos).IsSelectable = true;
                        }
                        else
                        {
                            this.GetCell(pos).GetCellMesh(player).material = this.BlockedMaterial;
                            this.GetCell(pos).IsSelectable = false;
                        }                   
                    }
                    else //Si la celda esta ocupada, la pinto con el material BlockedMaterial
                    {
                        this.GetCell(pos).GetCellMesh(player).material = this.BlockedMaterial;
                        this.GetCell(pos).IsSelectable = false;
                    }

                    this.GetCell(pos).GridPosition.Cost = pos.Cost; //////////////////////////////////
                }
                else if (phase == GameManager.PHASE.ACTION)
                    this.GetCell(pos).GetCellMesh(player).material = this.AttackMaterial;
            }                
        }

        /// <summary>
        /// Metodo para resetear las celdas que se han coloreado previamente
        /// </summary>
        public void ClearColoredCells(PlayerManager player)
        {         
            foreach (PositionGrid pos in player.ColoredPositions)
            {
                this.GetCell(pos).GetCellMesh(player).material = this.EmptyCellMaterial;
                this.GetCell(pos).IsSelectable = false;
                this.GetCell(pos).GridPosition.Cost = 0;
            }                
            player.ColoredPositions = new List<PositionGrid>();
        }
    }
}