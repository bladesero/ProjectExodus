﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace SceneExport{
	[System.Serializable]
	public class JsonScene: IFastJsonValue{
		public List<JsonGameObject> objects = new List<JsonGameObject>();
		
		public static JsonScene fromScene(Scene scene, ResourceMapper resMap, bool showGui){
			var rootObjects = scene.GetRootGameObjects();
			return fromObjects(rootObjects, resMap, showGui);
		}
		
		public static JsonScene fromObject(GameObject arg, ResourceMapper resMap, bool showGui){
			return fromObjects(new GameObject[]{arg}, resMap, showGui);
		}
		
		public static JsonScene fromObjects(GameObject[] args, ResourceMapper resMap, bool showGui){
			var result = new JsonScene();
			
			var objMap = new GameObjectMapper();
			foreach(var curObj in args){
				objMap.gatherObjectIds(curObj);
			}
			
			//Prefabs:
			/*
			for(int i = 0; i < objMap.objectList.Count; i++){
				var cur = objMap.objectList[i];
				var prefabType = PrefabUtility.GetPrefabType(cur);
				Debug.LogFormat("Cur obj: {0}({2}), prefabType: {1}", cur, prefabType, ExportUtility.getObjectPath(cur));
				if ((prefabType == PrefabType.PrefabInstance) || (prefabType == PrefabType.ModelPrefabInstance)){
					Debug.LogFormat("Object is a prefab instance (type: {0})", prefabType);
					var source = PrefabUtility.GetCorrespondingObjectFromSource(cur);
					var rootPrefab = PrefabUtility.FindPrefabRoot(source as GameObject);
					var rootPath = AssetDatabase.GetAssetPath(rootPrefab);
					Debug.LogFormat("Found prefab: {0}, prefab root: {1} ({2})", source, rootPrefab, rootPath);
					var path = AssetDatabase.GetAssetPath(source);
					Debug.LogFormat("Prefab path: {0}; objectPath: {1}", path, ExportUtility.getObjectPath(source as GameObject));
				}
				//var prefabObj = PrefabUtility.GetPrefabType(
				//Debug.LogFormat("Obj
			}
			*/
			
			for(int i = 0; i < objMap.objectList.Count; i++){
				/*TODO: The constructor CAN add more data, but most of it would've been added prior to this point.
				Contempalting whether to enforce it strictly or not.*/
				if (showGui){
					ExportUtility.showProgressBar("Collecting scene data", "Adding scene object {0}/{1}", i, objMap.objectList.Count);
				}
				
				var newObj = new JsonGameObject(objMap.objectList[i], objMap, resMap);
				result.objects.Add(newObj);
			}
			if (showGui){
				ExportUtility.hideProgressBar();
			}
			
			return result;
		}
			
		public void writeJsonObjectFields(FastJsonWriter writer){
			writer.writeKeyVal("objects", objects);
		}
			
		public void writeRawJsonValue(FastJsonWriter writer){
			writer.beginRawObject();
			writeJsonObjectFields(writer);
			writer.endObject();
		}

		public void fixNameClashes(){
			var nameClashes = new Dictionary<NameClashKey, List<int>>();
			for(int i = 0; i < objects.Count; i++){
				var cur = objects[i];
				var key = new NameClashKey(cur.name, cur.parent);
				var idList = nameClashes.getValOrGenerate(key, (parId_) => new List<int>());
				idList.Add(cur.id);
			}
			
			foreach(var entry in nameClashes){
				var key = entry.Key;
				var list = entry.Value;
				if ((list == null) || (list.Count <= 1))
					continue;

				for(int i = 1; i < list.Count; i++){
					var curId = list[i];
					if ((curId <= 0) || (curId >= objects.Count)){
						Debug.LogErrorFormat("Invalid object id {0}, while processing name clash {1};\"{2}\"", 
							curId, key.parentId, key.name);
						continue;
					}
					var curObj = objects[curId];
					var altName = string.Format("{0}-#{1}", key.name, i);
					while(nameClashes.ContainsKey(new NameClashKey(altName, key.parentId))){
						altName = string.Format("{0}-#{1}({2})", 
							key.name, i, System.Guid.NewGuid().ToString("n"));
						//break;
					}
					curObj.nameClash = true;
					curObj.uniqueName = altName;
				}								
			}			
		}
	}
}