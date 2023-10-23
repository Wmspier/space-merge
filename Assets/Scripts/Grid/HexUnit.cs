using UnityEngine;
using static Hex.Configuration.HexUnitConfiguration;

namespace Hex.Grid
{
    public class HexUnit : MonoBehaviour
    {
        public int Health { get; private set; }
        public int BaseHealth { get; private set; }

        public void ApplyData(HexUnitData data)
        {
            BaseHealth = Health = data.BaseHealth;
        }
    }
}