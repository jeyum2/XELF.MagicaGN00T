using System.Collections.Generic;
using UnityEngine;

namespace GN00T.MagicaUnity {
	[CreateAssetMenu(fileName = "VoxModel2", menuName = "Voxel/Model2", order = 10)]
	public class VoxModel2 : ScriptableObject {
		[Header("Output")]
		public GameObject prefab;

		[Header("Import settings")]
		public bool asSceneGraph = true;
		public bool OmitsUVUnwrapping;
		public Material materialOpaque;
		public Material materialTransparent;
		[Header("Scale for Voxel to world coordinates")]
		public float modelScale = 1f;
		[Header("Origin of model scale (Incorrect for Scene Graph)")]
		[Tooltip("(.5: center, 0: left, 1: right) for each axis")]
		public Vector3 origin = new Vector3(.5f, .5f, .5f);
		[Header("Level of Detail")]
		[Range(1, 32)]
		public int maxLOD = 1;
		[Range(-15, 15)]
		public int LODBias = 8;

		[Header("Model color palette")]
		public Color[] palette;
		public bool EnablesTransparent = true;

		[Header("Voxel frames")]
		public List<VoxelData> voxelFrames = new List<VoxelData>();

		[Header("Meshes attached to model")]
		public List<MeshLODs> meshes = new List<MeshLODs>();

		[Header("Extension Chunks")]
		public List<MaterialChunk> materialChunks = new List<MaterialChunk>();
		public List<TransformNodeChunk> transformNodeChunks = new List<TransformNodeChunk>();
		public List<GroupNodeChunk> groupNodeChunks = new List<GroupNodeChunk>();
		public List<ShapeNodeChunk> shapeNodeChunks = new List<ShapeNodeChunk>();
		public List<LayerChunk> layerChunks = new List<LayerChunk>();
		public List<RendererSettingChunk> rendererSettingChunks = new List<RendererSettingChunk>();
#if UNITY_EDITOR
		[HideInInspector]
		public string modelSource = string.Empty;
#endif

		public void SetAlphaFromTranparency() {
			for (int i = 0, count = Mathf.Min(palette.Length, materialChunks.Count); i < count; i++) {
				palette[i].a = materialChunks[i].Alpha;
			}
		}
	}
}
