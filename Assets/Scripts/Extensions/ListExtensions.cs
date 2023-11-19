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
	}
}