using UnityEngine;
using UnityEngine.UI;

namespace Hex.UI
{
    [RequireComponent(typeof(Button), typeof(Animator))]
    public class BasicButton : MonoBehaviour
    {
        private const string InOnEnterBool = "InOnEnter";
        private readonly int _inOnEnterHash =  Animator.StringToHash(InOnEnterBool);
        
        [SerializeField] private bool playInOnEnable;

        public Button Button => GetComponent<Button>();
        
        protected void OnEnable()
        {
            if (!playInOnEnable)
            {
                return;
            }
            GetComponent<Animator>().SetBool(_inOnEnterHash, true);
        }
    }
}