using System.Linq;
using UnityEngine;

namespace GN00T.MagicaUnity {
	/// <summary>
	/// Voxel data class contains color indices as bytes
	/// </summary>
	[System.Serializable]
	public sealed class VoxelData {
		[SerializeField]
		private int _voxelsWide, _voxelsTall, _voxelsDeep;
		[SerializeField, HideInInspector]
		private byte[] colors;
		/// <summary>
		/// Creates a new empty voxel data
		/// </summary>
		public VoxelData() { }

		/// <summary>
		/// Creates a voxeldata with provided dimensions
		/// </summary>
		/// <param name="voxelsWide"></param>
		/// <param name="voxelsTall"></param>
		/// <param name="voxelsDeep"></param>
		public VoxelData(int voxelsWide, int voxelsTall, int voxelsDeep) {
			Resize(voxelsWide, voxelsTall, voxelsDeep);
		}

		public void Resize(int voxelsWide, int voxelsTall, int voxelsDeep) {
			_voxelsWide = voxelsWide;
			_voxelsTall = voxelsTall;
			_voxelsDeep = voxelsDeep;
			colors = new byte[_voxelsWide * _voxelsTall * _voxelsDeep];
		}
		/// <summary>
		/// Gets a grid position from voxel coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public int GetGridPos(int x, int y, int z)
			=> (_voxelsWide * _voxelsTall) * z + (_voxelsWide * y) + x;

		/// <summary>
		/// Sets a color index from voxel coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <param name="value"></param>
		public void Set(int x, int y, int z, byte value)
			=> colors[GetGridPos(x, y, z)] = value;

		/// <summary>
		/// Sets a color index from grid position
		/// </summary>
		/// <param name="x"></param>
		/// <param name="value"></param>
		public void Set(int x, byte value)
			=> colors[x] = value;

		/// <summary>
		/// Gets a palette index from voxel coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public int Get(int x, int y, int z)
			=> colors[GetGridPos(x, y, z)];
		/// <summary>
		/// Gets a pa
		/// lette index from a grid position
		/// </summary>
		/// <param name="x"></param>
		/// <returns></returns>
		public byte Get(int x) => colors[x];

		/// <summary>
		/// width of the data in voxels
		/// </summary>
		public int VoxelsWide => _voxelsWide;

		/// <summary>
		/// height of the data in voxels
		/// </summary>
		public int VoxelsTall => _voxelsTall;

		/// <summary>
		/// Depth of the voxels in data
		/// </summary>
		public int VoxelsDeep => _voxelsDeep;

		/// <summary>
		/// Voxel dimension as integers
		/// </summary>
		public Vector3 VoxelDimension =>
			new Vector3(_voxelsWide, _voxelsTall, _voxelsDeep);

		#region added by XELF
		public bool Contains(int x, int y, int z)
			=> x >= 0 && y >= 0 && z >= 0 && x < _voxelsWide && y < VoxelsTall && z < VoxelsDeep;
		public byte GetSafe(int x, int y, int z)
			=> Contains(x, y, z) ? colors[GetGridPos(x, y, z)] : (byte)0;

		public delegate bool ColorPredicator(Color color);

		public VoxelData Where(Color[] palette, ColorPredicator predicate) {
			var result = new VoxelData(_voxelsWide, _voxelsTall, _voxelsDeep);
			for (int i = 0; i < colors.Length; i++) {
				if (predicate(palette[colors[i]]))
					result.colors[i] = colors[i];
			}
			return result;
		}
		public bool Any(Color[] palette, ColorPredicator predicate)
			=> colors.Any(c => predicate(palette[c]));

		public VoxelData ToSmaller() {
			var work = new byte[8];
			var result = new VoxelData(
				(_voxelsWide + 1) >> 1,
				(_voxelsTall + 1) >> 1,
				(_voxelsDeep + 1) >> 1);
			int i = 0;
			for (int z = 0; z < _voxelsDeep; z += 2) {
				var z1 = z + 1;
				for (int y = 0; y < _voxelsTall; y += 2) {
					var y1 = y + 1;
					for (int x = 0; x < _voxelsWide; x += 2) {
						var x1 = x + 1;
						work[0] = GetSafe(x, y, z);
						work[1] = GetSafe(x1, y, z);
						work[2] = GetSafe(x, y1, z);
						work[3] = GetSafe(x1, y1, z);
						work[4] = GetSafe(x, y, z1);
						work[5] = GetSafe(x1, y, z1);
						work[6] = GetSafe(x, y1, z1);
						work[7] = GetSafe(x1, y1, z1);

						if (work.Any(color => color != 0)) {
							var groups = work.Where(color => color != 0).GroupBy(v => v).OrderByDescending(v => v.Count());
							var count = groups.ElementAt(0).Count();
							var group = groups.TakeWhile(v => v.Count() == count)
								.OrderByDescending(v => v.Key).First();
							result.colors[i++] = group.Key;
						} else {
							result.colors[i++] = 0;
						}
					}
				}
			}
			return result;
		}
		#endregion
	}
}
