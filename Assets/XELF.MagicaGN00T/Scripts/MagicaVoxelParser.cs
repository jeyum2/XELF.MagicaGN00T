using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GN00T.MagicaUnity {
	/// <summary>
	/// Parses a magicavoxel file
	/// </summary>
	public sealed class MagicaVoxelParser {
		#region Chunk names
		private const string HEADER = "VOX ";
		private const string MAIN = "MAIN";
		private const string SIZE = "SIZE";
		private const string XYZI = "XYZI";
		private const string RGBA = "RGBA";
		private const string MATT = "MATT";
		private const string PACK = "PACK";

		private const string nTRN = "nTRN";
		private const string nGRP = "nGRP";
		private const string nSHP = "nSHP";
		private const string LAYR = "LAYR";
		private const string MATL = "MATL";
		private const string rOBJ = "rOBJ";
		#endregion

		private const int VERSION = 150;
		private int childCount = 0;
		public static readonly Quaternion toUnity = Quaternion.AngleAxis(90, Vector3.right);

		public MagicaVoxelParser() { }

		public delegate void Logger(string message);

		public bool LoadModel(string absolutePath, VoxModel output, Logger logger) {
			var name = Path.GetFileNameWithoutExtension(absolutePath);
			logger?.Invoke("load: " + name);
			//Load the whole file
			using (var reader = new BinaryReader(new MemoryStream(File.ReadAllBytes(absolutePath)))) {
				var head = new string(reader.ReadChars(4));
				if (!head.Equals(HEADER)) {
					Debug.LogError("Not a MagicaVoxel File!", output);
					return false;
				}
				int version = reader.ReadInt32();
				if (version != VERSION)
					Debug.LogWarning("Version number:" + version + " Was designed for " + VERSION);
				ResetModel(output);
				childCount = 0;
				while (reader.BaseStream.Position != reader.BaseStream.Length)
					ReadChunk(reader, output);
			}
			if (output.palette == null)
				output.palette = LoadDefaultPalette();

			output.SetAlphaFromTranparency();

			var mesher = new VoxMesher();
			for (int i = 0; i < output.voxelFrames.Count; i++) {
				var frame = output.voxelFrames[i];
				logger?.Invoke($"frame={i}/{output.voxelFrames.Count}: {name}");
				if (!output.Settings.EnablesTransparent || !frame.Any(output.palette, c => c.a < 1)) {
					var list = new MeshLODs(new Mesh { name = $"{i}.opaque" });
					mesher.MeshVoxelData(output, frame, list.LODs[0].opaque);
					output.meshes.Add(list);
				} else {
					var list = new MeshLODs(
						new Mesh { name = $"{i}.opaque", },
						new Mesh { name = $"{i}.transparent" });
					mesher.MeshVoxelData(output, frame.Where(output.palette, c => c.a >= 1), list.LODs[0].opaque);
					mesher.MeshVoxelData(output, frame.Where(output.palette, c => c.a < 1), list.LODs[0].transparent);
					output.meshes.Add(list);
				}
			}

			#region LOD supports: added by XELF
			var dataList = output.voxelFrames.ToArray();
			var sizeList = output.voxelFrames.Select(d =>
				new Int3(d.VoxelsWide, d.VoxelsTall, d.VoxelsDeep)).ToArray();
			for (int l = 1; l < output.Settings.maxLOD; l++) {
				for (int i = 0; i < output.voxelFrames.Count; i++) {
					logger?.Invoke($"frame={i}/{output.voxelFrames.Count}, lod={l}/{output.Settings.maxLOD}: {name}");
					var previous = dataList[i];
					var size = sizeList[i];
					size.X = (size.X + 1) >> 1;
					size.Y = (size.Y + 1) >> 1;
					size.Z = (size.Z + 1) >> 1;
					sizeList[i] = size;
					var scale = new Vector3(
						output.Settings.modelScale * output.voxelFrames[i].VoxelsWide / size.X,
						output.Settings.modelScale * output.voxelFrames[i].VoxelsTall / size.Y,
						output.Settings.modelScale * output.voxelFrames[i].VoxelsDeep / size.Z);
					var current = previous.ToSmaller();
					if (!output.Settings.EnablesTransparent || !current.Any(output.palette, c => c.a < 1)) {
						var m = new MeshSet { opaque = new Mesh(), };
						var materials = VoxMesher.MaterialChunkToVector4(output.materialChunks);
						mesher.MeshVoxelData(scale, current, output.palette,
							materials, output.Settings.origin, m.opaque);
						output.meshes[i].LODs.Add(m);
					} else {
						var m = new MeshSet { opaque = new Mesh(), transparent = new Mesh(), };
						var materials = VoxMesher.MaterialChunkToVector4(output.materialChunks);
						mesher.MeshVoxelData(scale, current.Where(output.palette, c => c.a >= 1), output.palette,
							materials, output.Settings.origin, m.opaque);
						mesher.MeshVoxelData(scale, current.Where(output.palette, c => c.a < 1), output.palette,
							materials, output.Settings.origin, m.transparent);
						output.meshes[i].LODs.Add(m);
					}
					dataList[i] = current;
				}
			}
			#endregion
			return true;
		}

		/// <summary>
		/// Clears model data
		/// </summary>
		/// <param name="model"></param>
		private void ResetModel(VoxModel model) {
			model.palette = null;
			if (model.voxelFrames != null)
				model.voxelFrames.Clear();
			else
				model.voxelFrames = new List<VoxelData>();
			if (model.meshes != null)
				model.meshes.Clear();
			else
				model.meshes = new List<MeshLODs>();
			model.materialChunks.Clear();
			model.transformNodeChunks.Clear();
			model.groupNodeChunks.Clear();
			model.shapeNodeChunks.Clear();
			model.layerChunks.Clear();
			model.rendererSettingChunks.Clear();
		}
		/// <summary>
		/// Loads the default color palette
		/// </summary>
		private Color[] LoadDefaultPalette() {
			var colorCount = default_palette.Length;
			var result = new Color[256];
			byte r, g, b, a;
			for (int i = 0; i < colorCount; i++) {
				var source = default_palette[i];
				r = (byte)(source & 0xff);
				g = (byte)((source >> 8) & 0xff);
				b = (byte)((source >> 16) & 0xff);
				a = (byte)((source >> 24) & 0xff);
				result[i] = new Color32(r, g, b, a);
			}
			return result;
		}
		/// <summary>
		/// palettes are offset by 1
		/// </summary>
		/// <param name="palette"></param>
		/// <returns></returns>
		private Color[] LoadPalette(BinaryReader cr) {
			var result = new Color[256];
			for (int i = 1; i < 256; i++)
				result[i] = new Color32(cr.ReadByte(), cr.ReadByte(), cr.ReadByte(), cr.ReadByte());
			return result;
		}

		#region vox extension, etc. : added by XELF

		private struct Int3 {
			public int X, Y, Z;
			public Int3(int x, int y, int z) { X = x; Y = y; Z = z; }
		}

		private static string ReadSTRING(BinaryReader reader) {
			var size = reader.ReadInt32();
			var bytes = reader.ReadBytes(size);
			return System.Text.Encoding.UTF8.GetString(bytes);
		}
		private delegate T ItemReader<T>(BinaryReader reader);
		private static T[] ReadArray<T>(BinaryReader reader, ItemReader<T> itemReader) =>
			Enumerable.Range(0, reader.ReadInt32())
				.Select(i => itemReader(reader)).ToArray();
		private static KeyValue[] ReadDICT(BinaryReader reader) {
			return Enumerable.Range(0, reader.ReadInt32())
				.Select(i => new KeyValue {
					Key = ReadSTRING(reader),
					Value = ReadSTRING(reader),
				}).ToArray();
		}
		private static MaterialChunk ReadMaterialChunk(BinaryReader reader) =>
			new MaterialChunk {
				id = reader.ReadInt32(),
				properties = ReadDICT(reader),
			};
		private static TransformNodeChunk ReadTransformNodeChunk(BinaryReader reader) =>
			new TransformNodeChunk {
				id = reader.ReadInt32(),
				attributes = ReadDICT(reader),
				childId = reader.ReadInt32(),
				reservedId = reader.ReadInt32(),
				layerId = reader.ReadInt32(),
				frameAttributes = ReadArray(reader, r => new DICT(ReadDICT(r))),
			};

		private static GroupNodeChunk ReadGroupNodeChunk(BinaryReader reader) =>
			new GroupNodeChunk {
				id = reader.ReadInt32(),
				attributes = ReadDICT(reader),
				childIds = ReadArray(reader, r => r.ReadInt32()),
			};
		private static ShapeNodeChunk ReadShapeNodeChunk(BinaryReader reader) =>
			new ShapeNodeChunk {
				id = reader.ReadInt32(),
				attributes = ReadDICT(reader),
				models = ReadArray(reader, r => new ShapeModel {
					modelId = r.ReadInt32(),
					attributes = ReadDICT(r),
				}),
			};
		private static LayerChunk ReadLayerChunk(BinaryReader reader) =>
			new LayerChunk {
				id = reader.ReadInt32(),
				attributes = ReadDICT(reader),
				unknown = reader.ReadInt32(),
			};
		private static RendererSettingChunk ReadRObjectChunk(BinaryReader reader) =>
			new RendererSettingChunk {
				//id = reader.ReadInt32(),
				attributes = ReadDICT(reader),
			};
		#endregion

		private void ReadChunk(BinaryReader reader, VoxModel output) {
			var chunkName = new string(reader.ReadChars(4));
			var chunkSize = reader.ReadInt32();
			var childChunkSize = reader.ReadInt32();
			//get current chunk bytes and process
			var chunk = reader.ReadBytes(chunkSize);
			var children = reader.ReadBytes(childChunkSize);
			using (var chunkReader = new BinaryReader(new MemoryStream(chunk))) {
				//Debug.Log(chunkName);

				switch (chunkName) {
					case MAIN:
						break;
					case SIZE:
						int w = chunkReader.ReadInt32();
						int h = chunkReader.ReadInt32();
						int d = chunkReader.ReadInt32();
						if (childCount >= output.voxelFrames.Count)
							output.voxelFrames.Add(new VoxelData());
						output.voxelFrames[childCount].Resize(w, d, h);
						childCount++;
						break;
					case XYZI:
						var voxelCount = chunkReader.ReadInt32();
						var frame = output.voxelFrames[childCount - 1];
						byte x, y, z;
						for (int i = 0; i < voxelCount; i++) {
							x = chunkReader.ReadByte();
							y = chunkReader.ReadByte();
							z = chunkReader.ReadByte();
							frame.Set(x, z, y, chunkReader.ReadByte());
						}
						break;
					case RGBA:
						output.palette = LoadPalette(chunkReader);
						break;
					case MATT:
						break;
					case PACK:
						int frameCount = chunkReader.ReadInt32();
						for (int i = 0; i < frameCount; i++)
							output.voxelFrames.Add(new VoxelData());
						break;

					#region Vox extension: added by XELF

					case nTRN:
						output.transformNodeChunks.Add(ReadTransformNodeChunk(chunkReader));
						break;
					case nGRP:
						output.groupNodeChunks.Add(ReadGroupNodeChunk(chunkReader));
						break;
					case nSHP:
						output.shapeNodeChunks.Add(ReadShapeNodeChunk(chunkReader));
						break;
					case LAYR:
						output.layerChunks.Add(ReadLayerChunk(chunkReader));
						break;
					case MATL:
						output.materialChunks.Add(ReadMaterialChunk(chunkReader));
						break;
					case rOBJ:
						output.rendererSettingChunks.Add(ReadRObjectChunk(chunkReader));
						break;

					#endregion

					default:
						Debug.LogError($"Unknown chunk: \"{chunkName}\"");
						break;
				}
			}
			//read child chunks
			using (var childReader = new BinaryReader(new MemoryStream(children))) {
				while (childReader.BaseStream.Position != childReader.BaseStream.Length)
					ReadChunk(childReader, output);
			}
		}
		#region Default Palette
		private static uint[] default_palette = new uint[] {
			0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff, 0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff,
			0xff6699ff, 0xff3399ff, 0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff, 0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff,
			0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff, 0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc, 0xff99cccc, 0xff66cccc, 0xff33cccc,
			0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc, 0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc, 0xff9933cc,
			0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc, 0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99,
			0xffcccc99, 0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999, 0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699,
			0xff006699, 0xffff3399, 0xffcc3399, 0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099, 0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66,
			0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66, 0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966, 0xff009966, 0xffff6666,
			0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366, 0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
			0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33, 0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933,
			0xff669933, 0xff339933, 0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333, 0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033,
			0xffcc0033, 0xff990033, 0xff660033, 0xff330033, 0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00, 0xff99cc00, 0xff66cc00, 0xff33cc00,
			0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900, 0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300, 0xff993300,
			0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000, 0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044,
			0xff000022, 0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400, 0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000,
			0xff880000, 0xff770000, 0xff550000, 0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777, 0xff555555, 0xff444444, 0xff222222, 0xff111111
		};
		#endregion
	}
}
