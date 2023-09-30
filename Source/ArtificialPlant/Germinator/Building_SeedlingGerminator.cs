﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Verse;

namespace VVRace
{
    public enum GerminateScheduleJob
    {
        None,
        Repotting,
        PestControl,
        Weeding,
    }

    [StaticConstructorOnStartup]
    public class Building_SeedlingGerminator : Building
    {
        public GerminateSchedule CurrentSchedule => _currentSchedule;
        public GerminateSchedule LastProcessedSchedule => _lastCompletedSchedule;

        private GerminateSchedule _currentSchedule;
        private GerminateSchedule _lastCompletedSchedule;

        private GerminatorModExtension _defModExtension;
        public GerminatorModExtension GerminatorModExtension
        {
            get
            {
                if (_defModExtension == null)
                {
                    _defModExtension = def.GetModExtension<GerminatorModExtension>();
                }

                return _defModExtension;
            }
        }

        private Dictionary<ThingDef, int> _germinateReservedThings = null;
        private Dictionary<ThingDef, int> _germinateIngredients = null;
        public IReadOnlyDictionary<ThingDef, int> GerminateIngredients
        {
            get
            {
                if (_germinateIngredients == null)
                {
                    _germinateIngredients = new Dictionary<ThingDef, int>();
                    foreach (var tdc in GerminatorModExtension.germinateIngredients)
                    {
                        _germinateIngredients.Add(tdc.thingDef, tdc.count);
                    }
                }

                return _germinateIngredients;
            }
        }

        public IEnumerable<(ThingDef def, int count)> RequiredGerminateIngredients
        {
            get
            {
                foreach (var ingredient in GerminateIngredients)
                {
                    _germinateReservedThings.TryGetValue(ingredient.Key, out var reserved);

                    if (reserved < ingredient.Value)
                    {
                        yield return (ingredient.Key, ingredient.Value - reserved);
                    }
                }
            }
        }

        public bool CanWithdrawProduct => _canWithdrawProduct; 
        private bool _canWithdrawProduct = true;

        public ThingDef ProductThingDef => _productThingDef;
        private ThingDef _productThingDef;

        public int ProductThingCount
        {
            get
            {
                if (_productThingDef == null)
                {
                    return 0;
                }
                else if (_productThingDef.IsPlant)
                {
                    return Mathf.CeilToInt(_productRemainedCount / (float)_productThingDef.stackLimit);
                }
                else
                {
                    return _productRemainedCount;
                }
            }
        }
        private int _productRemainedCount;

        public int GetGerminateRequiredCount(ThingDef def)
        {
            if (GerminateIngredients.TryGetValue(def, out var ingredientCount))
            {
                if (_germinateReservedThings != null && _germinateReservedThings.TryGetValue(def, out var reserved))
                {
                    if (ingredientCount - reserved < 0) { Log.Warning($"invalid germinate ingredients: {ingredientCount} {reserved}"); }
                    return Mathf.Max(ingredientCount - reserved, 0);
                }

                return ingredientCount;
            }

            return 0;
        }

        private static readonly Texture2D GerminateCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/VV_Germinate");

        private static readonly Texture2D CancelCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref _currentSchedule, "currentSchedule");
            Scribe_Deep.Look(ref _lastCompletedSchedule, "lastCompletedSchedule");

            Scribe_Collections.Look(ref _germinateReservedThings, "germinateReservedThings", LookMode.Def, LookMode.Value);
            Scribe_Collections.Look(ref _germinateIngredients, "germinateIngredients", LookMode.Def, LookMode.Value);

            Scribe_Values.Look(ref _canWithdrawProduct, "canWithdrawProduct");
            Scribe_Defs.Look(ref _productThingDef, "productThingDef");
            Scribe_Values.Look(ref _productRemainedCount, "productRemainedCount");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _currentSchedule?.ResolveBuildingDef(def);
                _lastCompletedSchedule?.ResolveBuildingDef(def);
            }
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());

            if (CurrentSchedule != null)
            {
                var schedule = CurrentSchedule;
                switch (schedule.Stage)
                {
                    case GerminateStage.None:
                        {
                            if (sb.Length > 0) { sb.AppendLine(); }
                            sb.Append(LocalizeTexts.InspectorViviGerminatorReserved.Translate());

                            if (GerminatorModExtension.germinateIngredients?.Count > 0)
                            {
                                foreach (var tdc in GerminatorModExtension.germinateIngredients)
                                {
                                    if (sb.Length > 0) { sb.AppendLine(); }
                                    sb.Append($"{tdc.thingDef.LabelCap}: {(_germinateReservedThings.TryGetValue(tdc.thingDef, out var count) ? count : 0)}/{tdc.count}");
                                }
                            }
                        }
                        break;

                    case GerminateStage.GerminateInProgress:
                        {
                            if (sb.Length > 0) { sb.AppendLine(); }
                            sb.Append(LocalizeTexts.InspectorViviGerminatorCompletePeriods.Translate(schedule.TicksToCompleteGerminate.ToStringTicksToPeriod(allowSeconds: false)));

                            if (schedule.CanManageJob)
                            {
                                var currentScheduleDef = schedule.CurrentManageScheduleDef;

                                if (sb.Length > 0) { sb.AppendLine(); }
                                sb.Append(LocalizeTexts.InspectorViviGerminatorManageAvailable.Translate(schedule.CurrentScheduleNumber, currentScheduleDef.LabelCap));

                                if (currentScheduleDef.ingredients?.Count > 0)
                                {
                                    var sbIngredients = new StringBuilder();
                                    foreach (var tdc in currentScheduleDef.ingredients)
                                    {
                                        if (sbIngredients.Length > 0) { sbIngredients.Append(", "); }
                                        sbIngredients.Append($"{tdc.thingDef.LabelCap} x{tdc.count}");
                                    }

                                    if (sb.Length > 0) { sb.AppendLine(); }
                                    sb.Append(LocalizeTexts.InspectorViviGerminatorManageIngredient.Translate(sbIngredients.ToString()));
                                }
                            }
                            else if (schedule.HasNextManageJob)
                            {
                                if (sb.Length > 0) { sb.AppendLine(); }
                                sb.Append(LocalizeTexts.InspectorViviGerminatorManageLastPeriods.Translate(
                                    schedule.TicksToNextManageJob.ToStringTicksToPeriod()));
                            }
                        }
                        break;

                    case GerminateStage.GerminateComplete:
                        {
                            if (sb.Length > 0) { sb.AppendLine(); }
                            if (_productThingDef == null)
                            {
                                sb.Append(LocalizeTexts.InspectorViviGerminatorNoProduct.Translate());
                            }
                            else
                            {
                                sb.Append(LocalizeTexts.InspectorViviGerminatorProduct.Translate(_productThingDef.LabelCap, _productRemainedCount));
                            }
                        }
                        break;
                }
            }

            return sb.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (Spawned)
            {
                if (_currentSchedule == null && Find.Selector.SelectedObjectsListForReading
                    .Where(v => v is Building_SeedlingGerminator)
                    .Cast<Building_SeedlingGerminator>()
                    .GroupBy(v => v.def)
                    .Count() == 1)
                {
                    var commandRegisterSchedule = new Command_RegisterGerminateSchedule();
                    commandRegisterSchedule.building = this;
                    commandRegisterSchedule.icon = GerminateCommandTex;
                    commandRegisterSchedule.iconOffset = new Vector2(0f, -0.08f);
                    commandRegisterSchedule.defaultLabel = LocalizeTexts.CommandRegisterGerminateSchedule.Translate();
                    commandRegisterSchedule.defaultDesc = LocalizeTexts.CommandRegisterGerminateScheduleDesc.Translate();
                    yield return commandRegisterSchedule;
                }
                else if (_currentSchedule != null && _currentSchedule.Stage != GerminateStage.GerminateComplete)
                {
                    var commandCancelSchedule = new Command_CancelGerminateSchedule();
                    commandCancelSchedule.building = this;
                    commandCancelSchedule.icon = CancelCommandTex;
                    commandCancelSchedule.defaultLabel = LocalizeTexts.CommandCancelGerminateSchedule.Translate();
                    commandCancelSchedule.defaultDesc = LocalizeTexts.CommandCancelGerminateScheduleDesc.Translate();
                    yield return commandCancelSchedule;
                }

                if (_productThingDef != null)
                {
                    var commandCanWithdrawProduct = new Command_Toggle();
                    commandCanWithdrawProduct.defaultLabel = LocalizeTexts.CommandCanWithdrawProduct.Translate();
                    commandCanWithdrawProduct.defaultDesc = LocalizeTexts.CommandCanWithdrawProductDesc.Translate();
                    commandCanWithdrawProduct.isActive = () => _canWithdrawProduct;
                    commandCanWithdrawProduct.icon = _productThingDef.uiIcon;
                    commandCanWithdrawProduct.toggleAction = () =>
                    {
                        _canWithdrawProduct = !_canWithdrawProduct;
                    };
                }

                if (DebugSettings.godMode)
                {
                    if (CurrentSchedule != null && CurrentSchedule.Stage == GerminateStage.GerminateInProgress)
                    {
                        var commandDebugReduceNextTick = new Command_Action();
                        commandDebugReduceNextTick.defaultLabel = "Reduce next manage ticks 1hours";
                        commandDebugReduceNextTick.action = () =>
                        {
                            CurrentSchedule.Debug_ReduceNextManageTick(2500);
                        };
                        yield return commandDebugReduceNextTick;

                        var commandDebugReduceCompleteTick = new Command_Action();
                        commandDebugReduceCompleteTick.defaultLabel = "Reduce complete ticks 1day";
                        commandDebugReduceCompleteTick.action = () =>
                        {
                            CurrentSchedule.Debug_ReduceCompleteTick(2500 * 24);
                        };
                        yield return commandDebugReduceCompleteTick;
                    }
                }
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(60))
            {
                CurrentSchedule?.Tick(this);
            }
        }

        public override void TickRare()
        {
            base.TickRare();

            CurrentSchedule?.Tick(this);
        }

        public override void TickLong()
        {
            base.TickLong();

            CurrentSchedule?.Tick(this);
        }

        public override void Print(SectionLayer layer)
        {
            base.Print(layer);

            if (CurrentSchedule == null || CurrentSchedule.Stage == GerminateStage.None)
            {
                return;
            }

            try
            {
                Rand.PushState();
                Rand.Seed = base.thingIDNumber.GetHashCode();

                if (CurrentSchedule.Stage == GerminateStage.GerminateInProgress)
                {
                    var vectors = this.OccupiedRect().Cells.Select(v => v.ToVector3ShiftedWithAltitude(def.Altitude)).ToList();
                    vectors.Add(this.TrueCenter());

                    var drawSize = 0.35f;
                    foreach (var vector in vectors)
                    {
                        var zero = vector;
                        if (zero.z - 0.5f < Position.z)
                        {
                            zero.z += (Position.z - zero.z) * 0.3f;
                        }

                        var isFlipUV = Rand.Bool;
                        var material = ThingDefOf.Plant_Grass.graphic.MatSingle;
                        Graphic.TryGetTextureAtlasReplacementInfo(material, def.category.ToAtlasGroup(), isFlipUV, vertexColors: false, out material, out var uvs, out var _);

                        var colors = new Color32[4];
                        colors[1].a = (colors[2].a = 25);
                        colors[0].a = (colors[3].a = 0);

                        Printer_Plane.PrintPlane(
                            size: new Vector2(drawSize, drawSize),
                            layer: layer,
                            center: zero,
                            mat: material,
                            rot: 0f,
                            flipUv: isFlipUV,
                            uvs: uvs,
                            colors: colors,
                            topVerticesAltitudeBias: 0.1f,
                            uvzPayload: this.HashOffset() % 1024);
                    }
                }
                else if (CurrentSchedule.Stage == GerminateStage.GerminateComplete || CurrentSchedule.Stage == GerminateStage.GerminateComplete)
                {
                    var vectors = this.OccupiedRect().Cells.Select(v => v.ToVector3ShiftedWithAltitude(def.Altitude)).ToList();
                    vectors.Add(this.TrueCenter());

                    var drawSize = 0.6f;
                    for (int i = 0; i < Mathf.Min(vectors.Count, _productRemainedCount); ++i)
                    {
                        var zero = vectors[i];
                        if (zero.z - 0.5f < Position.z)
                        {
                            zero.z += (Position.z - zero.z) * 0.5f;
                        }

                        var isFlipUV = Rand.Bool;
                        var material = _productThingDef != null ? _productThingDef.graphic.MatSingle : ThingDefOf.Plant_Grass.graphic.MatSingle;
                        Graphic.TryGetTextureAtlasReplacementInfo(material, def.category.ToAtlasGroup(), isFlipUV, vertexColors: false, out material, out var uvs, out var _);

                        var colors = new Color32[4];
                        colors[1].a = (colors[2].a = 25);
                        colors[0].a = (colors[3].a = 0);

                        Printer_Plane.PrintPlane(
                            size: new Vector2(drawSize, drawSize),
                            layer: layer,
                            center: zero,
                            mat: material,
                            rot: 0f,
                            flipUv: isFlipUV,
                            uvs: uvs,
                            colors: colors,
                            topVerticesAltitudeBias: 0.1f,
                            uvzPayload: this.HashOffset() % 1024);
                    }
                }
            }
            finally
            {
                Rand.PopState();
            }
        }

        public void AddThings(Thing thing)
        {
            var requiredCount = GetGerminateRequiredCount(thing.def);
            if (requiredCount > 0)
            {
                int count = Mathf.Min(requiredCount, thing.stackCount);
                thing.SplitOff(count).Destroy();

                if (_germinateReservedThings.TryGetValue(thing.def, out var reservedCount))
                {
                    _germinateReservedThings[thing.def] = reservedCount + count;
                }
                else
                {
                    _germinateReservedThings.Add(thing.def, count);
                }
            }

            if (!RequiredGerminateIngredients.Any())
            {
                _germinateReservedThings = null;

                CurrentSchedule.StartGerminate();
                Notify_RefreshDrawer();
            }
        }

        public void ReserveSchedule(GerminateSchedule schedule)
        {
            _currentSchedule = schedule;
            _germinateReservedThings = new Dictionary<ThingDef, int>();
        }

        public void CancelSchedule()
        {
            _currentSchedule = null;

            if (_germinateReservedThings != null)
            {
                foreach (var kv in _germinateReservedThings)
                {
                    var maxStackCount = kv.Key.stackLimit;
                    int stackCount = kv.Value;
                    while (stackCount > 0)
                    {
                        var count = Mathf.Min(maxStackCount, stackCount);
                        var thing = ThingMaker.MakeThing(kv.Key);
                        thing.stackCount = count;
                        stackCount -= count;

                        GenSpawn.Spawn(thing, Position, Map);
                    }
                }

                _germinateReservedThings = null;
            }
        }

        public void Clear()
        {
            _canWithdrawProduct = true;

            _productThingDef = null;
            _productRemainedCount = 0;

            _lastCompletedSchedule = _currentSchedule;
            _currentSchedule = null;

            Notify_RefreshDrawer();
        }

        public Thing WithdrawProduct()
        {
            if (_productThingDef == null || _productRemainedCount <= 0) { return null; }

            Thing thing = null;
            if (_productThingDef.IsPlant && _productThingDef.plant.harvestedThingDef != null)
            {
                thing = ThingMaker.MakeThing(_productThingDef.plant.harvestedThingDef);
                thing.stackCount = Mathf.Clamp(_productRemainedCount, 0, _productThingDef.plant.harvestedThingDef.stackLimit);

                _productRemainedCount -= thing.stackCount;
            }
            else if (_productThingDef.Minifiable)
            {
                thing = ThingMaker.MakeThing(_productThingDef);
                thing = thing.MakeMinified();

                _productRemainedCount--;
            }

            if (_productRemainedCount == 0)
            {
                Clear();
            }

            return thing;
        }

        public void Notify_RefreshDrawer()
        {
            DirtyMapMesh(Map);
        }

        public void Notify_ScheduleComplete(ThingDef resultThingDef, int resultCount)
        {
            if (resultThingDef != null)
            {
                _productThingDef = resultThingDef;
                _productRemainedCount = resultThingDef.IsPlant ? Rand.Range(resultCount * 2, resultCount * 3) : resultCount;

                Messages.Message(
                    LocalizeTexts.MessageGerminateSeedlingSuccess.Translate(resultThingDef.LabelCap, resultCount),
                    this,
                    MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message(
                    LocalizeTexts.MessageGerminateSeedlingFailed.Translate(),
                    this,
                    MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
