#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using MoonSharp.Interpreter;
using OpenRA.Scripting;
using OpenRA.Support;

namespace OpenRA
{
	/// <summary>
	/// 1d world distance - 1024 units = 1 cell.
	/// </summary>
	
	[MoonSharpUserData]
	public struct WDist : IComparable, IComparable<WDist>, IEquatable<WDist>, IScriptBindable
	{
		public readonly int Length;
		public long LengthSquared { get { return (long)Length * (long)Length; } }

		public WDist(int r) { Length = r; }
		public static readonly WDist Zero = new WDist(0);
		public static WDist FromCells(int cells) { return new WDist(1024 * cells); }

		public static WDist operator +(WDist a, WDist b) { return new WDist(a.Length + b.Length); }
		public static WDist operator -(WDist a, WDist b) { return new WDist(a.Length - b.Length); }
		public static WDist operator -(WDist a) { return new WDist(-a.Length); }
		public static WDist operator /(WDist a, int b) { return new WDist(a.Length / b); }
		public static WDist operator *(WDist a, int b) { return new WDist(a.Length * b); }
		public static WDist operator *(int a, WDist b) { return new WDist(a * b.Length); }
		public static bool operator <(WDist a, WDist b) { return a.Length < b.Length; }
		public static bool operator >(WDist a, WDist b) { return a.Length > b.Length; }
		public static bool operator <=(WDist a, WDist b) { return a.Length <= b.Length; }
		public static bool operator >=(WDist a, WDist b) { return a.Length >= b.Length; }

		public static bool operator ==(WDist me, WDist other) { return me.Length == other.Length; }
		public static bool operator !=(WDist me, WDist other) { return !(me == other); }

		// Sampled a N-sample probability density function in the range [-1024..1024]
		// 1 sample produces a rectangular probability
		// 2 samples produces a triangular probability
		// ...
		// N samples approximates a true gaussian
		public static WDist FromPDF(MersenneTwister r, int samples)
		{
			return new WDist(Exts.MakeArray(samples, _ => r.Next(-1024, 1024))
				.Sum() / samples);
		}

		public static bool TryParse(string s, out WDist result)
		{
			result = WDist.Zero;

			if (string.IsNullOrEmpty(s))
				return false;

			s = s.ToLowerInvariant();
			var components = s.Split('c');
			var cell = 0;
			var subcell = 0;

			switch (components.Length)
			{
				case 2:
					if (!Exts.TryParseIntegerInvariant(components[0], out cell) ||
						!Exts.TryParseIntegerInvariant(components[1], out subcell))
						return false;
					break;
				case 1:
					if (!Exts.TryParseIntegerInvariant(components[0], out subcell))
						return false;
					break;
				default: return false;
			}

			// Propagate sign to fractional part
			if (cell < 0)
				subcell = -subcell;

			result = new WDist(1024 * cell + subcell);
			return true;
		}

		public override int GetHashCode() { return Length.GetHashCode(); }

		public bool Equals(WDist other) { return other == this; }
		public override bool Equals(object obj) { return obj is WDist && Equals((WDist)obj); }

		public int CompareTo(object obj)
		{
			if (!(obj is WDist))
				return 1;
			return Length.CompareTo(((WDist)obj).Length);
		}

		public int CompareTo(WDist other) { return Length.CompareTo(other.Length); }

		public override string ToString() { return Length.ToString(); }

		public DynValue this[Script runtime, DynValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "Length": return DynValue.FromObject(runtime, Length);
					case "Range": Game.Debug("WRange.Range is deprecated. Use WDist.Length instead"); return DynValue.FromObject(runtime, Length);
					default: throw new ScriptRuntimeException("WDist does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new ScriptRuntimeException("WDist is read-only. Use WDist.New to create a new value");
			}
		}
	}
}
