#if ENABLE_BURST
using Unity.Burst;
#endif

using System.Runtime.CompilerServices;

namespace XericLibrary.Runtime.MacroLibrary
{
	/// <summary>
	/// 空间填充曲线映射——提供 Z 字曲线(Morton Code) 与 Hilbert 曲线的 2D/3D 坐标与 1D 索引的映射工具
	/// 所有方法支持 Burst 编译，可用于 Unity 多线程环境
	/// </summary>
#if ENABLE_BURST
	[BurstCompile]
#endif
	public static class MacroCurveMapping
	{
		#region Z 字曲线 (Morton Code)

		/// <summary>
		/// 将 2D 块坐标交织为 1D Z 索引 (Morton Code)
		/// </summary>
		/// <param name="blockX">X 坐标 (非负)</param>
		/// <param name="blockY">Y 坐标 (非负)</param>
		/// <returns>64 位 Z 索引</returns>
#if ENABLE_BURST
		[BurstCompile]
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ZOrderEncode(int blockX, int blockY)
		{
			ulong x = (uint)blockX;
			ulong y = (uint)blockY;

			x = (x | (x << 16)) & 0x0000FFFF0000FFFF;
			x = (x | (x << 8)) & 0x00FF00FF00FF00FF;
			x = (x | (x << 4)) & 0x0F0F0F0F0F0F0F0F;
			x = (x | (x << 2)) & 0x3333333333333333;
			x = (x | (x << 1)) & 0x5555555555555555;

			y = (y | (y << 16)) & 0x0000FFFF0000FFFF;
			y = (y | (y << 8)) & 0x00FF00FF00FF00FF;
			y = (y | (y << 4)) & 0x0F0F0F0F0F0F0F0F;
			y = (y | (y << 2)) & 0x3333333333333333;
			y = (y | (y << 1)) & 0x5555555555555555;

			return x | (y << 1);
		}

		/// <summary>
		/// 将 1D Z 索引解码还原为 2D 块坐标
		/// </summary>
		/// <param name="z">Z 索引</param>
		/// <param name="blockX">解码后的 X 坐标</param>
		/// <param name="blockY">解码后的 Y 坐标</param>
#if ENABLE_BURST
		[BurstCompile]
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ZOrderDecode(ulong z, out int blockX, out int blockY)
		{
			ulong x = z & 0x5555555555555555;
			ulong y = (z >> 1) & 0x5555555555555555;

			x = (x ^ (x >> 1)) & 0x3333333333333333;
			x = (x ^ (x >> 2)) & 0x0F0F0F0F0F0F0F0F;
			x = (x ^ (x >> 4)) & 0x00FF00FF00FF00FF;
			x = (x ^ (x >> 8)) & 0x0000FFFF0000FFFF;
			x = (x ^ (x >> 16)) & 0x00000000FFFFFFFF;

			y = (y ^ (y >> 1)) & 0x3333333333333333;
			y = (y ^ (y >> 2)) & 0x0F0F0F0F0F0F0F0F;
			y = (y ^ (y >> 4)) & 0x00FF00FF00FF00FF;
			y = (y ^ (y >> 8)) & 0x0000FFFF0000FFFF;
			y = (y ^ (y >> 16)) & 0x00000000FFFFFFFF;

			blockX = (int)x;
			blockY = (int)y;
		}

		/// <summary>
		/// 将 3D 块坐标编码为 1D Z 索引 (Morton Code)
		/// </summary>
		/// <param name="x">X 坐标 (非负, 最多 21 bit)</param>
		/// <param name="y">Y 坐标 (非负, 最多 21 bit)</param>
		/// <param name="z">Z 坐标 (非负, 最多 21 bit)</param>
		/// <returns>64 位 Z 索引</returns>
#if ENABLE_BURST
		[BurstCompile]
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ZOrderEncode3D(int x, int y, int z)
		{
			return SplitBy3((uint)x) | (SplitBy3((uint)y) << 1) | (SplitBy3((uint)z) << 2);
		}

		/// <summary>
		/// 将 1D Z 索引解码还原为 3D 坐标
		/// </summary>
#if ENABLE_BURST
		[BurstCompile]
#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ZOrderDecode3D(ulong code, out int x, out int y, out int z)
		{
			x = (int)CompactBy3((uint)(code));
			y = (int)CompactBy3((uint)(code >> 1));
			z = (int)CompactBy3((uint)(code >> 2));
		}

		/// <summary>
		/// 将 21-bit 整数按位分散到 63-bit 空间 (每 3 位留 1 位)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong SplitBy3(uint a)
		{
			ulong x = (ulong)a & 0x1fffffUL; // 仅保留低 21 位
			x = (x | (x << 32)) & 0x1f00000000ffffUL;
			x = (x | (x << 16)) & 0x1f0000ff0000ffUL;
			x = (x | (x << 8)) & 0x100f00f00f00f00fUL;
			x = (x | (x << 4)) & 0x10c30c30c30c30c3UL;
			x = (x | (x << 2)) & 0x1249249249249249UL;
			return x;
		}

		/// <summary>
		/// CompactBy3: 反向提取分散的位，还原为原始 21-bit 整数
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint CompactBy3(uint a)
		{
			ulong x = (ulong)a & 0x1249249249249249UL;
			x = (x ^ (x >> 2)) & 0x10c30c30c30c30c3UL;
			x = (x ^ (x >> 4)) & 0x100f00f00f00f00fUL;
			x = (x ^ (x >> 8)) & 0x1f0000ff0000ffUL;
			x = (x ^ (x >> 16)) & 0x1f00000000ffffUL;
			x = (x ^ (x >> 32)) & 0x1fffffUL;
			return (uint)x;
		}

		#endregion

		#region Hilbert 曲线 (2D)

		// 2D Hilbert 状态转移表: encodeTable[state][rx*2+ry] = (quadrant << 2) | nextState
		private static readonly byte[] Hilbert2DEncodeTable = new byte[16]
		{
			0x00, 0x01, 0x0E, 0x0B, // state 0: (0,0)→0/0, (0,1)→1/0, (1,0)→3/3, (1,1)→2/3
			0x08, 0x05, 0x06, 0x03, // state 1: (0,0)→2/1, (0,1)→1/1, (1,0)→1/2, (1,1)→0/3
			0x08, 0x0D, 0x04, 0x01, // state 2: (0,0)→2/2, (0,1)→3/2, (1,0)→1/1, (1,1)→0/0
			0x00, 0x0D, 0x07, 0x0A, // state 3: (0,0)→0/3, (0,1)→3/0, (1,0)→3/3, (1,1)→2/2
		};

		// 2D Hilbert 解码用: decodeTable[state][quadrant] = (rx << 1 | ry << 0) | (nextState << 2)
		private static readonly byte[] Hilbert2DDecodeTable = new byte[16]
		{
			0x00, 0x01, 0x0B, 0x06, // state 0: 0→(0,0)/0, 1→(0,1)/0, 2→(1,1)/2, 3→(1,0)/3
			0x08, 0x05, 0x03, 0x06, // state 1: 0→(1,1)/2, 1→(0,1)/1, 2→(0,0)/0, 3→(1,0)/3
			0x0A, 0x07, 0x01, 0x04, // state 2: 0→(1,1)/2, 1→(1,0)/3, 2→(0,0)/0, 3→(0,1)/1
			0x00, 0x0D, 0x0B, 0x06, // state 3: 0→(0,0)/0, 1→(1,0)/3, 2→(1,1)/2, 3→(0,1)/1
		};

		/// <summary>
		/// 将 2D 坐标编码为 Hilbert 曲线索引
		/// </summary>
		/// <param name="x">X 坐标 (非负)</param>
		/// <param name="y">Y 坐标 (非负)</param>
		/// <param name="order">曲线阶数 (1~16), 决定网格大小为 2^order</param>
		/// <returns>Hilbert 索引</returns>
#if ENABLE_BURST
		[BurstCompile]
#endif
		public static ulong Hilbert2DEncode(int x, int y, int order)
		{
			ulong index = 0;
			int state = 0;
			for (int s = order - 1; s >= 0; s--)
			{
				int rx = (x >> s) & 1;
				int ry = (y >> s) & 1;
				int key = (state << 2) | (rx << 1) | ry;
				byte entry = Hilbert2DEncodeTable[key];
				int quadrant = entry & 3;
				state = entry >> 2;
				index = (index << 2) | (uint)quadrant;
			}
			return index;
		}

		/// <summary>
		/// 将 Hilbert 索引解码为 2D 坐标
		/// </summary>
		/// <param name="index">Hilbert 索引</param>
		/// <param name="order">曲线阶数</param>
		/// <param name="x">解码后的 X 坐标</param>
		/// <param name="y">解码后的 Y 坐标</param>
#if ENABLE_BURST
		[BurstCompile]
#endif
		public static void Hilbert2DDecode(ulong index, int order, out int x, out int y)
		{
			x = 0;
			y = 0;
			int state = 0;
			for (int s = 0; s < order; s++)
			{
				int quadrant = (int)((index >> ((order - 1 - s) * 2)) & 3);
				int key = (state << 2) | quadrant;
				byte entry = Hilbert2DDecodeTable[key];
				int rx = (entry >> 1) & 1;
				int ry = entry & 1;
				state = entry >> 2;
				x = (x << 1) | rx;
				y = (y << 1) | ry;
			}
		}

		#endregion

		#region Hilbert 曲线 (3D)

		// 3D Hilbert 编码状态转移表: encode3DTable[state][rx*4+ry*2+rz] = (octant << 3) | nextState
		// state 索引原始坐标位(rx,ry,rz), 返回:高4位为octant,低3位为nextState
		private static readonly byte[] Hilbert3DEncodeTable = new byte[64]
		{
			// state 0
			0x00, 0x01, 0x13, 0x12, 0x3F, 0x36, 0x24, 0x2D,
			// state 1
			0x09, 0x08, 0x1B, 0x1A, 0x37, 0x3E, 0x2C, 0x25,
			// state 2
			0x1B, 0x1A, 0x08, 0x09, 0x2C, 0x25, 0x37, 0x3E,
			// state 3
			0x12, 0x13, 0x01, 0x00, 0x24, 0x2D, 0x3F, 0x36,
			// state 4
			0x2B, 0x32, 0x38, 0x31, 0x04, 0x05, 0x17, 0x16,
			// state 5
			0x3A, 0x33, 0x21, 0x28, 0x0C, 0x0D, 0x1F, 0x1E,
			// state 6
			0x20, 0x29, 0x3A, 0x33, 0x15, 0x1C, 0x0C, 0x05,
			// state 7
			0x29, 0x20, 0x33, 0x3A, 0x1D, 0x14, 0x04, 0x0D,
		};

		// 3D Hilbert 解码状态转移表: decode3DTable[state][octant] = (rx<<2|ry<<1|rz) | (nextState << 3)
		private static readonly byte[] Hilbert3DDecodeTable = new byte[64]
		{
			// state 0
			0x00, 0x01, 0x1B, 0x13, 0x36, 0x37, 0x25, 0x2C,
			// state 1
			0x09, 0x10, 0x1A, 0x03, 0x3E, 0x2F, 0x24, 0x35,
			// state 2
			0x1B, 0x02, 0x08, 0x11, 0x25, 0x3C, 0x3F, 0x26,
			// state 3
			0x12, 0x0B, 0x01, 0x18, 0x2D, 0x2C, 0x36, 0x27,
			// state 4
			0x2B, 0x2A, 0x39, 0x30, 0x04, 0x0D, 0x1F, 0x16,
			// state 5
			0x32, 0x23, 0x38, 0x21, 0x0C, 0x15, 0x1E, 0x07,
			// state 6
			0x38, 0x29, 0x2A, 0x23, 0x1D, 0x04, 0x0D, 0x16,
			// state 7
			0x21, 0x30, 0x23, 0x2A, 0x15, 0x0C, 0x0D, 0x0E,
		};

		/// <summary>
		/// 将 3D 坐标编码为 Hilbert 曲线索引
		/// </summary>
		/// <param name="x">X 坐标 (非负)</param>
		/// <param name="y">Y 坐标 (非负)</param>
		/// <param name="z">Z 坐标 (非负)</param>
		/// <param name="order">曲线阶数 (1~10), 决定网格大小为 2^order</param>
		/// <returns>Hilbert 索引</returns>
#if ENABLE_BURST
		[BurstCompile]
#endif
		public static ulong Hilbert3DEncode(int x, int y, int z, int order)
		{
			ulong index = 0;
			int state = 0;
			for (int s = order - 1; s >= 0; s--)
			{
				int rx = (x >> s) & 1;
				int ry = (y >> s) & 1;
				int rz = (z >> s) & 1;
				int key = (state << 3) | (rx << 2) | (ry << 1) | rz;
				byte entry = Hilbert3DEncodeTable[key];
				int octant = entry >> 3;
				state = entry & 7;
				index = (index << 3) | (uint)octant;
			}
			return index;
		}

		/// <summary>
		/// 将 Hilbert 索引解码为 3D 坐标
		/// </summary>
		/// <param name="index">Hilbert 索引</param>
		/// <param name="order">曲线阶数</param>
		/// <param name="x">解码后的 X 坐标</param>
		/// <param name="y">解码后的 Y 坐标</param>
		/// <param name="z">解码后的 Z 坐标</param>
#if ENABLE_BURST
		[BurstCompile]
#endif
		public static void Hilbert3DDecode(ulong index, int order, out int x, out int y, out int z)
		{
			x = 0;
			y = 0;
			z = 0;
			int state = 0;
			for (int s = 0; s < order; s++)
			{
				int octant = (int)((index >> ((order - 1 - s) * 3)) & 7);
				int key = (state << 3) | octant;
				byte entry = Hilbert3DDecodeTable[key];
				int rx = (entry >> 2) & 1;
				int ry = (entry >> 1) & 1;
				int rz = entry & 1;
				state = entry >> 3;
				x = (x << 1) | rx;
				y = (y << 1) | ry;
				z = (z << 1) | rz;
			}
		}

		#endregion
	}
}
