using System;
using System.Collections.Generic;
using System.IO;
using Hex.Grid.Cell;
using Hex.Grid.Serialization;
using UnityEngine;

namespace Hex.Tools
{
	public static class LevelEditorUtility
	{
		private static readonly IGridSerializer Serializer = new BasicGridSerializer();

		private static readonly string FileRootPath = $"{Application.dataPath}/StaticData/Levels/";

		public static bool LevelExists(string fileName)
		{
			var filePath = $"{FileRootPath}{fileName}.json";
			return File.Exists(filePath);
		}
		
		public static void SaveLevel(string fileName, List<HexCellDefinition> cellDefinitions)
		{
			var serializedGrid = Serializer.Serialize(cellDefinitions);

			var filePath = $"{FileRootPath}{fileName}.json";
			System.IO.File.WriteAllText(filePath, serializedGrid);
		}

		public static List<HexCellDefinition> LoadLevel(string fileName)
		{
			try
			{
				var filePath = $"{FileRootPath}{fileName}.json";
				var serializedGrid = System.IO.File.ReadAllText(filePath);
				return Serializer.Deserialize(serializedGrid);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}
	}
}