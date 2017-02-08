using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.IO;

public class ScriptableObjects {
	[MenuItem("Assets/Create/Game/Atmosphere")]
	public static void CreateAtmosphere() { CustomAssetUtility.CreateAsset<Atmosphere>(); }
	[MenuItem("Assets/Create/Game/Entity")]
	public static void CreateEntity() { CustomAssetUtility.CreateAsset<Entity>(); }
	[MenuItem("Assets/Create/Game/Scene Redirect")]
	public static void CreateSceneRedirect() { CustomAssetUtility.CreateAsset<SceneRedirect>(); }
}

public static class CustomAssetUtility {
	public static void CreateAsset<T>() where T : ScriptableObject {
		T asset = ScriptableObject.CreateInstance<T>();

		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		if (path == "") {
			path = "Assets";
		}
		else if (Path.GetExtension(path) != "") {
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

		AssetDatabase.CreateAsset(asset, assetPathAndName);

		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = asset;
	}

	public static void CreateAsset<T>(string path) where T : ScriptableObject {
		T asset = ScriptableObject.CreateInstance<T>();

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");

		AssetDatabase.CreateAsset(asset, assetPathAndName);

		AssetDatabase.SaveAssets();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = asset;
	}
}