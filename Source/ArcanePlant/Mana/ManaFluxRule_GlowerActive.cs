﻿using Verse;

namespace VVRace
{
    public class ManaFluxRule_GlowerActive : ManaFluxRule
    {
        public float mana;

        public override IntRange ApproximateManaFlux => new IntRange(0, (int)mana);

        public override float CalcManaFlux(ArcanePlant plant, int ticks)
        {
            var compGlower = plant.TryGetComp<CompGlowerFlora>();
            if (compGlower.Glows)
            {
                return mana / 60000f * ticks;
            }

            return 0f;
        }
    }
}