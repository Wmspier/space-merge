using System.Collections.Generic;
using Hex.Extensions;
using TMPro;
using UnityEngine;

namespace Hex.Grid
{
    public enum HexCellDirection
    {
        None,
        Top,
        TopRight,
        BottomRight,
        Bottom,
        BottomLeft,
        TopLeft
    }
    
    [RequireComponent(typeof(HexCellDetail))]
    public class HexCell : MonoBehaviour
    {
        [SerializeField] private MeshRenderer outline;
        [SerializeField] private GameObject outlineTop;
        [SerializeField] private GameObject outlineTopRight;
        [SerializeField] private GameObject outlineBottomRight;
        [SerializeField] private GameObject outlineBottom;
        [SerializeField] private GameObject outlineBottomLeft;
        [SerializeField] private GameObject outlineTopLeft;

        [Header("UI")] 
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject canCombineRoot;
        [SerializeField] private GameObject cannotCombineRoot;
        [SerializeField] private TMP_Text matchCountText;
        [SerializeField] private TMP_Text matchRequirementText;
        
        private Material _outlineMaterial;
        private bool _arrowOverHex;

        private HexUnit _unit;
        
        private readonly Dictionary<HexCellDirection, GameObject> _outlineByDirection = new();

        public Vector3Int Coordinates { get; private set; }

        public List<HexCell> Neighbors { get; } = new ();

        public HexUnit Unit => _unit;
        
        public void RegisterNeighbor(HexCell cell) => Neighbors.Add(cell);

        public HexCellDetail Detail => GetComponent<HexCellDetail>();

        private void Awake()
        {
            _outlineMaterial = new Material(outline.material);
            
            _outlineByDirection[HexCellDirection.Top] = outlineTop;
            _outlineByDirection[HexCellDirection.TopRight] = outlineTopRight;
            _outlineByDirection[HexCellDirection.BottomRight] = outlineBottomRight;
            _outlineByDirection[HexCellDirection.Bottom] = outlineBottom;
            _outlineByDirection[HexCellDirection.BottomLeft] = outlineBottomLeft;
            _outlineByDirection[HexCellDirection.TopLeft] = outlineTopLeft;

            foreach (var (_, o) in _outlineByDirection)
            {
                o.transform.GetChild(0).GetComponent<MeshRenderer>().material = _outlineMaterial;
            }
        }

        public override string ToString() => $"[{Coordinates.x},{Coordinates.y},{Coordinates.z}]";

        public void SetOutlineColor(Color color) => _outlineMaterial.color = color;

        public void ToggleCanvas(bool visible) => canvasRoot.SetActive(visible);

        public void SetMatchCount(int count, int req)
        {
            matchCountText.text = count.ToString();
            matchRequirementText.text = req.ToString();
        }

        public void ToggleCanCombine(bool canCombine)
        {
            canCombineRoot.SetActive(canCombine);
            cannotCombineRoot.SetActive(!canCombine);
        }

        public void ApplyCoordinates(int x, int y, int z) => Coordinates = new Vector3Int(x, y, z);
        
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

        public bool AssignUnit(HexUnit unit)
        {
            if (_unit != null) return false;
            _unit = unit;
            return true;
        }
    }
}