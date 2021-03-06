﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace Replace_Stuff.Replace
{
	[HarmonyPatch(typeof(GenConstruct), "PlaceBlueprintForBuild")]
	class InterceptBlueprint
	{
		//public static Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		public static bool Prefix(ref Blueprint_Build __result, BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff)
		{
			if (faction != Faction.OfPlayer) return true;

			//Fix for door rotation
			if (sourceDef is ThingDef thingDef && thingDef.thingClass == typeof(Building_Door))
				rotation = Building_Door.DoorRotationAt(center, map);

			Func<Thing, bool> posCheck = t => t.Position == center && t.Rotation == rotation;
			
			Func<Thing, bool> newReplaceCheck = t => posCheck(t) &&
				t.def == sourceDef && t.Stuff != stuff;
			Func<Thing, bool> changeFrameStuffCheck = t => posCheck(t) &&
				t is Frame f && f.UIStuff() != stuff && f.def.entityDefToBuild == sourceDef;
			Func<Thing, bool> changeReplaceStuffCheck = t => posCheck(t) &&
				t is ReplaceFrame rf && rf.UIStuff() != stuff && rf.def.entityDefToBuild == sourceDef;

			List<Thing> thingsHere = center.GetThingList(map);
			if (thingsHere.FirstOrDefault(changeReplaceStuffCheck) is ReplaceFrame oldReplaceFrame)
			{
				if (oldReplaceFrame.oldStuff == stuff)
					oldReplaceFrame.Destroy(DestroyMode.Cancel);
				else
					oldReplaceFrame.ChangeStuff(stuff);
				__result = null;
				return false;
			}
			else if (thingsHere.FirstOrDefault(changeFrameStuffCheck) is Thing oldFrame)
			{
				oldFrame.Destroy(DestroyMode.Cancel);
				return true;
			}
			else if (thingsHere.FirstOrDefault(newReplaceCheck) is Thing oldThing)
			{
				GenReplace.PlaceReplaceFrame(oldThing, stuff);
				__result = null;
				return false;
			}
			return true;
		}
	}
}

