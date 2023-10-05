﻿using Verse;

namespace VVRace
{
    public class CompProperties_LightningLod : CompProperties
    {
        public IntRange energyConditionRange;
        public int energyGain;

        public CompProperties_LightningLod()
        {
            compClass = typeof(CompLightningLod);
        }
    }

    public class CompLightningLod : ThingComp
    {
        public CompProperties_LightningLod Props => (CompProperties_LightningLod)props;

        public bool Active => parent is ArtificialPlant plant &&
            plant.Spawned &&
            !plant.Position.Roofed(plant.Map) &&
            plant.Energy >= Props.energyConditionRange.min && 
            plant.Energy < Props.energyConditionRange.max;

        public void OnThunderStrike()
        {
            if (parent is ArtificialPlant plant)
            {
                plant.AddEnergy(Props.energyGain);
            }
        }
    }
}
