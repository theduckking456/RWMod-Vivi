﻿using System.Collections.Generic;
using Verse;

namespace VVRace
{
    public class ManaFluxRule_Weather : ManaFluxRule
    {
        public HashSet<WeatherDef> weatherDefs;
        public float mana;

        public override IntRange ApproximateManaFlux => new IntRange(0, (int)mana);

        public override float CalcManaFlux(ArcanePlant plant, int ticks)
        {
            if (weatherDefs != null && weatherDefs.Contains(plant.Map.weatherManager.curWeather))
            {
                return mana / 60000f * ticks;
            }

            return 0f;
        }
    }
}