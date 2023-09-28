﻿using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VVRace
{
    public class GerminateScheduleDef : Def
    {
        public int uiOrder;
        public Color uiColor = Color.white;
        public Color uiTextColor = Color.white;
        [NoTranslate]
        public string uiIconPath;

        [Unsaved(false)]
        public Texture2D uiIcon = BaseContent.BadTex;

        public int workAmount;
        public List<IngredientCount> ingredients;

        public JobDef germinateJob;

        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (!uiIconPath.NullOrEmpty())
                {
                    uiIcon = ContentFinder<Texture2D>.Get(uiIconPath);
                }
            });
        }
    }
}
