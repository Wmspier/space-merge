using System;
using System.Collections.Generic;

namespace Hex.Extensions
{
	public static class ListExtensions
	{
		public static IList<T> Shuffle<T>(this IList<T> values)
		{
			var rand = new Random();
			for (var i = values.Count - 1; i > 0; i--) {
				var k = rand.Next(i + 1);
				(values[i], values[k]) = (values[k], values[i]);
			}
			return values;
		}

		public static IList<T> FillWithDefault<T>(this IList<T> list, int amount)
		{
			for (var i = 0; i < amount; i++)
			{
				list.Add(default);
			}

			return list;
		}
	}
}