using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    [ExecuteInEditMode] //Disable if you don't care about previewing outside of play mode
    public class WorldSpaceOverlayUI : MonoBehaviour
    {
        private const string ShaderTestMode = "unity_GUIZTestMode"; //The magic property we need to set
        [SerializeField] 
        private UnityEngine.Rendering.CompareFunction desiredUIComparison = UnityEngine.Rendering.CompareFunction.Always; //If you want to try out other effects
        [Tooltip("Set to blank to automatically populate from the child UI elements")]
        [SerializeField]
        private Graphic[] uiElementsToApplyTo;
        //Allows us to reuse materials
        private readonly Dictionary<Material, Material> _materialMappings = new ();
        private static readonly int UnityGuizTestMode = Shader.PropertyToID(ShaderTestMode);

        private void Start()
        {
            if (uiElementsToApplyTo == null || uiElementsToApplyTo.Length == 0)
            {
                uiElementsToApplyTo = gameObject.GetComponentsInChildren<Graphic>(true);
            }
            foreach (var graphic in uiElementsToApplyTo)
            {
                var material = graphic.materialForRendering;
                if (material == null)
                {
                    Debug.LogError($"{nameof(WorldSpaceOverlayUI)}: skipping target without material {graphic.name}.{graphic.GetType().Name}");
                    continue;
                }
                if (!_materialMappings.TryGetValue(material, out Material materialCopy))
                {
                    materialCopy = new Material(material);
                    _materialMappings.Add(material, materialCopy);
                }
                materialCopy.SetInt(UnityGuizTestMode, (int)desiredUIComparison);
                graphic.material = materialCopy;
            }
        }
    }
}