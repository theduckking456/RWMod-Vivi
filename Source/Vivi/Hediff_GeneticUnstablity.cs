﻿using RimWorld;
using Verse;

namespace VVRace
{
    public class Hediff_GeneticUnstablity : HediffWithComps
    {
        private const int SeverityChangeInterval = 30000;
        private const float TendSuccessChanceFactor = 0.5f;
        private const float TendSeverityReduction = 0.4f;

        private float _intervalFactor;

        public override void PostMake()
        {
            base.PostMake();
            _intervalFactor = Rand.Range(0.25f, 2f);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _intervalFactor, "intervalFactor", 0f);
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick((int)(SeverityChangeInterval * _intervalFactor)))
            {
                Severity += Rand.Range(-0.25f, 0.25f);

                if (Severity >= 1f)
                {
                    pawn.Kill(null, this);
                    pawn.GenerateHornetFromGeneticDeath();
                }
            }
        }

        public override void Tended(float quality, float maxQuality, int batchPosition = 0)
        {
            base.Tended(quality, maxQuality, 0);

            var chance = TendSuccessChanceFactor * quality;
            if (Rand.Value < chance)
            {
                if (batchPosition == 0 && pawn.Spawned)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_TreatSuccess".Translate(chance.ToStringPercent()), 6.5f);
                }
                Severity -= TendSeverityReduction;
            }
            else if (batchPosition == 0 && pawn.Spawned)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_TreatFailed".Translate(chance.ToStringPercent()), 6.5f);
            }
        }
    }
}
