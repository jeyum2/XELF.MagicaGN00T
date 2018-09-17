using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GN00T.MagicaUnity {
	/// <summary>
	/// Converts voxel volume to mesh
	/// </summary>
	public sealed class VoxMesher {
		private List<Vector3> verts = new List<Vector3>();
		private List<Color32> colors = new List<Color32>();
		private List<Vector4> materials = new List<Vector4>();
		private List<Vector3> normals = new List<Vector3>();
		private List<int> indices = new List<int>();

		public VoxMesher() { }
		/// <summary>
		/// Clears the mesh data
		/// </summary>
		public void Reset() {
			verts.Clear();
			normals.Clear();
			indices.Clear();
			colors.Clear();
			materials.Clear();
		}

		/// <summary>
		/// Puts mesh data into passed mesh
		/// </summary>
		/// <param name="mesh"></param>
		public void SetMesh(Mesh mesh) {
			mesh.Clear();
			mesh.SetVertices(verts);
			mesh.SetTriangles(indices, 0);
			mesh.SetColors(colors);
			mesh.SetUVs(2, materials);
			mesh.SetNormals(normals);
		}
		public Mesh MeshVoxelData(VoxModel data, VoxelData frame, Mesh result) {
			var scale = data.Settings.modelScale;
			var grid = frame;
			var colorList = data.palette;
			var materialList = MaterialChunkToVector4(data.materialChunks);
			var origin = data.Settings.origin;
			return MeshVoxelData(scale * Vector3.one, grid, colorList, materialList, origin, result);
		}

		public static Vector4[] MaterialChunkToVector4(List<MaterialChunk> materialChunks) =>
			materialChunks.Select(i =>
				new Vector4(i.Smoothness, i.Emission, i.Spec, i.Metallic)).ToArray();

		private static readonly Vector4 defaultVoxMaterial = new Vector4(1f, 0f, 0f, 0f);

		/// <summary>
		/// Returns a mesh from a voxmodel 
		/// </summary>
		/// <param name="data">data to process</param>
		/// <param name="frame">frame to parse</param>
		/// <param name="result">Mesh result</param>
		/// <returns>Same mesh a result</returns>
		public Mesh MeshVoxelData(Vector3 scale, VoxelData grid, Color[] colorList,
			Vector4[] materialList,
			Vector3 origin, Mesh result) {
			var scales = new float[] { scale.x, scale.y, scale.z, };
			Reset();
			int[] x = new int[3], q = new int[3];
			int[] mask;
			byte[] colorMask;
			int[] dimensions = new int[3] { grid.VoxelsWide, grid.VoxelsTall, grid.VoxelsDeep };
			Vector3 originOffset = new Vector3(
				grid.VoxelsWide * origin.x * scale.x,
				grid.VoxelsTall * origin.y * scale.y,
				grid.VoxelsDeep * origin.z * scale.z);
			int current, next;
			//iterate through each axis
			for (int d = 0; d < 3; d++) {
				int u = (d + 1) % 3;
				int v = (d + 2) % 3;
				int l, k;
				int w, h;
				x[0] = x[1] = x[2] = 0;
				q[0] = q[1] = q[2] = 0;
				mask = new int[dimensions[u] * dimensions[v]];
				colorMask = new byte[dimensions[u] * dimensions[v]];
				q[d] = 1;
				//iterate through current axis
				for (x[d] = -1; x[d] < dimensions[d];) {
					int n = 0;
					for (x[v] = 0; x[v] < dimensions[v]; ++x[v])
						for (x[u] = 0; x[u] < dimensions[u]; ++x[u], ++n) {
							//TODO store color value
							//Determine if value change for face render
							current = (x[d] >= 0 ? grid.Get(x[0], x[1], x[2]) : 0);
							next = (x[d] < dimensions[d] - 1 ? grid.Get(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : 0);
							if (current != next) {
								if (current > 0 && next > 0) {
									mask[n] = 0;
									colorMask[n] = 0;
								} else if (current == 0) {
									mask[n] = -1;
									colorMask[n] = (byte)next;
								} else if (next == 0) {
									mask[n] = 1;
									colorMask[n] = (byte)current;
								} else {
									mask[n] = 0;
									colorMask[n] = 0;
								}
							} else {
								mask[n] = 0;
								colorMask[n] = 0;
							}
						}

					x[d] = x[d] + 1;
					//generate mesh for slice
					n = 0;
					int maskValue, colorValue;
					for (int j = 0; j < dimensions[v]; ++j)
						for (int i = 0; i < dimensions[u];) {
							maskValue = mask[n];
							colorValue = colorMask[n];
							if (maskValue != 0) {
								//width
								for (w = 1; n + w < mask.Length && mask[n + w] == maskValue && colorValue == colorMask[n + w] && i + w < dimensions[u]; ++w) {

								}
								//height
								bool done = false;
								for (h = 1; j + h < dimensions[v]; ++h) {
									for (k = 0; k < w; ++k) {
										if (mask[n + k + h * dimensions[u]] != maskValue || colorMask[n + k + h * dimensions[u]] != colorValue) {
											done = true;
											break;
										}
									}
									if (done)
										break;
								}
								//add quad
								// x[u] = i;
								//x[v] = j;
								Vector3 xp = new Vector3();
								xp[u] = i;
								xp[v] = j;
								xp[d] = x[d];
								xp.x *= scale.x;
								xp.y *= scale.y;
								xp.z *= scale.z;
								Vector3 du = new Vector3();
								du[u] = w * scales[u];
								Vector3 dv = new Vector3();
								dv[v] = h * scales[v];
								//create the face
								Add(
									new Vector3(xp[0], xp[1], xp[2]) - originOffset,
									new Vector3(xp[0] + du[0], xp[1] + du[1], xp[2] + du[2]) - originOffset,
									new Vector3(xp[0] + du[0] + dv[0], xp[1] + du[1] + dv[1], xp[2] + du[2] + dv[2]) - originOffset,
									new Vector3(xp[0] + dv[0], xp[1] + dv[1], xp[2] + dv[2]) - originOffset,
									new Vector3(q[0], q[1], q[2]),
									colorList[colorValue], colorValue < materialList.Length ? materialList[colorValue] : defaultVoxMaterial,
									maskValue < 0, verts, normals, colors, materials, indices);
								//Increment counters and continue
								//Zero-out mask
								for (l = 0; l < h; ++l)
									for (k = 0; k < w; ++k) {
										//h*dimension+x=index
										mask[(n + k) + l * dimensions[u]] = 0;
										colorMask[(n + k) + l * dimensions[u]] = 0;
									}
								//Increment counters and continue
								i += w;
								n += w;
							} else {
								++i;
								++n;
							}
						}
				}
			}
			SetMesh(result);
			return result;
		}

		/// <summary>
		/// Puts a quad into the mesh
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		/// <param name="normal"></param>
		/// <param name="color"></param>
		/// <param name="flip"></param>
		/// <param name="verts"></param>
		/// <param name="normals"></param>
		/// <param name="colors"></param>
		/// <param name="material"></param>
		/// <param name="indices"></param>
		private static void Add(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
			Vector3 normal, Color32 color, Vector4 material,
			bool flip, List<Vector3> verts, List<Vector3> normals,
			List<Color32> colors, List<Vector4> materials, List<int> indices) {
			int start = verts.Count;
			verts.Add(a);
			verts.Add(b);
			verts.Add(c);
			verts.Add(a);
			verts.Add(c);
			verts.Add(d);
			colors.Add(color);
			colors.Add(color);
			colors.Add(color);
			colors.Add(color);
			colors.Add(color);
			colors.Add(color);
			materials.Add(material);
			materials.Add(material);
			materials.Add(material);
			materials.Add(material);
			materials.Add(material);
			materials.Add(material);
			if (flip)
				normal = -normal;
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			if (flip) {
				indices.Add(start + 5);
				indices.Add(start + 4);
				indices.Add(start + 3);
				indices.Add(start + 2);
				indices.Add(start + 1);
				indices.Add(start);
			} else {
				indices.Add(start);
				indices.Add(start + 1);
				indices.Add(start + 2);
				indices.Add(start + 3);
				indices.Add(start + 4);
				indices.Add(start + 5);
			}
		}
	}
}
