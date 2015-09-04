#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using MoonSharp.Interpreter;
using OpenRA.Mods.RA.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptPropertyGroup("Support Powers")]
	public class ChronsphereProperties : ScriptActorProperties, Requires<ChronoshiftPowerInfo>
	{
		public ChronsphereProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Chronoshift a group of actors. A duration of 0 will teleport the actors permanently.")]
		public void Chronoshift(Table unitLocationPairs, int duration = 0, bool killCargo = false)
		{
			foreach (var kv in unitLocationPairs.Pairs)
			{
				Actor actor = kv.Key.UserData != null ? (Actor)kv.Key.UserData.Object : null;
				CPos? cell = kv.Value.UserData != null ? (CPos?)kv.Value.UserData.Object : null;
				//if (!kv.Key.TryGetClrValue<Actor>(out actor) || cell == null)
				//	throw new ScriptRuntimeException("Chronoshift requires a table of Actor,CPos pairs. Received {0},{1}".F(kv.Key.Type, kv.Value.Type));

				var cs = actor.TraitOrDefault<Chronoshiftable>();
				if (cs != null && cs.CanChronoshiftTo(actor, cell.Value))
					cs.Teleport(actor, cell.Value, duration, killCargo, Self);
			}
		}
	}
}