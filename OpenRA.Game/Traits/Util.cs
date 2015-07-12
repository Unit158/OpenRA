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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Support;

namespace OpenRA.Traits
{
	public static class Util
	{
		public static int TickFacing(int facing, int desiredFacing, int rot)
		{
			var leftTurn = (facing - desiredFacing) & 0xFF;
			var rightTurn = (desiredFacing - facing) & 0xFF;
			if (Math.Min(leftTurn, rightTurn) < rot)
				return desiredFacing & 0xFF;
			else if (rightTurn < leftTurn)
				return (facing + rot) & 0xFF;
			else
				return (facing - rot) & 0xFF;
		}

		public static int GetFacing(WVec d, int currentFacing)
		{
			if (d.LengthSquared == 0)
				return currentFacing;

			// OpenRA defines north as -y, so invert
			var angle = WAngle.ArcTan(-d.Y, d.X, 4).Angle;

			// Convert back to a facing
			return (angle / 4 - 0x40) & 0xFF;
		}

		public static int GetNearestFacing(int facing, int desiredFacing)
		{
			var turn = desiredFacing - facing;
			if (turn > 128)
				turn -= 256;
			if (turn < -128)
				turn += 256;

			return facing + turn;
		}

		public static int QuantizeFacing(int facing, int numFrames)
		{
			var step = 256 / numFrames;
			var a = (facing + step / 2) & 0xff;
			return a / step;
		}

		public static WPos BetweenCells(World w, CPos from, CPos to)
		{
			return WPos.Lerp(w.Map.CenterOfCell(from), w.Map.CenterOfCell(to), 1, 2);
		}

		public static Activity SequenceActivities(params Activity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue(next); return a; });
		}

		public static Activity RunActivity(Actor self, Activity act)
		{
			// Note - manual iteration here for performance due to high call volume.
			var longTickThresholdInStopwatchTicks = PerfTimer.LongTickThresholdInStopwatchTicks;
			var start = Stopwatch.GetTimestamp();
			while (act != null)
			{
				var prev = act;
				act = act.Tick(self);
				var current = Stopwatch.GetTimestamp();
				if (current - start > longTickThresholdInStopwatchTicks)
				{
					PerfTimer.LogLongTick(start, current, "Activity", prev);
					start = Stopwatch.GetTimestamp();
				}
				else
					start = current;

				if (prev == act)
					break;
			}

			return act;
		}

		/* pretty crap */
		public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> ts, MersenneTwister random)
		{
			var items = ts.ToList();
			while (items.Count > 0)
			{
				var t = items.Random(random);
				yield return t;
				items.Remove(t);
			}
		}

		static IEnumerable<CPos> Neighbours(CPos c, bool allowDiagonal)
		{
			yield return c;
			yield return new CPos(c.X - 1, c.Y);
			yield return new CPos(c.X + 1, c.Y);
			yield return new CPos(c.X, c.Y - 1);
			yield return new CPos(c.X, c.Y + 1);

			if (allowDiagonal)
			{
				yield return new CPos(c.X - 1, c.Y - 1);
				yield return new CPos(c.X + 1, c.Y - 1);
				yield return new CPos(c.X - 1, c.Y + 1);
				yield return new CPos(c.X + 1, c.Y + 1);
			}
		}

		public static IEnumerable<CPos> ExpandFootprint(IEnumerable<CPos> cells, bool allowDiagonal)
		{
			return cells.SelectMany(c => Neighbours(c, allowDiagonal)).Distinct();
		}

		public static IEnumerable<CPos> AdjacentCells(World w, Target target)
		{
			var cells = target.Positions.Select(p => w.Map.CellContaining(p)).Distinct();
			return ExpandFootprint(cells, true);
		}

		public static int ApplyPercentageModifiers(int number, IEnumerable<int> percentages)
		{
			// See the comments of PR#6079 for a faster algorithm if this becomes a performance bottleneck
			var a = (decimal)number;
			foreach (var p in percentages)
				a *= p / 100m;

			return (int)a;
		}

		// Algorithm obtained from ftp://ftp.isc.org/pub/usenet/comp.sources.unix/volume26/line3d
		public static IEnumerable<CPos> Raycast(Map map, WPos source, WPos target)
		{
			if(map.TileShape == TileShape.Diamond)
			{
				var angle = new WAngle(-45);
				source = new WPos(
					(source.X * angle.Cos()) - (source.Y * angle.Sin()),
					(source.X * angle.Sin()) + (source.Y * angle.Cos()), 0);
				target = new WPos(
					(target.X * angle.Cos()) - (target.Y * angle.Sin()),
					(target.X * angle.Sin()) + (target.Y * angle.Cos()), 0);
			}

			var rCell = map.CellContaining(source);

			var delta = new WPos(
				target.X - source.X,
				target.Y - source.Y, 0);

			var absolute = new WPos(
				Math.Abs(delta.X),
				Math.Abs(delta.Y), 0);

			var cellBoundDelta = new WPos(
				delta.X > 0 ? 0 : Exts.ISqrt(1 + (delta.Y * delta.Y) / (delta.X * delta.X)),
				delta.Y > 0 ? 0 : Exts.ISqrt(1 + (delta.X * delta.X) / (delta.Y * delta.Y)), 0);

			var error = new WPos(
				delta.X < sideDistX ? (rayPosX - map.) * deltaDistX : (mapX + 1.0 - rayPosX) * deltaDistX
				, 0);

			var xIncrement = Math.Sign(yDelta);
			var yIncrement = Math.Sign(yDelta);

			var x = source.X;
			var y = source.Y;

			if (xAbsolute >= yAbsolute)
			{
				var error = yAbsolute - (xAbsolute >> 1);

				do
				{
					yield return map.CellContaining(new WPos(x, y, 0));

					if (error >= 0)
					{
						y += yIncrement;
						error -= xAbsolute;
					}

					x += xIncrement;
					error += yAbsolute;
				}
				while (y != target.Y);

				yield return map.CellContaining(new WPos(x, y, 0));
			}
			else
			{
				var error = xAbsolute - (yAbsolute >> 1);
				do
				{
					yield return map.CellContaining(new WPos(x, y, 0));

					if (error >= 0)
					{
						x += xIncrement;
						error -= yAbsolute;
					}

					y += yIncrement;
					error += xAbsolute;
				}
				while (y != target.Y);

				yield return map.CellContaining(new WPos(x, y, 0));
			}
		}
	}
}
