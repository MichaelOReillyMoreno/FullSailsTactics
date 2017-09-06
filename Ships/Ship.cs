using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace FullSailTactics
{
    /// <summary>
    /// Clase que representa un barco
    /// </summary>
    public class Ship : MonoBehaviour
    {
        //Datos del barco
        public int ShipID;
        public int ShipInitiative;
        public int ShipLife;
        public int ShipSkillPoints = 0;
        public int ShipCost = 0;
        public float ShipHeight = -0.4f;
        public PositionGrid ActualPosition;

        public PositionGrid TargetPosition { get; set; }
        public AttackCard TargetCard { get; set; }

        public PlayerManager OwnerPlayer { get; set; }

        //Tiempos de movimiento y rotacion
        public float RotationTime = 1;
        public float MovementTime = 1;

        public ShipDeck Deck { get; set; }

        //Indica si se esta moviendo el barco
        private bool isMoving;

        //Canvas
        private ShipCanvas shipCanvas_Player_1;
        private ShipCanvas shipCanvas_Player_2;

        void Start()
        {
            this.TargetPosition = null;

            this.shipCanvas_Player_1 = this.GetComponentsInChildren<ShipCanvas>()[0];
            this.shipCanvas_Player_2 = this.GetComponentsInChildren<ShipCanvas>()[1];

            //Asocio el barco a la celda del tablero correspondiente
            Cell actualCell = GridManager.Instance.GetCell(this.ActualPosition);
            actualCell.SetShipCell(this);

            //Posicion el barco en la celda a la altura indicada
            this.transform.position = new Vector3(actualCell.WorldPosition.x, this.ShipHeight, actualCell.WorldPosition.z);

            //Asocio el mazo al barco
            this.Deck = new ShipDeck(TextsManager.Instance.ShipDecks[this.ShipID]);
            this.Deck.ShuffleDeck();

            //Actualizo la vida del barco
            this.UpdateLifeBoat();
        }

        /// <summary>
        /// Metodo que muestra las celdas de movimiento del barco
        /// </summary>
        public void ShowMovementCells()
        {
            GridManager.Instance.PaintCells(this.OwnerPlayer, GameManager.Instance.CurrentPhase, this.ShowCells(TextsManager.Instance.ShipDecks[this.ShipID].Movements));
        }

        /// <summary>
        /// Corutina que realiza el movimiento a la posicion indicada
        /// </summary>
        /// <param name="finalPosition"></param>
        /// <returns></returns>
        public IEnumerator ExecuteMovement()
        {
            //Si no he seleccionado la carta
            if (this.TargetPosition == null)
                yield break;

            //Si se esta moviendo el barco, no hago nada
            if (this.isMoving)
                yield break;

            this.isMoving = true;

            //Reseteo la posicion inicial
            GridManager.Instance.GetCell(this.ActualPosition).SetShipCell(null);

            //Calculo la distancia entre las posiciones y la descompongo en movimientos unitarios
            Vector3 finalRotation = this.transform.localEulerAngles;
            PositionGrid distance = this.TargetPosition - this.ActualPosition;

            List<DIRECTIONS> movements = new List<DIRECTIONS>();
            if(finalRotation.y == 270 || finalRotation.y == 90)
            {
                //Caso de estar mirando arriba o abajo y el destino esta detras mio
                if ((distance.X > 0 && finalRotation.y == 270) || (distance.X < 0 && finalRotation.y == 90)) 
                    movements = this.ConfigMovements(distance, false);
                else
                    movements = this.ConfigMovements(distance, true);
            }
            else if(finalRotation.y == 0 || finalRotation.y == 180)
            {
                //Caso de estar mirando izq o dr y el destino esta detras mio
                if ((distance.Y > 0 && finalRotation.y == 180) || (distance.Y < 0 && finalRotation.y == 0))
                    movements = this.ConfigMovements(distance, true);
                else
                    movements = this.ConfigMovements(distance, false);
            }       
            
            //Segun los movimientos unitarios, los convierto en posiciones en el tablero y configuro la rotacion segun el movimiento
            PositionGrid pos = new PositionGrid(0, 0);
            foreach(DIRECTIONS dir in movements)
            {
                switch(dir)
                {
                    case DIRECTIONS.UP:
                        pos = new PositionGrid(-1, 0);
                        finalRotation = new Vector3(0, 270, 0);
                        break;
                    case DIRECTIONS.RIGHT:
                        pos = new PositionGrid(0, 1);
                        finalRotation = new Vector3(0, 0, 0);
                        break;
                    case DIRECTIONS.DOWN:
                        pos = new PositionGrid(1, 0);
                        finalRotation = new Vector3(0, 90, 0);
                        break;
                    case DIRECTIONS.LEFT:
                        pos = new PositionGrid(0, -1);
                        finalRotation = new Vector3(0, 180, 0);
                        break;
                }

                //Actualizo la finalPosition
                this.TargetPosition = this.ActualPosition + pos;

                //Realizo la rotacion y el movimiento del barco
                if(this.transform.localEulerAngles != finalRotation)
                    yield return StartCoroutine(CorgiTools.RotateFromTo(this.gameObject, this.transform.localEulerAngles, finalRotation, this.RotationTime));
                yield return StartCoroutine(CorgiTools.MoveFromTo(this.gameObject, this.transform.position, new Vector3(GridManager.Instance.GetCell(this.TargetPosition).WorldPosition.x, this.ShipHeight, GridManager.Instance.GetCell(this.TargetPosition).WorldPosition.z), this.MovementTime));

                //Actualizo la posicion actual
                this.ActualPosition = this.TargetPosition;
            }

            this.isMoving = false;

            //Reseteo la posicion 
            this.TargetPosition = null;

            //Actualizo la celda actual
            GridManager.Instance.GetCell(this.ActualPosition).SetShipCell(this);
        }

        /// <summary>
        /// Metodo que devuelve la lista de movimientos segun la distancia entre la posicion inicial y la final
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="order">Segun la rotacion del barco, priorizo el eje X o el eje Y</param>
        /// <returns></returns>
        private List<DIRECTIONS> ConfigMovements(PositionGrid distance, bool order)
        {
            List<DIRECTIONS> movements = new List<DIRECTIONS>();
            if (order)
            {
                for (int i = 0; i < Mathf.Abs(distance.X); i++)
                {
                    int sign = (int)Mathf.Sign(distance.X);
                    movements.Add((sign == -1) ? DIRECTIONS.UP : DIRECTIONS.DOWN);
                }
                for (int i = 0; i < Mathf.Abs(distance.Y); i++)
                {
                    int sign = (int)Mathf.Sign(distance.Y);
                    movements.Add((sign == -1) ? DIRECTIONS.LEFT : DIRECTIONS.RIGHT);
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Abs(distance.Y); i++)
                {
                    int sign = (int)Mathf.Sign(distance.Y);
                    movements.Add((sign == -1) ? DIRECTIONS.LEFT : DIRECTIONS.RIGHT);
                }
                for (int i = 0; i < Mathf.Abs(distance.X); i++)
                {
                    int sign = (int)Mathf.Sign(distance.X);
                    movements.Add((sign == -1) ? DIRECTIONS.UP : DIRECTIONS.DOWN);
                }
            }
            return movements;
        }

        /// <summary>
        /// Metodo que muestra las celdas de ataque de la carta del barco
        /// </summary>
        public void ShowAttackCells(int indexCard)
        {
            AttackCard card = this.Deck.AttackCards[indexCard];
            GridManager.Instance.PaintCells(this.OwnerPlayer, GameManager.Instance.CurrentPhase, this.ShowCells(card.AttackCells));
        }

        /// <summary>
        /// Metodo para realizar un ataque de la carta indicada
        /// </summary>
        /// <param name="finalPosition"></param>
        public IEnumerator ExecuteAttack()
        {
            //Si no he seleccionado la carta
            if (this.TargetCard == null)
                yield break;

            int sign = 0;
            bool flip = false;

            //Dependiendo de la rotacion actual del barco, cambio el signo o el orden de las coordenadas
            if (this.transform.localEulerAngles.y == 270) //UP
            {
                sign = 1;
            }
            else if (this.transform.localEulerAngles.y == 90) //DOWN
            {
                sign = -1;
            }
            else if (this.transform.localEulerAngles.y == 180) //LEFT
            {
                sign = 1;
                flip = true;
            }
            else if (this.transform.localEulerAngles.y == 0) //RIGHT
            {
                sign = -1;
                flip = true;
            }

            List<PositionGrid> moves = this.TargetCard.AttackCells;
            foreach (PositionGrid pos in moves)
            {
                PositionGrid _pos = this.ActualPosition + new PositionGrid(pos.X * sign, pos.Y).Flip(flip);
                if(GridManager.Instance.IsCellValid(_pos))
                {
                    if(GridManager.Instance.GetCell(_pos).IsEmpty)
                        Instantiate(GridManager.Instance.AttackWaterParticle, GridManager.Instance.GetCell(_pos).WorldPosition, Quaternion.Euler(-90, 0, 0));
                    else
                    {
                        Instantiate(GridManager.Instance.AttackBoatParticle, GridManager.Instance.GetCell(_pos).WorldPosition, Quaternion.Euler(-90, 0, 0));

                        GridManager.Instance.GetCell(_pos).ShipCell.ShipLife -= this.TargetCard.Damage;
                        GridManager.Instance.GetCell(_pos).ShipCell.UpdateLifeBoat();
                    }                        
                }                    
            }

            //Elimino la carta usada
            this.Deck.AttackCards.Remove(this.TargetCard);
            this.TargetCard = null;

            yield return new WaitForSeconds(2.5f);
        }

        /// <summary>
        /// Metodo que devuelve las celdas coloreadas
        /// </summary>
        /// <param name="moves"></param>
        /// <returns></returns>
        private List<PositionGrid> ShowCells(List<PositionGrid> moves)
        {
            List<PositionGrid> coloredPositions = new List<PositionGrid>();
            int sign = 0;
            bool flip = false;

            //Dependiendo de la rotacion actual del barco, cambio el signo o el orden de las coordenadas
            if (this.transform.localEulerAngles.y == 270) //UP
            {
                sign = 1;
            }
            else if (this.transform.localEulerAngles.y == 90) //DOWN
            {
                sign = -1;
            }
            else if (this.transform.localEulerAngles.y == 180) //LEFT
            {
                sign = 1;
                flip = true;
            }
            else if (this.transform.localEulerAngles.y == 0) //RIGHT
            {
                sign = -1;
                flip = true;
            }

            foreach (PositionGrid pos in moves)
            {
                PositionGrid _p = this.ActualPosition + new PositionGrid(pos.X * sign, pos.Y).Flip(flip);
                _p.Cost = pos.Cost;

                coloredPositions.Add(_p);
            }

            //Limpio la lista de celdas, quedandome solo con las celdas validas
            coloredPositions = coloredPositions.Where(x => GridManager.Instance.IsCellValid(x)).ToList();
            return coloredPositions;
        }

        /// <summary>
        /// Metodo para actualizar la vida del barco
        /// </summary>
        public void UpdateLifeBoat()
        {
            this.shipCanvas_Player_1.UpdateBoatLife(this.ShipLife);
            this.shipCanvas_Player_2.UpdateBoatLife(this.ShipLife);
        }
    }
}