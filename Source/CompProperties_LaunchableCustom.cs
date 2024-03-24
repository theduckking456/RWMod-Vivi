﻿using RimWorld;
using System;
using Verse;

namespace VVRace
{
    public class ActiveDropPodInfoCustom : ActiveDropPodInfo
    {
        public ThingDef activeDropPod;
        public ThingDef incomingDropPod;

        public ActiveDropPodInfoCustom(CompProperties_LaunchableCustom compProps)
        {
            activeDropPod = compProps.activeDropPod;
            incomingDropPod = compProps.incomingDropPod;
        }
    }

    public class CompProperties_LaunchableCustom : CompProperties_Launchable
    {
        public ThingDef activeDropPod;
        public ThingDef incomingDropPod;
    }
}
