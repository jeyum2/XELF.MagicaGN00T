using UnityEngine;

namespace GN00T.MagicaUnity {
	[CreateAssetMenu(fileName = "VoxSettings", menuName = "Voxel/Settings")]
	public class VoxImportSettings : ScriptableObject {
		[Header("Import settings")]
		public bool asSceneGraph = true;
		public bool OmitsUVUnwrapping;
		public bool EnablesTransparent = true;
		public Material materialOpaque;
		public Material materialTransparent;
		[Header("Scale for Voxel to world coordinates")]
		public float modelScale = 0.01f;
		[Header("Origin of model scale (Incorrect for Scene Graph)")]
		[Tooltip("(.5: center, 0: left, 1: right) for each axis")]
		public Vector3 origin = new Vector3(.5f, .5f, .5f);
		[Header("Level of Detail")]
		[Range(1, 32)]
		public int maxLOD = 8;
		[Range(-15, 15)]
		public int LODBias = 8;
	}
}
