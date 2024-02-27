using UnityEngine;

namespace Hex.Extensions
{
	public static class GameObjectExtensions
	{
		public static void SetLayer(this GameObject obj, int layer, bool includeChildren = false, bool recursive = false)
		{
			obj.layer = layer;
			if (!includeChildren)
				return;

			for (var i = 0; i < obj.transform.childCount; i++)
			{
				var child = obj.transform.GetChild(i).gameObject;
				child.layer = layer;
				if(recursive) child.SetLayer(layer, true, true);
			}
		}
	}
}