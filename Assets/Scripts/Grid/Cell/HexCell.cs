using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Hex.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Hex.Grid.Cell
{
    public enum HexCellDirection
    {
        None,
        TopLeft,
        TopRight,
        Right,
        BottomLeft,
        BottomRight,
        Left
    }
    
    [RequireComponent(typeof(HexCellInfoHolder))]
    public class HexCell : MonoBehaviour
    {
		private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
		
        [Header("Outlines")]
        [SerializeField] private MeshRenderer outline;
        [SerializeField] private GameObject outlineTopLeft;
        [SerializeField] private GameObject outlineTopRight;
        [SerializeField] private GameObject outlineRight;
        [SerializeField] private GameObject outlineBottomLeft;
        [SerializeField] private GameObject outlineBottomRight;
        [SerializeField] private GameObject outlineLeft;
        
        [Header("Move Arrows")]
        [SerializeField] private MeshRenderer moveArrow;
        [SerializeField] private GameObject moveArrowTopLeft;
        [SerializeField] private GameObject moveArrowTopRight;
        [SerializeField] private GameObject moveArrowRight;
        [SerializeField] private GameObject moveArrowBottomLeft;
        [SerializeField] private GameObject moveArrowBottomRight;
        [SerializeField] private GameObject moveArrowLeft;

        private Material _outlineMaterial;
        private bool _arrowOverHex;
        private Vector3 _originLocal;

        private readonly Dictionary<HexCellDirection, GameObject> _outlineByDirection = new();
        private readonly Dictionary<HexCellDirection, GameObject> _moveArrowByDirection = new();

        public int3 Coordinates { get; private set; }

        public List<HexCell> Neighbors { get; } = new ();

        public void RegisterNeighbor(HexCell cell) => Neighbors.Add(cell);

        public HexCellInfoHolder InfoHolder { get; private set; }

        public HexCellUI UI { get; private set; }

        public Transform SurfaceAnchor => InfoHolder.UnitAnchor;

        public bool HoldingEnemyAttack => InfoHolder.EnemyPower > 0;
        public bool HoldingUnit => InfoHolder.HeldPlayerUnit != null;

        private void Awake()
        {
            InfoHolder = GetComponent<HexCellInfoHolder>();
            UI = GetComponent<HexCellUI>(); 
            
            _outlineMaterial = new Material(outline.material);
            
            _outlineByDirection[HexCellDirection.TopLeft] = outlineTopLeft;
            _outlineByDirection[HexCellDirection.TopRight] = outlineTopRight;
            _outlineByDirection[HexCellDirection.Right] = outlineRight;
            _outlineByDirection[HexCellDirection.BottomRight] = outlineBottomRight;
            _outlineByDirection[HexCellDirection.BottomLeft] = outlineBottomLeft;
            _outlineByDirection[HexCellDirection.Left] = outlineLeft;
            _outlineByDirection[HexCellDirection.TopLeft] = outlineTopLeft;
            
            _moveArrowByDirection[HexCellDirection.TopLeft] = moveArrowTopLeft;
            _moveArrowByDirection[HexCellDirection.TopRight] = moveArrowTopRight;
            _moveArrowByDirection[HexCellDirection.Right] = moveArrowRight;
            _moveArrowByDirection[HexCellDirection.BottomRight] = moveArrowBottomRight;
            _moveArrowByDirection[HexCellDirection.BottomLeft] = moveArrowBottomLeft;
            _moveArrowByDirection[HexCellDirection.Left] = moveArrowLeft;
            _moveArrowByDirection[HexCellDirection.TopLeft] = moveArrowTopLeft;

            foreach (var (_, o) in _outlineByDirection)
            {
                o.transform.GetChild(0).GetComponent<MeshRenderer>().material = _outlineMaterial;
            }
        }

        public override string ToString() => $"[{Coordinates.x},{Coordinates.y},{Coordinates.z}]";

        public void SetOutlineColor(Color color)
        {
            _outlineMaterial.color = color;
            _outlineMaterial.SetColor(EmissionColor, color * 10);
        }

        public void ApplyCoordinates(int x, int y, int z) => Coordinates = new int3(x, y, z);
        public void SetLocalOrigin(Vector3 origin) => _originLocal = origin;
        
        public void ToggleOutline(bool visible, List<HexCell> connectedCells = null)
        {
            foreach (var o in _outlineByDirection)
            {
                o.Value.SetActive(visible);
            }

            if (connectedCells == null)
            {
                return;
            }

            foreach (var connectedCell in connectedCells)
            {
                var direction = this.GetDirectionOfNeighbor(connectedCell);
                
                if (direction == HexCellDirection.None)
                {
                    continue;
                }
                _outlineByDirection[direction].SetActive(false);
            }
        }

        public void ToggleMoveArrow(bool visible, HexCell nextCell = null)
        {
            foreach (var o in _moveArrowByDirection)
            {
                o.Value.SetActive(false);
            } 
            if (nextCell == null)
            {
                return;
            }
            
            var direction = this.GetDirectionOfNeighbor(nextCell);
            _moveArrowByDirection[direction].SetActive(visible);
        }
        
        public bool CanPulse = true;
        public async void Pulse(float maxIntensity, int intensityDecay = 0)
        {
            if (!CanPulse) return;
            
            transform.localPosition = _originLocal;

            var intensity = maxIntensity / (intensityDecay + 1);
            transform.DOPunchPosition(new Vector3(0f, intensity, 0f), .5f, 0, .25f);
            
            CanPulse = false;
            await Task.Delay(100);
            
            foreach (var n in Neighbors.Where(n => n.CanPulse))
            {
                n.Pulse(maxIntensity, intensityDecay+1);
            }

            await Task.Delay(500);
            CanPulse = true;
        }

        public void Impact()
        {
            transform.DOPunchPosition(new Vector3(0f, -.15f, 0f), .5f, 0, .25f);
        }
    }
}