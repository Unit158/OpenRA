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
using System.Linq;
using MoonSharp.Interpreter;
using OpenRA.Activities;
using OpenRA.Effects;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Reinforcements")]
	public class ReinforcementsGlobal : ScriptGlobal
	{
		public ReinforcementsGlobal(ScriptContext context) : base(context) { }

		Actor CreateActor(Player owner, string actorType, bool addToWorld, CPos? entryLocation = null, CPos? nextLocation = null)
		{
			ActorInfo ai;
			if (!Context.World.Map.Rules.Actors.TryGetValue(actorType, out ai))
				throw new ScriptRuntimeException("Unknown actor type '{0}'".F(actorType));

			var initDict = new TypeDictionary();

			initDict.Add(new OwnerInit(owner));

			if (entryLocation.HasValue)
			{
				var pi = ai.Traits.GetOrDefault<AircraftInfo>();
				initDict.Add(new CenterPositionInit(owner.World.Map.CenterOfCell(entryLocation.Value) + new WVec(0, 0, pi != null ? pi.CruiseAltitude.Length : 0)));
				initDict.Add(new LocationInit(entryLocation.Value));
			}

			if (entryLocation.HasValue && nextLocation.HasValue)
				initDict.Add(new FacingInit(Context.World.Map.FacingBetween(CPos.Zero, CPos.Zero + (nextLocation.Value - entryLocation.Value), 0)));

			var actor = Context.World.CreateActor(addToWorld, actorType, initDict);

			return actor;
		}

		void Move(Actor actor, CPos dest)
		{
			var move = actor.TraitOrDefault<IMove>();
			if (move == null)
				return;

			actor.QueueActivity(move.MoveTo(dest, 2));
		}

		[Desc("Send reinforcements consisting of multiple units. Supports ground-based, naval and air units. " +
			"The first member of the entryPath array will be the units' spawnpoint, " +
			"while the last one will be their destination. If actionFunc is given, " +
			"it will be executed once a unit has reached its destination. actionFunc " +
			"will be called as actionFunc(Actor actor)")]
		public Actor[] Reinforce(Player owner, string[] actorTypes, CPos[] entryPath, int interval = 25, Closure actionFunc = null)
		{
			var actors = new List<Actor>();
			for (var i = 0; i < actorTypes.Length; i++)
			{
				var actor = CreateActor(owner, actorTypes[i], false, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
				actors.Add(actor);

				var actionDelay = i * interval;
				Action actorAction = () =>
				{
					Context.World.Add(actor);
					for (var j = 1; j < entryPath.Length; j++)
						Move(actor, entryPath[j]);

					if (actionFunc != null)
					{
						actor.QueueActivity(new CallFunc(() =>
						{
							actionFunc.Call(actor);
						}));
					}
				};

				Context.World.AddFrameEndTask(w => w.Add(new DelayedAction(actionDelay, actorAction)));
			}

			return actors.ToArray();
		}

		[Desc("Send reinforcements in a transport. A transport can be a ground unit (APC etc.), ships and aircraft. " +
			"The first member of the entryPath array will be the spawnpoint for the transport, " +
			"while the last one will be its destination. The last member of the exitPath array " +
			"is be the place where the transport will be removed from the game. When the transport " +
			"has reached the destination, it will unload its cargo unless a custom actionFunc has " +
			"been supplied. Afterwards, the transport will follow the exitPath and leave the map, " +
			"unless a custom exitFunc has been supplied. actionFunc will be called as " +
			"actionFunc(Actor transport, Actor[] cargo). exitFunc will be called as exitFunc(Actor transport).")]
		public Table ReinforceWithTransport(Script runtime, Player owner, string actorType, string[] cargoTypes, CPos[] entryPath, CPos[] exitPath = null,
			Closure actionFunc = null, Closure exitFunc = null)
		{
			var transport = CreateActor(owner, actorType, true, entryPath[0], entryPath.Length > 1 ? entryPath[1] : (CPos?)null);
			var cargo = transport.TraitOrDefault<Cargo>();

			var passengers = new List<Actor>();
			if (cargo != null && cargoTypes != null)
			{
				foreach (var cargoType in cargoTypes)
				{
					var passenger = CreateActor(owner, cargoType, false, entryPath[0]);
					passengers.Add(passenger);
					cargo.Load(transport, passenger);
				}
			}

			for (var i = 1; i < entryPath.Length; i++)
				Move(transport, entryPath[i]);

			if (actionFunc != null)
			{
				transport.QueueActivity(new CallFunc(() =>
				{
					actionFunc.Call(transport, passengers);
				}));
			}
			else
			{
				var heli = transport.TraitOrDefault<Helicopter>();
				if (heli != null)
				{
					transport.QueueActivity(new Turn(transport, heli.Info.InitialFacing));
					transport.QueueActivity(new HeliLand(transport, true));
					transport.QueueActivity(new Wait(15));
				}

				if (cargo != null)
				{
					transport.QueueActivity(new UnloadCargo(transport, true));
					transport.QueueActivity(new WaitFor(() => cargo.IsEmpty(transport)));
				}

				transport.QueueActivity(new Wait(heli != null ? 50 : 25));
			}

			if (exitFunc != null)
			{
				transport.QueueActivity(new CallFunc(() =>
				{
					exitFunc.Call(transport);
				}));
			}
			else if (exitPath != null)
			{
				foreach (var wpt in exitPath)
					Move(transport, wpt);

				transport.QueueActivity(new RemoveSelf());
			}

			var ret = Context.CreateTable();
			ret.Set(1, DynValue.FromObject(runtime, transport));
			ret.Set(2, DynValue.FromObject(runtime, passengers.ToArray()));
			return ret;
		}
	}
}