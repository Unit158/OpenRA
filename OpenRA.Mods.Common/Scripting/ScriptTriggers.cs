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
using OpenRA.Primitives;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	public enum Trigger
	{
		OnIdle, OnDamaged, OnKilled, OnProduction, OnOtherProduction, OnPlayerWon, OnPlayerLost,
		OnObjectiveAdded, OnObjectiveCompleted, OnObjectiveFailed, OnCapture, OnInfiltrated,
		OnAddedToWorld, OnRemovedFromWorld, OnDiscovered, OnPlayerDiscovered
	}

	[Desc("Allows map scripts to attach triggers to this actor via the Triggers global.")]
	public class ScriptTriggersInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new ScriptTriggers(init.World); }
	}

	public sealed class ScriptTriggers : INotifyIdle, INotifyDamage, INotifyKilled, INotifyProduction, INotifyOtherProduction,
		INotifyObjectivesUpdated, INotifyCapture, INotifyInfiltrated, INotifyAddedToWorld, INotifyRemovedFromWorld, IDisposable, INotifyDiscovered
	{
		readonly World world;

		public event Action<Actor> OnKilledInternal = _ => { };
		public event Action<Actor> OnCapturedInternal = _ => { };
		public event Action<Actor> OnRemovedInternal = _ => { };
		public event Action<Actor, Actor> OnProducedInternal = (a, b) => { };
		public event Action<Actor, Actor> OnOtherProducedInternal = (a, b) => { };

		public Dictionary<Trigger, List<Pair<Closure, ScriptContext>>> Triggers = new Dictionary<Trigger, List<Pair<Closure, ScriptContext>>>();

		public ScriptTriggers(World world)
		{
			this.world = world;

			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
				Triggers.Add(t, new List<Pair<Closure, ScriptContext>>());
		}

		public void RegisterCallback(Trigger trigger, Closure func, ScriptContext context)
		{
			Triggers[trigger].Add(Pair.New(func, context));
		}

		public void TickIdle(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnIdle])
			{
				try
				{
					f.First.Call(self);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnDamaged])
			{
				try
				{
					f.First.Call(self, e.Attacker);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnKilled])
			{
				try
				{
					f.First.Call(self, e.Attacker);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnKilledInternal(self);
		}

		public void UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnProduction])
			{
				try
				{
					f.First.Call(self, other);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnProducedInternal(self, other);
		}

		public void OnPlayerWon(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnPlayerWon])
			{
				try
				{
					f.First.Call(player);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnPlayerLost(Player player)
		{
			if (world.Disposing)
				return;

			foreach (var func in Triggers[Trigger.OnPlayerLost])
			{
				try
				{
					func.First.Call(player);
				}
				catch (Exception ex)
				{
					func.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveAdded(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnObjectiveAdded])
			{
				try
				{
						f.First.Call(player, id);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveCompleted(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnObjectiveCompleted])
			{
				try
				{
					f.First.Call(player, id);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnObjectiveFailed(Player player, int id)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnObjectiveFailed])
			{
				try
				{
					f.First.Call(player, id);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnCapture])
			{
				try
				{
					f.First.Call(self, captor, oldOwner, newOwner);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnCapturedInternal(self);
		}

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnInfiltrated])
			{
				try
				{
					f.First.Call(self, infiltrator);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void AddedToWorld(Actor self)
		{
			if (world.Disposing)
				return;

			foreach (var f in Triggers[Trigger.OnAddedToWorld])
			{
				try
				{
						f.First.Call(self);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void RemovedFromWorld(Actor self)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnRemovedFromWorld])
			{
				try
				{
					f.First.Call(self);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnRemovedInternal(self);
		}

		public void UnitProducedByOther(Actor self, Actor producee, Actor produced)
		{
			if (world.Disposing)
				return;

			// Run Lua callbacks
			foreach (var f in Triggers[Trigger.OnOtherProduction])
			{
				try
				{
					f.First.Call(producee, produced);
				}
				catch (Exception ex)
				{
					f.Second.FatalError(ex.Message);
					return;
				}
			}

			// Run any internally bound callbacks
			OnOtherProducedInternal(producee, produced);
		}

		public void OnDiscovered(Actor self, Player discoverer, bool playNotification)
		{
			if (world.Disposing)
				return;

			foreach (var func in Triggers[Trigger.OnDiscovered])
			{
				try
				{
					func.First.Call(self, discoverer);
				}
				catch (Exception ex)
				{
					func.Second.FatalError(ex.Message);
					return;
				}
			}

			foreach (var func in Triggers[Trigger.OnPlayerDiscovered])
			{
				try
				{
					func.First.Call(self.Owner, discoverer, self);
				}
				catch (Exception ex)
				{
					func.Second.FatalError(ex.Message);
					return;
				}
			}
		}

		public void Clear(Trigger trigger)
		{
			world.AddFrameEndTask(w =>
			{
				Triggers[trigger].Clear();
			});
		}

		public void ClearAll()
		{
			foreach (Trigger t in Enum.GetValues(typeof(Trigger)))
				Clear(t);
		}

		public void Dispose()
		{
			ClearAll();
		}
	}
}
