using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GN00T.MagicaUnity {
	//[CustomEditor(typeof(VoxModel))]
	public class VoxModelEditor : Editor {
		private string filePath = string.Empty;
		private VoxModel targetModel;

		#region Renderer

		void Merge(VoxModel model, IEnumerable<Texture3D> textures) {

			var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model));
			var assetTextures = subAssets
				.Select(s => s as Texture3D)
				.Where(s => s != null)
				.ToArray();
			foreach (var go in assetTextures)
				DestroyImmediate(go, true);

			foreach (var texture in textures) {
				AssetDatabase.AddObjectToAsset(texture, targetModel);
			}
			EditorUtility.SetDirty(targetModel);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		void Merge(VoxModel model, params Texture2D[] textures) {

			var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model));
			var assetTextures = subAssets
				.Select(s => s as Texture2D)
				.Where(s => s != null)
				.ToArray();
			foreach (var go in assetTextures)
				DestroyImmediate(go, true);

			foreach (var texture in textures)
				AssetDatabase.AddObjectToAsset(texture, targetModel);

			EditorUtility.SetDirty(targetModel);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}


		void GenerateMaterialTexture2D(VoxModel model) {
			var texture = new Texture2D(256, 1,
				TextureFormat.RGBA32, false) {
				name = "Color",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point
			};
			texture.SetPixels(model.palette);

			var materials = VoxMesher.MaterialChunkToVector4(model.materialChunks);
			var material = new Texture2D(256, 1,
				TextureFormat.RGBA32, false) {
				name = "Material",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point
			};
			material.SetPixels(materials.Select(x =>
				new Color(x.x, x.y, x.z, x.w)).ToArray());
			Merge(model, texture, material);
		}

		void GenerateTex3D(VoxModel model) {
			Merge(model, model.voxelFrames
				.Select((f, i) => CreateTexture3D(f, $"{i}")));
		}
		Texture3D CreateTexture3D(VoxelData frame, string name) {
			var texture = new Texture3D(frame.VoxelsWide, frame.VoxelsTall, frame.VoxelsDeep,
				TextureFormat.Alpha8, false) {
				name = name,
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Point
			};
			var length = frame.VoxelsWide * frame.VoxelsTall * frame.VoxelsDeep;
			texture.SetPixels(
				Enumerable
					.Range(0, length)
					.Select(x => (Color)new Color32(0, 0, 0, frame.Get(x)))
					.ToArray());
			texture.Apply();
			return texture;
		}
		#endregion

		public override void OnInspectorGUI() {
			var model = target as VoxModel;

			if (targetModel == null || model != targetModel) {
				//filePath = model.modelSource;
				targetModel = model;
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("Input File", GUILayout.ExpandWidth(false));
			GUI.enabled = false;

			GUILayout.TextArea(filePath.Replace(Application.dataPath, ""));
			GUI.enabled = true;

			if (GUILayout.Button("…", GUILayout.ExpandWidth(false))) {
				filePath = EditorUtility.OpenFilePanel("Open model", EditorPrefs.GetString("LastVoxPath", Application.dataPath), "vox");
				if (File.Exists(filePath))
					EditorPrefs.SetString("LastVoxPath", filePath);
			}
			GUILayout.EndHorizontal();
			if (filePath != string.Empty) {
				if (GUILayout.Button("➡Generate Model", GUILayout.ExpandWidth(false))) {
					try {
						EditorUtility.DisplayProgressBar("Vox", "", 0);
						GenerateModel(filePath, model, targetModel);
					} catch (System.Exception ex) {
						Debug.LogException(ex);
					} finally {
						EditorUtility.ClearProgressBar();
					}
				}

				if (GUILayout.Button("Gen Texture 3D", GUILayout.ExpandWidth(false))) {
					GenerateTex3D(model);
				}
				if (GUILayout.Button("Gen Color/Material Texture 2D", GUILayout.ExpandWidth(false))) {
					GenerateMaterialTexture2D(model);
				}

			}

			EditorGUILayout.Separator();

			base.OnInspectorGUI();
		}

		delegate Mesh MeshSelector(MeshSet m);
		GameObject BuildLODGroup(MeshLODs list, MeshSelector selector,
			VoxModel model, GameObject modelGO, string name, Material material) {
			var go = new GameObject(name);
			go.transform.SetParent(modelGO.transform);
			var g = go.AddComponent<LODGroup>();

			g.SetLODs(list.LODs.Select((m, l) => selector(list.LODs[l])).Select((m, l) => {
				var goLOD = new GameObject(l.ToString(),
					typeof(MeshFilter), typeof(MeshRenderer));
				goLOD.transform.SetParent(go.transform);
				var h = Mathf.Pow(2 - (model.Settings.LODBias / 16f), -l);
				//Debug.Log(h);
				var r = new[] { goLOD.GetComponent<MeshRenderer>(), };
				r[0].material = material;
				var mf = goLOD.GetComponent<MeshFilter>();
				mf.mesh = m;
				return new LOD(h, r);
			}).ToArray());
			g.RecalculateBounds();
			return go;
		}

		void BuildLODGroups(VoxModel model, List<GameObject> targets) {
			for (int i = 0; i < model.meshes.Count; i++) {
				var modelGO = targets[i];
				var frame = model.meshes[i];
				if (frame.LODs.Any(m => m.opaque != null && m.opaque.triangles.Length > 0))
					BuildLODGroup(frame, m => m.opaque, model, modelGO,
						$"Model[{i}].opaque", model.Settings.materialOpaque);
				if (frame.LODs
					.Any(m => m.transparent != null && m.transparent.triangles.Length > 0))
					BuildLODGroup(frame, m => m.transparent, model, modelGO,
						$"Model[{i}].transparent", model.Settings.materialTransparent);
			}
		}
		GameObject BuildSceneGraph(VoxModel model, List<GameObject> models) {
			if (!model.transformNodeChunks.Any())
				return null;
			var chunks = model.transformNodeChunks.Cast<NodeChunk>()
						.Concat(model.groupNodeChunks)
						.Concat(model.shapeNodeChunks)
						.ToDictionary(x => x.id, x => x);

			var gos = Enumerable.Range(0, chunks.Count).Select(i => {
				switch (chunks[i].Type) {
					default:
					case NodeType.Transform:
						return new GameObject();
					case NodeType.Group:
						return null;
					case NodeType.Shape:
						return null;
				}
			}).ToArray();

			var toVox = Quaternion.Inverse(MagicaVoxelParser.toUnity);
			foreach (var t in model.transformNodeChunks) {
				Vector3 scale;
				var rotation = GetTransform(t.RotationAt(0), out scale);
				rotation.x *= -1;
				rotation.z *= -1;
				//rotation.w *= -1;
				gos[t.id].transform.localScale = scale;
				var translation = t.TranslationAt(0) * model.Settings.modelScale;
				translation.z = -translation.z;
				gos[t.id].transform.localPosition = translation;
				gos[t.id].transform.localRotation = rotation * toVox;
				//Debug.Log($"t{t.id}→{t.childId}");
				BuildContentNode(model, gos[t.id], t.childId, chunks[t.childId].Type, models, gos);
				var go = gos[t.id];
				go.name = t.Name;
				if (go.name == string.Empty)
					go.name = $"Transform[{t.id}]";
				go.SetActive(!t.Hidden && !(t.layerId >= 0 && model.layerChunks.ElementAt(t.layerId).Hidden));
			}

			if (gos.Any()) {
				var root = gos.First();
				root.name = model.name;
				var translation = MagicaVoxelParser.toUnity * root.transform.localPosition;
				var rotation = root.transform.localRotation * MagicaVoxelParser.toUnity * MagicaVoxelParser.toUnity;
				translation.z = -translation.z;
				rotation.w *= -1;
				root.transform.localPosition = translation;
				root.transform.localScale = new Vector3(
						-root.transform.localScale.x, root.transform.localScale.y, -root.transform.localScale.z);
				root.transform.localRotation = rotation;
				return root;
			}
			return null;
		}

		public static Quaternion GetTransform(ROTATION r, out Vector3 scale) {
			var b = (byte)r;
			var m = Matrix4x4.zero;
			var x = b & 3;
			var y = (b >> 2) & 3;
			scale.x = ((b >> 4) & 1) != 0 ? -1 : 1;
			scale.y = ((b >> 5) & 1) != 0 ? -1 : 1;
			scale.z = ((b >> 6) & 1) != 0 ? -1 : 1;
			m[x, 0] = scale.x;
			m[y, 1] = scale.y;
			m[Mathf.Clamp(3 - x - y, 0, 2), 2] = scale.z;
			m[3, 3] = 1;
			//Debug.Log($"{r} ({b})↓\r\n{m}");
			//Debug.Log($"lossyScale={m.lossyScale}");
			//Debug.Log($"scale={scale}");
			scale = m.lossyScale;
			return m.rotation;
		}

		void BuildContentNode(VoxModel model, GameObject parent, int node, NodeType type, List<GameObject> models, GameObject[] gos) {
			switch (type) {
				case NodeType.Group:
					foreach (var childId in model.groupNodeChunks.First(c => c.id == node).childIds) {
						gos[childId].transform.SetParent(parent.transform);
					}
					break;
				case NodeType.Shape:
					foreach (var sm in model.shapeNodeChunks.First(c => c.id == node).models) {
						var child = Instantiate(models[sm.modelId], parent.transform);
						child.name = $"Model[{sm.modelId}]";
					}
					break;
			}
		}

		void BuildMesh(Mesh target, Mesh source, string name, List<Vector4> work, bool uvUnwrapping) {
			target.Clear();
			target.name = name;
			if (source == null)
				return;
			target.vertices = source.vertices;
			target.colors = source.colors;
			target.normals = source.normals;
			target.triangles = source.triangles;
			source.GetUVs(2, work);
			target.SetUVs(2, work);
			//Debug.Log($"uv3: {uv3.Where(uv => uv.y > 0.5f).Count()}");
			//Debug.Log($"v: {m.vertices.Length} c:{m.colors.Length} n:{m.normals.Length} t:{m.triangles.Length}");

			if (!uvUnwrapping && target.triangles.Length > 0) {
				target.uv = Unwrapping.GeneratePerTriangleUV(target);
				Unwrapping.GenerateSecondaryUVSet(target);
				target.RecalculateBounds();
			}
		}

		void GenerateModel(string filePath, VoxModel model, VoxModel targetModel) {
			var parser = new MagicaVoxelParser();

			//model.modelSource = filePath;
			parser.LoadModel(filePath, model,
				s => EditorUtility.DisplayProgressBar("vox", s, 0));

			EditorUtility.SetDirty(model);
			var path = AssetDatabase.GetAssetPath(model);
			var name = Path.GetFileNameWithoutExtension(path);
			var subAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(model));
			//load asset meshes
			var assetMeshes = subAssets
				.Select(s => s as Mesh)
				.Where(s => s != null)
				.ToArray();
			var updateOpaque = false;
			var updateTransparent = false;

			var uv3 = new List<Vector4>();

			var assetIndex = 0;
			for (int i = 0; i < model.meshes.Count; i++) {
				for (int l = 0; l < model.meshes[i].LODs.Count; l++) {
					MeshSet m;
					//get mesh
					m.opaque = assetIndex < assetMeshes.Length ? assetMeshes[assetIndex++] : null;
					m.transparent = assetIndex < assetMeshes.Length ? assetMeshes[assetIndex++] : null;
					updateOpaque = m.opaque == null;
					if (updateOpaque)
						m.opaque = new Mesh();
					updateTransparent = m.transparent == null;
					if (updateTransparent)
						m.transparent = new Mesh();

					//turn temp meshes into assets

					EditorUtility.DisplayProgressBar("vox",
						$"asset: frame={i}/{model.meshes.Count}, lod={l}/{model.meshes[i].LODs.Count}",
						.5f + (.5f * i) / model.voxelFrames.Count);
					var frame = model.meshes[i].LODs[l];
					BuildMesh(m.opaque, frame.opaque,
						$"{name}.{i}.{l}.opaque", uv3, model.Settings.OmitsUVUnwrapping);
					BuildMesh(m.transparent, frame.transparent,
						$"{name}.{i}.{l}.transparent", uv3, model.Settings.OmitsUVUnwrapping);
					//new mesh
					if (updateOpaque)
						AssetDatabase.AddObjectToAsset(m.opaque, targetModel);
					if (updateTransparent)
						AssetDatabase.AddObjectToAsset(m.transparent, targetModel);
					model.meshes[i].LODs[l] = m;
				}
			}

			var baseGO = new GameObject();
			var gos = Enumerable.Range(0, baseGO.transform.childCount)
				.Select(i => baseGO.transform.GetChild(i))
				.Select(t => t.gameObject)
				.ToList();
			var gosCount = gos.Count;
			for (int i = gosCount; i < model.meshes.Count; i++) {
				var target = new GameObject();
				target.transform.SetParent(baseGO.transform);
				gos.Add(target.gameObject);
			}
			BuildLODGroups(model, gos);
			if (model.Settings.asSceneGraph) {
				var root = BuildSceneGraph(model, gos);
				DestroyImmediate(baseGO);
				baseGO = root;
			}
			//destroy unneeded meshes
			foreach (var go in subAssets
				.Where(s => (s as Mesh) != null)
				.Where(s => !model.meshes.SelectMany(x => x.LODs).Any(x => x.Contains(s as Mesh))))
				DestroyImmediate(go, true);

			if (targetModel.prefab == null) {
				targetModel.prefab = PrefabUtility.CreatePrefab(path + ".prefab", new GameObject());
			}
			PrefabUtility.ReplacePrefab(baseGO, targetModel.prefab, ReplacePrefabOptions.ReplaceNameBased);
			DestroyImmediate(baseGO);
			EditorUtility.SetDirty(targetModel);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}
