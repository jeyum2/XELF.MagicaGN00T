using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GN00T.MagicaUnity {
	//[CreateAssetMenu(fileName = "VoxModel", menuName = "Voxel/Model", order = 0)]
	public partial class VoxModel : ScriptableObject {
		[Header("Output")]
		public GameObject prefab;

		public VoxImportSettings Settings;
		[Header("Model color palette")]
		public Color[] palette;

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

		public void SetAlphaFromTranparency() {
			for (int i = 0, count = Mathf.Min(palette.Length, materialChunks.Count); i < count; i++) {
				palette[i].a = materialChunks[i].Alpha;
			}
		}
	}
	[System.Serializable]
	public struct MeshLODs {
		public List<MeshSet> LODs;

		public MeshLODs(Mesh opaque = null, Mesh tranparent = null) {
			LODs = new List<MeshSet> { new MeshSet { opaque = opaque, transparent = tranparent, }, };
		}
	}
	[System.Serializable]
	public struct MeshSet {
		public Mesh opaque;
		public Mesh transparent;

		public bool Contains(Mesh m) =>
			opaque == m || transparent == m;
	}
	public enum MaterialType {
		_diffuse, _metal, _glass, _emit
	}
	[System.Serializable]
	public class MaterialChunk { // MATL: Material Chunk
		public int id;
		public KeyValue[] properties;

		#region Getters
		public MaterialType Type {
			get {
				var result = MaterialType._diffuse;
				var item = properties.FirstOrDefault(i => i.Key == "_type");
				if (item.Key != null)
					System.Enum.TryParse(item.Value, out result);
				return result;
			}
		}
		public float Weight {
			get {
				var result = 1f;
				var item = properties.FirstOrDefault(i => i.Key == "_weight");
				if (item.Key != null)
					float.TryParse(item.Value, out result);
				return result;
			}
		}
		public float Rough {
			get {
				var result = 1f;
				var item = properties.FirstOrDefault(i => i.Key == "_rough");
				if (item.Key != null)
					float.TryParse(item.Value, out result);
				return result;
			}
		}
		public float Spec {
			get {
				var result = 1f;
				var item = properties.FirstOrDefault(i => i.Key == "_spec");
				if (item.Key != null)
					float.TryParse(item.Value, out result);
				return result;
			}
		}
		public float Flux {
			get {
				var result = 1f;
				var item = properties.FirstOrDefault(i => i.Key == "_flux");
				if (item.Key != null)
					float.TryParse(item.Value, out result);
				return result;
			}
		}
		#endregion

		#region Getters friendly for Unity Standard Shader

		public float Smoothness => 1 - Rough;
		public float Metallic => Type == MaterialType._metal ? Weight : 0;
		public float Emission => Type == MaterialType._emit ? Weight * Flux : 0;
		public float Transparency => Type == MaterialType._glass ? Weight : 0;
		public float Alpha => 1 - Transparency;

		#endregion
	}
	[System.Serializable]
	public struct KeyValue {
		public string Key, Value;
	}

	public enum NodeType { Transform, Group, Shape, }

	[System.Serializable]
	public abstract class NodeChunk {
		public int id;
		public KeyValue[] attributes;
		public abstract NodeType Type { get; }
	}

	[System.Serializable]
	public class TransformNodeChunk : NodeChunk { // nTRN: Transform Node Chunk
		public int childId;
		public int reservedId;
		public int layerId;
		public DICT[] frameAttributes;

		public override NodeType Type => NodeType.Transform;

		public ROTATION RotationAt(int frame = 0)
			=> frame < frameAttributes.Length ? frameAttributes[frame]._r : ROTATION._PX_PY_P;
		public Vector3 TranslationAt(int frame = 0)
			=> frame < frameAttributes.Length ? frameAttributes[frame]._t : Vector3.zero;

		public string Name {
			get {
				var kv = attributes.FirstOrDefault(a => a.Key == "_name");
				return kv.Key != null ? kv.Value : string.Empty;
			}
		}
		public bool Hidden {
			get {
				var kv = attributes.FirstOrDefault(a => a.Key == "_hidden");
				return kv.Key != null ? kv.Value != "0" : false;
			}
		}
	}
	[System.Serializable]
	public class DICT { // DICT
		public KeyValue[] items;

		public DICT(KeyValue[] items) {
			this.items = items;
		}
		public DICT() { }

		public ROTATION _r {
			get {
				var item = items.FirstOrDefault(i => i.Key == "_r");
				if (item.Key == null)
					return ROTATION._PX_PY_P;
				byte result;
				if (!byte.TryParse(item.Value, out result))
					return ROTATION._PX_PY_P;
				return (ROTATION)result;
			}
		}
		public Vector3 _t {
			get {
				var result = Vector3.zero;
				var item = items.FirstOrDefault(i => i.Key == "_t");
				if (item.Key == null)
					return result;
				var data = item.Value.Split(' ');
				if (data.Length > 0)
					float.TryParse(data[0], out result.x);
				if (data.Length > 1)
					float.TryParse(data[1], out result.y);
				if (data.Length > 2)
					float.TryParse(data[2], out result.z);
				return result;
			}
		}
	}
	[System.Serializable]
	public class GroupNodeChunk : NodeChunk { // nGRP: Group Node Chunk
		public int[] childIds;
		public override NodeType Type => NodeType.Group;
	}
	[System.Serializable]
	public class ShapeNodeChunk : NodeChunk { // nSHP: Shape Node Chunk
		public ShapeModel[] models;
		public override NodeType Type => NodeType.Shape;
	}
	[System.Serializable]
	public class ShapeModel {
		public int modelId;
		public KeyValue[] attributes; // reserved
	}
	[System.Flags]
	public enum ROTATION : byte { // ROTATION
		_PX_PX_P, _PY_PX_P, _PZ_PX_P, _PW_PX_P,
		_PX_PY_P, _PY_PY_P, _PZ_PY_P, _PW_PY_P,
		_PX_PZ_P, _PY_PZ_P, _PZ_PZ_P, _PW_PZ_P,
		_PX_PW_P, _PY_PW_P, _PZ_PW_P, _PW_PW_P,
		_NX_PX_P, _NY_PX_P, _NZ_PX_P, _NW_PX_P,
		_NX_PY_P, _NY_PY_P, _NZ_PY_P, _NW_PY_P,
		_NX_PZ_P, _NY_PZ_P, _NZ_PZ_P, _NW_PZ_P,
		_NX_PW_P, _NY_PW_P, _NZ_PW_P, _NW_PW_P,
		_PX_NX_P, _PY_NX_P, _PZ_NX_P, _PW_NX_P,
		_PX_NY_P, _PY_NY_P, _PZ_NY_P, _PW_NY_P,
		_PX_NZ_P, _PY_NZ_P, _PZ_NZ_P, _PW_NZ_P,
		_PX_NW_P, _PY_NW_P, _PZ_NW_P, _PW_NW_P,
		_NX_NX_P, _NY_NX_P, _NZ_NX_P, _NW_NX_P,
		_NX_NY_P, _NY_NY_P, _NZ_NY_P, _NW_NY_P,
		_NX_NZ_P, _NY_NZ_P, _NZ_NZ_P, _NW_NZ_P,
		_NX_NW_P, _NY_NW_P, _NZ_NW_P, _NW_NW_P,

		_PX_PX_N, _PY_PX_N, _PZ_PX_N, _PW_PX_N,
		_PX_PY_N, _PY_PY_N, _PZ_PY_N, _PW_PY_N,
		_PX_PZ_N, _PY_PZ_N, _PZ_PZ_N, _PW_PZ_N,
		_PX_PW_N, _PY_PW_N, _PZ_PW_N, _PW_PW_N,
		_NX_PX_N, _NY_PX_N, _NZ_PX_N, _NW_PX_N,
		_NX_PY_N, _NY_PY_N, _NZ_PY_N, _NW_PY_N,
		_NX_PZ_N, _NY_PZ_N, _NZ_PZ_N, _NW_PZ_N,
		_NX_PW_N, _NY_PW_N, _NZ_PW_N, _NW_PW_N,
		_PX_NX_N, _PY_NX_N, _PZ_NX_N, _PW_NX_N,
		_PX_NY_N, _PY_NY_N, _PZ_NY_N, _PW_NY_N,
		_PX_NZ_N, _PY_NZ_N, _PZ_NZ_N, _PW_NZ_N,
		_PX_NW_N, _PY_NW_N, _PZ_NW_N, _PW_NW_N,
		_NX_NX_N, _NY_NX_N, _NZ_NX_N, _NW_NX_N,
		_NX_NY_N, _NY_NY_N, _NZ_NY_N, _NW_NY_N,
		_NX_NZ_N, _NY_NZ_N, _NZ_NZ_N, _NW_NZ_N,
		_NX_NW_N, _NY_NW_N, _NZ_NW_N, _NW_NW_N,
	}
	[System.Serializable]
	public class LayerChunk { // LAYR: Layer Chunk
		public int id;
		public KeyValue[] attributes;
		public int unknown;

		public string Name {
			get {
				var item = attributes.FirstOrDefault(i => i.Key == "_name");
				return item.Key != null ? item.Value : string.Empty;
			}
		}
		public bool Hidden {
			get {
				var item = attributes.FirstOrDefault(i => i.Key == "_hidden");
				return item.Key != null ? item.Value != "0" : false;
			}
		}
	}
	[System.Serializable]
	public class RendererSettingChunk { // rOBJ: Renderer Setting Chunk (undocumented)
		public KeyValue[] attributes;
	}
}
