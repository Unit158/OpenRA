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
using System.Text;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Mods.Common.Activities;

namespace OpenRA.Mods.D2k.Traits
{
	public class SpicebloomInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<HealthInfo>
	{
		[SequenceReference]
		public readonly string[] GrowthSequences = { "grow1", "grow2", "grow3" };

		[Desc("The range of time (in ticks) that the spicebloom will take to respawn.")]
		public readonly int[] RespawnDelay = { 1500/50, 2500/50 };

		[Desc("The range of time (in ticks) that the spicebloom will take to grow.")]
		public readonly int[] GrowthDelay = { 1000/50, 1500/50 };

		public readonly string ResourceType = "Spice";

		[Desc("The weapon to use as spice.")]
		[WeaponReference]
		public readonly string Weapon = "SpiceExplosion";

		[Desc("The range of spice to expel.")]
		public readonly int[] Pieces = { 7, 9 };

		[Desc("The range in cells that spice may be expelled.")]
		public readonly int[] Range = { 5, 7 };

		public object Create(ActorInitializer init) { return new Spicebloom(init, this); }
	}

	public class Spicebloom : ITick, INotifyKilled
	{
		readonly Actor self;
		readonly SpicebloomInfo info;
		readonly ResourceType resType;
		readonly ResourceLayer resLayer;
		readonly Health health;
		readonly AnimationWithOffset anim;
		readonly string race;

		int counter;
		int respawnTicks;
		int growTicks;

		public Spicebloom(ActorInitializer init, SpicebloomInfo info)
		{
			this.info = info;
			self = init.Self;

			health = self.Trait<Health>();
			health.RemoveOnDeath = false;

			resType = self.World.WorldActor.TraitsImplementing<ResourceType>()
							.First(t => t.Info.Name == info.ResourceType);

			resLayer = self.World.WorldActor.Trait<ResourceLayer>();

			var render = self.Trait<RenderSprites>();

			anim = new AnimationWithOffset(new Animation(init.Self.World, render.GetImage(self)), null, () => self.IsDead);

			render.Add(anim);

			respawnTicks = self.World.SharedRandom.Next(info.RespawnDelay[0], info.RespawnDelay[1]);
			growTicks = self.World.SharedRandom.Next(info.GrowthDelay[0], info.GrowthDelay[1]);
			anim.Animation.Play(info.GrowthSequences[0]);
			
			Game.Debug("Hi, I'm spicebloom {0}", self.ActorID);
		}

		public void Tick(Actor self)
		{
			if (!self.IsDead)
			{
				counter++;

				if (counter >= growTicks)
					self.Kill(self);
				else
				{
					var index = info.GrowthSequences.Length * counter / growTicks;
					anim.Animation.Play(info.GrowthSequences[index]);
				}
			}
		}

		public void Killed(Actor self, AttackInfo e)
		{
			self.QueueActivity(new Wait(respawnTicks));
			self.QueueActivity(new CallFunc(() =>
				self.World.AddFrameEndTask(w =>
				{
					var a = w.CreateActor(self.Info.Name, new TypeDictionary
					{
						new LocationInit(self.Location),
						new HealthInit(health.MaxHP),
						new OwnerInit(self.Owner)
					});
					Game.Debug("{0} spawned {1}", self.ActorID, a.ActorID);
					self.Dispose();
				})
			));

			var pieces = self.World.SharedRandom.Next(info.Pieces[0], info.Pieces[1]);
			var wep = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];

			for (var i = 0; pieces > i; i++)
			{
				var range = self.World.SharedRandom.Next(info.Pieces[0], info.Pieces[1]);

				var cells = OpenRA.Traits.Util.RandomWalk(self.Location, self.World.SharedRandom);
				var cell = cells.Take(range).SkipWhile(p => resLayer.GetResource(p) == resType && resLayer.IsFull(p)).Cast<CPos?>().RandomOrDefault(self.World.SharedRandom);

				if (cell == null)
					cell = cells.Take(range).Random(self.World.SharedRandom);

				var args = new ProjectileArgs
				{
					Weapon = wep,
					Facing = 0,

					DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray(),

					InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray(),

					Source = self.CenterPosition,
					SourceActor = self,
					PassiveTarget = self.World.Map.CenterOfCell(cell.Value)
				};

				self.World.AddFrameEndTask(x =>
				{
					if (args.Weapon.Projectile != null)
					{
						var projectile = args.Weapon.Projectile.Create(args);
						if (projectile != null)
							self.World.Add(projectile);

						if (args.Weapon.Report != null && args.Weapon.Report.Any())
							Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
					}
				});
			}
		}
	}
}
