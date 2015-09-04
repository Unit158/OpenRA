#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using MoonSharp.Interpreter;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Utils")]
	public class UtilsGlobal : ScriptGlobal
	{
		public UtilsGlobal(ScriptContext context) : base(context) { }

		[Desc("Calls a function on every element in a collection.")]
		public void Do(DynValue[] collection, Closure func)
		{
			foreach (var c in collection)
				func.Call(c);
		}

		[Desc("Returns true if func returns true for any element in a collection.")]
		public bool Any(DynValue[] collection, Closure func)
		{
			foreach (var c in collection)
			{
				if (func.Call(c).CastToBool())
					return true;
			}

			return false;
		}

		[Desc("Returns true if func returns true for all elements in a collection.")]
		public bool All(DynValue[] collection, Closure func)
		{
			foreach (var c in collection)
			{
				if (func.Call(c).CastToBool())
					return false;
			}

			return true;
		}

		[Desc("Returns the first n values from a collection.")]
		public DynValue[] Take(int n, DynValue[] source)
		{
			return source.Take(n).ToArray();
		}

		[Desc("Skips over the first numElements members of a table and return the rest.")]
		public Table Skip(Table table, int numElements)
		{
			var t = Context.CreateTable();

			for (var i = numElements; i <= table.Length; i++)
				t.Set(t.Length + 1, table.Get(i));

			return t;
		}

		[Desc("Returns a random value from a collection.")]
		public DynValue Random(DynValue[] collection)
		{
			return collection.Random(Context.World.SharedRandom);
		}

		[Desc("Expands the given footprint one step along the coordinate axes, and (if requested) diagonals.")]
		public CPos[] ExpandFootprint(CPos[] footprint, bool allowDiagonal)
		{
			return Util.ExpandFootprint(footprint, allowDiagonal).ToArray();
		}

		[Desc("Returns a random integer x in the range low &lt;= x &lt; high.")]
		public int RandomInteger(int low, int high)
		{
			if (high <= low)
				return low;

			return Context.World.SharedRandom.Next(low, high);
		}

		[Desc("Returns the ticks formatted to HH:MM:SS.")]
		public string FormatTime(int ticks, bool leadingMinuteZero = true)
		{
			return WidgetUtils.FormatTime(ticks, leadingMinuteZero);
		}
	}
}
