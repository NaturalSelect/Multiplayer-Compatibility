
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Multiplayer.API;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Unity.Collections;

namespace Multiplayer.Compat
{
    [MpCompatFor("fxz.glitterworlddestroyer.mk5")]
    class GD3
    {
        public GD3(ModContentPack mod)
        {
            // 延迟初始化以确保目标程序集已加载
            LongEventHandler.ExecuteWhenFinished(LatePatch);
        }

        private static void LatePatch()
        {
            // 注册直接同步方法
            RegisterSyncMethods();

            // 注册 Lambda 方法
            RegisterLambdaMethods();

            // 修复随机数问题
            PatchRandomUsage();
        }

        private static void RegisterSyncMethods()
        {
            // BlackApocriton 相关
            TryRegister("GD3.BlackApocriton:SetCaneAttack");
            TryRegister("GD3.BlackApocriton:SetCometRain");
            TryRegister("GD3.BlackApocriton:SetThunderArrows");

            // CompDeckReinforce 相关
            TryRegister("GD3.CompDeckReinforce:ChangeState");
            TryRegister("GD3.CompDeckReinforce:Upgrade");

            // CompObserverLink 相关
            TryRegister("GD3.CompObserverLink:StartConnect");

            // CompCommunicationStation 相关
            TryRegister("GD3.CompCommunicationStation:DoRaid");

            // Exostrider 相关
            TryRegister("GD3.Exostrider:OrderAttack");

            // Annihilator
            // TryRegister("GD3.Annihilator:JumpTo");

            TryRegister("GD3.Annihilator:LongJumpTo")
                .TransformArgument(0, Serializer.New(
                    // Writer: 将 TargetInfo 转换为 (thingId,mapId,x,y,z) 元组
                    (TargetInfo target) =>
                    {
                        if (target.HasThing)
                            return (target.Thing.thingIDNumber, -1, -1, -1, -1);
                        var cell = target.Cell;
                        return (-1, target.Map.uniqueID, cell.x, cell.y, cell.z);
                    },
                    // Reader: 从元组重建 TargetInfo
                    (data) =>
                    {
                        var (thingId, mapId, cellX, cellY, cellZ) = data;
                        if (thingId != -1)
                        {
                            // 寻找 Thing
                            var maps = Find.Maps;
                            for (int i = 0; i < maps.Count; i++)
                            {
                                var thing = maps[i].listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == thingId);
                                if (thing != null)
                                {
                                    return new TargetInfo(thing);
                                }

                            }
                            Log.Warning($"[MP Compat] Thing with ID {thingId} not found for TargetInfo deserialization");
                            return TargetInfo.Invalid;
                        }
                        // 使用 Map + Cell
                        var map = Find.Maps.FirstOrDefault(m => m.uniqueID == mapId);
                        if (map == null)
                        {
                            Log.Warning($"[MP Compat] Map with ID {mapId} not found for TargetInfo deserialization");
                            return TargetInfo.Invalid;
                        }
                        var cell = new IntVec3(cellX, cellY, cellZ);
                        return new TargetInfo(cell, map, true);
                    }
                ));
            TryRegister("GD3.Annihilator:ResetAllAbilities");

        }

        private static void RegisterLambdaMethods()
        {
            // CompRadio - toggleAction (delegate) + isActive (() =>)
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 0);  // toggleAction A
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 1);    // isActive A
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 2);  // toggleAction B
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 3);    // isActive B
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 4);  // toggleAction C
            TryRegisterLambdaMethod("GD3.CompRadio", "CompGetGizmosExtra", 5);    // isActive C

            // CompDeckReinforce - action (delegate)
            TryRegisterLambdaMethod("GD3.CompDeckReinforce", "CompGetGizmosExtra", 0);

            // CompAttackMode - toggleAction (delegate) + isActive (() =>)
            TryRegisterLambdaMethod("GD3.CompAttackMode", "CompGetGizmosExtra", 0);  // toggleAction
            TryRegisterLambdaMethod("GD3.CompAttackMode", "CompGetGizmosExtra", 1);    // isActive

            // CompChangeWeapon - action (delegate, 捕获 pawn)
            TryRegisterLambdaDelegate("GD3.CompChangeWeapon", "CompGetGizmosExtra", new[] { "pawn" }, 0);

            // CompChangeWeaponB - action (delegate, 捕获 pawn)
            TryRegisterLambdaDelegate("GD3.CompChangeWeaponB", "CompGetGizmosExtra", new[] { "pawn" }, 0);

            // CompObserverLink - action (delegate)
            TryRegisterLambdaMethod("GD3.CompObserverLink", "CompGetGizmosExtra", 0);

            // CompReceiverSelect - action (delegate)
            TryRegisterLambdaMethod("GD3.CompReceiverSelect", "CompGetGizmosExtra", 0);

            // CompCommunicationStation - toggleAction (delegate) + isActive (() =>) + Dev actions
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 0);        // toggleAction
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 1);          // isActive
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 2, true);  // Dev: Add intelligence-1
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 3, true);  // Dev: Add intelligence-2
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 4, true);  // Dev: Change Firewall
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 5, true);  // Dev: Discover BlackMech
            TryRegisterLambdaMethod("GD3.CompCommunicationStation", "CompGetGizmosExtra", 6, true);  // Dev: Militor

            // CompAnalyzableSubcore - action (delegate) + Dev actions
            TryRegisterLambdaMethod("GD3.CompAnalyzableSubcore", "CompGetGizmosExtra", 0);           // SendSubcore
            TryRegisterLambdaMethod("GD3.CompAnalyzableSubcore", "CompGetGizmosExtra", 1, true);
            TryRegisterLambdaMethod("GD3.CompAnalyzableSubcore", "CompGetGizmosExtra", 3, true);     // Dev: Stage to 9

            // BlackApocriton - toggleAction (delegate) + isActive (() =>) + Dev
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 0);        // toggleAction attackWings
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 1);          // isActive attackWings
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 2, true);  // Dev: toggleAction alwaysSwap
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 3, true);    // Dev: isActive alwaysSwap
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 4, true);  // Dev: Break wings
            TryRegisterLambdaMethod("GD3.BlackApocriton", "GetGizmos", 5, true);  // Dev: End fight

            // Annihilator - action (delegate) + Dev toggles
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 0);           // Target Part (空 action)
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 1, true);     // Dev: toggleAction NoAI
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 2, true);       // Dev: isActive NoAI
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 3, true);     // Dev: toggleAction StopAnimation
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 4, true);       // Dev: isActive StopAnimation
            TryRegisterLambdaMethod("GD3.Annihilator", "GetGizmos", 5, true);     // Dev: Instant Kill

            // Building_ArchoMine - Dev action (delegate)
            TryRegisterLambdaMethod("GD3.Building_ArchoMine", "GetGizmos", 0, true);  // Dev: visible?

            // Exostrider - Dev actions (delegate)
            TryRegisterLambdaMethod("GD3.Exostrider", "GetGizmos", 0, true);  // Dev: Target Random Mortar
            TryRegisterLambdaMethod("GD3.Exostrider", "GetGizmos", 1, true);  // Dev: Set health to 1
            TryRegisterLambdaMethod("GD3.Exostrider", "GetGizmos", 2, true);  // Dev: Summon Assist
            TryRegisterLambdaMethod("GD3.Exostrider", "GetGizmos", 3, true);  // Dev: Summon All Assists
            TryRegisterLambdaMethod("GD3.Exostrider", "GetGizmos", 4, true);  // Dev: Quest Finish
        }

        private static void PatchRandomUsage()
        {
            // System.Random 修复
            PatchSystemRandom();

            // UnityEngine.Random 修复
            PatchUnityRandom();

            // Verse.Rand Push/Pop 保护
            PatchVersePushPopRand();
        }

        private static void PatchSystemRandom()
        {
            TryPatchSystemRand("GD3.MissionComponent:ChangeFirewallRandom");
            TryPatchSystemRand("GD3.CompAnalyzableSubcore:OnAnalyzed");
            TryPatchSystemRand("GD3.CompCommunicationStation:OperateWorkDone");
        }

        private static void PatchUnityRandom()
        {
            TryPatchUnityRand("GD3.GDUtility:RandomPointInCircle");
        }

        private static void PatchVersePushPopRand()
        {
            // BlackApocriton 相关
            TryPatchPushPopRand("GD3.BlackApocriton:PreApplyDamage");
            TryPatchPushPopRand("GD3.BlackApocriton:TrySwapVictim");
            TryPatchPushPopRand("GD3.BlackApocriton:SetCometRain");
            TryPatchPushPopRand("GD3.BlackApocriton:ApplyCometRain");
            TryPatchPushPopRand("GD3.BlackApocriton:ApplyThunderArrows");
            TryPatchPushPopRand("GD3.ThunderArrow:Tick");
            TryPatchPushPopRand("GD3.ThunderArrow:SpawnSetup");
            TryPatchPushPopRand("GD3.ThrowingCane:Tick");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.BlackApocriton:PreApplyDamage");
            TryPatchSystemRand("GD3.BlackApocriton:TrySwapVictim");
            TryPatchSystemRand("GD3.BlackApocriton:SetCometRain");
            TryPatchSystemRand("GD3.BlackApocriton:ApplyCometRain");
            TryPatchSystemRand("GD3.BlackApocriton:ApplyThunderArrows");
            TryPatchSystemRand("GD3.ThunderArrow:Tick");
            TryPatchSystemRand("GD3.ThunderArrow:SpawnSetup");
            TryPatchSystemRand("GD3.ThrowingCane:Tick");

            // Annihilator 相关
            TryPatchPushPopRand("GD3.Annihilator:DoJump");
            TryPatchPushPopRand("GD3.Annihilator:DoLongJump");
            TryPatchPushPopRand("GD3.Annihilator:Laser");
            TryPatchPushPopRand("GD3.Annihilator:PreApplyDamage");
            TryPatchPushPopRand("GD3.Annihilator:DyingTick");
            TryPatchPushPopRand("GD3.Annihilator:DestroyRoofs");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.Annihilator:DoJump");
            TryPatchSystemRand("GD3.Annihilator:DoLongJump");
            TryPatchSystemRand("GD3.Annihilator:Laser");
            TryPatchSystemRand("GD3.Annihilator:PreApplyDamage");
            TryPatchSystemRand("GD3.Annihilator:DyingTick");
            TryPatchSystemRand("GD3.Annihilator:DestroyRoofs");


            // Exostrider
            TryPatchPushPopRand("GD3.Exostrider:TryStartShootSomething");
            TryPatchPushPopRand("GD3.Exostrider:TryFindNewTarget");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.Exostrider:TryStartShootSomething");
            TryPatchSystemRand("GD3.Exostrider:TryFindNewTarget");

            // Building 相关
            TryPatchPushPopRand("GD3.Building_AntiAirTurret:TryFindNewTargetStatic");
            TryPatchPushPopRand("GD3.Building_ArchoMine:Tick");
            TryPatchPushPopRand("GD3.DamageWorker_ArchoMine:Apply");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.Building_AntiAirTurret:TryFindNewTargetStatic");
            TryPatchSystemRand("GD3.Building_ArchoMine:Tick");
            TryPatchSystemRand("GD3.DamageWorker_ArchoMine:Apply");

            // 机甲相关
            TryPatchPushPopRand("GD3.MechMosquito:CastShoot");
            TryPatchPushPopRand("GD3.MechMosquito:ThrowFleck");
            TryPatchPushPopRand("GD3.ReinforceMosquito:ThrowFleck");
            TryPatchPushPopRand("GD3.ReinforceFlare:DoReinforcement");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.MechMosquito:CastShoot");
            TryPatchSystemRand("GD3.MechMosquito:ThrowFleck");
            TryPatchSystemRand("GD3.ReinforceMosquito:ThrowFleck");
            TryPatchSystemRand("GD3.ReinforceFlare:DoReinforcement");

            // CompAbilityEffect 相关 - 使用特殊处理方式修补重载方法
            TryPatchPushPopRandWithArgs("GD3.CompAbilityEffect_Comet", "Apply",
                new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) });
            // NOTE: also patch system random
            TryPatchSystemRandWithArgs("GD3.CompAbilityEffect_Comet", "Apply",
                new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(Thing) });

            // Projectile 相关
            TryPatchPushPopRand("GD3.Verb_LaunchProjectileBackfire:TryCastShot");
            TryPatchPushPopRand("GD3.ProjectileAntiAir:ImpactSomething");
            TryPatchPushPopRand("GD3.ProjectileAntiAir:Explode");
            TryPatchPushPopRand("GD3.ProjectileAntiAir:ThrowFleck");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.Verb_LaunchProjectileBackfire:TryCastShot");
            TryPatchSystemRand("GD3.ProjectileAntiAir:ImpactSomething");
            TryPatchSystemRand("GD3.ProjectileAntiAir:Explode");
            TryPatchSystemRand("GD3.ProjectileAntiAir:ThrowFleck");

            // Skyfaller 相关
            TryPatchPushPopRand("GD3.AlphaSkyfaller:PostMake");
            TryPatchPushPopRand("GD3.AlphaSkyfaller:SpawnSetup");
            TryPatchPushPopRand("GD3.Skyfaller_LandingMech:DrawAt");
            TryPatchPushPopRand("GD3.Skyfaller_LandingMech:SpawnThings");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.AlphaSkyfaller:PostMake");
            TryPatchSystemRand("GD3.AlphaSkyfaller:SpawnSetup");
            TryPatchSystemRand("GD3.Skyfaller_LandingMech:DrawAt");
            TryPatchSystemRand("GD3.Skyfaller_LandingMech:SpawnThings");

            // Bezier 相关
            TryPatchPushPopRand("GD3.BezierProjectiles:InitRandOffset");
            TryPatchPushPopRand("GD3.BezierProjectiles_Explosive:Explode");
            TryPatchPushPopRand("GD3.BezierProjectiles_Explosive:Impact");
            TryPatchPushPopRand("GD3.Mst_BeziertBullet:Impact");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.BezierProjectiles:InitRandOffset");
            TryPatchSystemRand("GD3.BezierProjectiles_Explosive:Explode");
            TryPatchSystemRand("GD3.BezierProjectiles_Explosive:Impact");
            TryPatchSystemRand("GD3.Mst_BeziertBullet:Impact");

            // HediffComp 相关
            TryPatchPushPopRand("GD3.HediffComp_OverHorizon:Notify_PawnPostApplyDamage");
            TryPatchPushPopRand("GD3.HediffComp_SavingMech:CompPostPostAdd");
            TryPatchPushPopRand("GD3.HediffComp_BlackShield:CompPostPostRemoved");
            TryPatchPushPopRand("GD3.HediffCompTerror:CompPostTick");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.HediffComp_OverHorizon:Notify_PawnPostApplyDamage");
            TryPatchSystemRand("GD3.HediffComp_SavingMech:CompPostPostAdd");
            TryPatchSystemRand("GD3.HediffComp_BlackShield:CompPostPostRemoved");
            TryPatchSystemRand("GD3.HediffCompTerror:CompPostTick");

            // Cluster/Sketch 相关
            TryPatchPushPopRand("GD3.IncidentWorker_MechClusterGiant:GenerateClusterSketch");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:GenerateClusterSketch");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:ResolveSketch");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:GetBuildingDefsForCluster");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:AddBuildingsToSketch");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:TryRandomBuildingWithTag");
            TryPatchPushPopRand("GD3.MechClusterGenerator_Giant:TryFindRandomPlaceFor");
            TryPatchPushPopRand("GD3.GenStep_CustomStructureGen_Platform:Generate");
            TryPatchPushPopRand("GD3.GenStep_CustomStructureGen_Mechhive:Generate");
            TryPatchPushPopRand("GD3.TileMutatorWorker_Structure:GenerateCriticalStructures");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.IncidentWorker_MechClusterGiant:GenerateClusterSketch");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:GenerateClusterSketch");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:ResolveSketch");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:GetBuildingDefsForCluster");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:AddBuildingsToSketch");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:TryRandomBuildingWithTag");
            TryPatchSystemRand("GD3.MechClusterGenerator_Giant:TryFindRandomPlaceFor");
            TryPatchSystemRand("GD3.GenStep_CustomStructureGen_Platform:Generate");
            TryPatchSystemRand("GD3.GenStep_CustomStructureGen_Mechhive:Generate");
            TryPatchSystemRand("GD3.TileMutatorWorker_Structure:GenerateCriticalStructures");

            // Quest/Script 相关
            TryPatchPushPopRand("GD3.QuestNode_Root_SavingMechModified:RunInt");
            TryPatchPushPopRand("GD3.QuestNode_Root_SavingMechNotModified:RunInt");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.QuestNode_Root_SavingMechModified:RunInt");
            TryPatchSystemRand("GD3.QuestNode_Root_SavingMechNotModified:RunInt");

            // Death/Damage 相关
            TryPatchPushPopRand("GD3.CompDeath:DamageUntilDead");
            TryPatchPushPopRand("GD3.CompDeath:RandomViolenceDamageType");
            TryPatchPushPopRand("GD3.CompDeath:StartAffect");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.CompDeath:DamageUntilDead");
            TryPatchSystemRand("GD3.CompDeath:RandomViolenceDamageType");
            TryPatchSystemRand("GD3.CompDeath:StartAffect");

            // 其他杂项
            TryPatchPushPopRand("GD3.CompCerebrexAbility:CompTick");
            TryPatchPushPopRand("GD3.CompCerebrexAbility:CallLandReinforce");
            TryPatchPushPopRand("GD3.CompCerebrexAbility:CallAirReinforce");
            TryPatchPushPopRand("GD3.CompCerebrexAbility:RandomSkip");
            TryPatchPushPopRand("GD3.CompCerebrexAbility:CreateNewLord");
            TryPatchSystemRand("GD3.CompCerebrexAbility:CompTick");
            TryPatchSystemRand("GD3.CompCerebrexAbility:CallLandReinforce");
            TryPatchSystemRand("GD3.CompCerebrexAbility:CallAirReinforce");
            TryPatchSystemRand("GD3.CompCerebrexAbility:RandomSkip");
            TryPatchSystemRand("GD3.CompCerebrexAbility:CreateNewLord");

            TryPatchPushPopRand("GD3.CompConquer:CompTick");
            TryPatchPushPopRand("GD3.FinalBattleDummy:Tick");
            TryPatchPushPopRand("GD3.CompArchoDrone:CompTick");
            TryPatchPushPopRand("GD3.CompArchoMineTerminal:OnHacked");
            TryPatchPushPopRand("GD3.ArtilleryStrike_Inferno:Tick");
            TryPatchPushPopRand("GD3.JobGiver_AIAirUnitAbilityFight:TryGiveJob");
            TryPatchPushPopRand("GD3.JobGiver_PsychicGrenade:TryGiveJob");
            TryPatchPushPopRand("GD3.ShellRandomAngle:PostMake");
            TryPatchPushPopRand("GD3.Verb_LaunchMultiProjectile:TryCastShot");
            TryPatchPushPopRand("GD3.Shield_Patch_Draw:Postfix");
            TryPatchPushPopRand("GD3.Shield_Patch_Absorb:Prefix");
            TryPatchPushPopRand("GD3.EMP_Patch:Prefix");
            TryPatchPushPopRand("GD3.CompReinforceHediff:PostSpawnSetup");
            TryPatchPushPopRand("GD3.CompTeleport_Attack:CompTick");
            TryPatchPushPopRand("GD3.CompCommunicationStation:OperateWorkDone");
            // NOTE: also patch system random
            TryPatchSystemRand("GD3.CompConquer:CompTick");
            TryPatchSystemRand("GD3.FinalBattleDummy:Tick");
            TryPatchSystemRand("GD3.CompArchoDrone:CompTick");
            TryPatchSystemRand("GD3.CompArchoMineTerminal:OnHacked");
            TryPatchSystemRand("GD3.ArtilleryStrike_Inferno:Tick");
            TryPatchSystemRand("GD3.JobGiver_AIAirUnitAbilityFight:TryGiveJob");
            TryPatchSystemRand("GD3.JobGiver_PsychicGrenade:TryGiveJob");
            TryPatchSystemRand("GD3.ShellRandomAngle:PostMake");
            TryPatchSystemRand("GD3.Verb_LaunchMultiProjectile:TryCastShot");
            TryPatchSystemRand("GD3.Shield_Patch_Draw:Postfix");
            TryPatchSystemRand("GD3.Shield_Patch_Absorb:Prefix");
            TryPatchSystemRand("GD3.EMP_Patch:Prefix");
            TryPatchSystemRand("GD3.CompReinforceHediff:PostSpawnSetup");
            TryPatchSystemRand("GD3.CompTeleport_Attack:CompTick");
            TryPatchSystemRand("GD3.CompCommunicationStation:OperateWorkDone");

            TryPatchPushPopRand("GD3.JobGiver_AnnihilatorFight:TryGiveJob");
            TryPatchSystemRand("GD3.JobGiver_AnnihilatorFight:TryGiveJob");

            TryPatchPushPopRand("GD3.Verb_ThrowArc_Red:LightningStrike");
            TryPatchSystemRand("GD3.Verb_ThrowArc_Red:LightningStrike");

            TryPatchPushPopRand("GD3.Verb_ThrowArc:TryCastShot");
            TryPatchSystemRand("GD3.Verb_ThrowArc:TryCastShot");
        }

        // 辅助方法：安全地注册同步方法
        private static ISyncMethod TryRegister(string methodPath)
        {
            try
            {
                var method = AccessTools.DeclaredMethod(methodPath);
                if (method != null)
                {
                    return MP.RegisterSyncMethod(method);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to register sync method {methodPath}: {ex.Message}");
            }
            return null;
        }

        // 辅助方法：安全地注册 Lambda 方法
        private static ISyncMethod TryRegisterLambdaMethod(string typeName, string parentMethod, int lambdaOrdinal, bool debugOnly = false)
        {
            try
            {
                var methods = MpCompat.RegisterLambdaMethod(typeName, parentMethod, lambdaOrdinal);
                if (methods.Count() == 0)
                {
                    Log.Warning($"[MP Compat] Lambda method not found: {typeName}:{parentMethod}[{lambdaOrdinal}]");
                    return null;
                }
                var method = methods.First();
                if (debugOnly)
                {
                    method.SetDebugOnly();
                }
                return method;
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to register lambda method {typeName}:{parentMethod}[{lambdaOrdinal}]: {ex.Message}");
            }
            return null;
        }

        // 辅助方法：安全地注册 Lambda 委托（用于 delegate () {} 语法）
        // private static void TryRegisterLambdaDelegate(string typeName, string parentMethod, int lambdaOrdinal, bool debugOnly = false)
        // {
        //     try
        //     {
        //         var delegates = MpCompat.RegisterLambdaDelegate(typeName, parentMethod, lambdaOrdinal);
        //         if (debugOnly)
        //         {
        //             foreach (var del in delegates)
        //                 del.SetDebugOnly();
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Log.Warning($"[MP Compat] Failed to register lambda delegate {typeName}:{parentMethod}[{lambdaOrdinal}]: {ex.Message}");
        //     }
        // }

        // 辅助方法：安全地注册 Lambda 委托（带 fields 参数，用于捕获局部变量的闭包）
        private static void TryRegisterLambdaDelegate(string typeName, string parentMethod, string[] fields, int lambdaOrdinal, bool debugOnly = false)
        {
            try
            {
                var delegates = MpCompat.RegisterLambdaDelegate(typeName, parentMethod, fields, lambdaOrdinal);
                if (debugOnly)
                {
                    foreach (var del in delegates)
                        del.SetDebugOnly();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to register lambda delegate {typeName}:{parentMethod}[{lambdaOrdinal}]: {ex.Message}");
            }
        }

        // 辅助方法：安全地修补 System.Random
        private static void TryPatchSystemRand(string methodPath)
        {
            try
            {
                PatchingUtilities.PatchSystemRand(methodPath);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to patch System.Random in {methodPath}: {ex.Message}");
            }
        }

        private static void TryPatchSystemRandWithArgs(string typeName, string methodName, Type[] argTypes)
        {
            try
            {
                var type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    Log.Warning($"[MP Compat] Type not found: {typeName}");
                    return;
                }

                var method = AccessTools.Method(type, methodName, argTypes);
                if (method == null)
                {
                    Log.Warning($"[MP Compat] Method not found: {typeName}:{methodName}");
                    return;
                }

                PatchingUtilities.PatchSystemRand(method);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to patch System.Random in {typeName}:{methodName}: {ex.Message}");
            }
        }

        // 辅助方法：安全地修补 UnityEngine.Random
        private static void TryPatchUnityRand(string methodPath)
        {
            try
            {
                PatchingUtilities.PatchUnityRand(methodPath);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to patch UnityEngine.Random in {methodPath}: {ex.Message}");
            }
        }

        // 辅助方法：安全地修补 Verse.Rand Push/Pop
        private static void TryPatchPushPopRand(string methodPath)
        {
            try
            {
                PatchingUtilities.PatchPushPopRand(methodPath);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to patch PushPopRand in {methodPath}: {ex.Message}");
            }
        }

        // 辅助方法：安全地修补 Verse.Rand Push/Pop（带参数类型，用于重载方法）
        private static void TryPatchPushPopRandWithArgs(string typeName, string methodName, Type[] argTypes)
        {
            try
            {
                var type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    Log.Warning($"[MP Compat] Type not found: {typeName}");
                    return;
                }

                var method = AccessTools.Method(type, methodName, argTypes);
                if (method == null)
                {
                    Log.Warning($"[MP Compat] Method not found: {typeName}:{methodName}");
                    return;
                }

                PatchingUtilities.PatchPushPopRand(method);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MP Compat] Failed to patch PushPopRand in {typeName}:{methodName}: {ex.Message}");
            }
        }
    }

    [HarmonyPatch("CompAbilityEffect_AnnihilatorLongJump", "StartChoosingDestination")]
    static class MakeSpaceForReplayTimeline
    {

        static bool Prefix()
        {
            if (MP.IsInMultiplayer && MP.IsExecutingSyncCommand && !MP.IsExecutingSyncCommandIssuedBySelf)
            {
                // 在执行同步命令时，禁止 CompAbilityEffect_AnnihilatorLongJump.StartChoosingDestination 以避免打开目标选择界面
                return false;
            }
            return true;
        }
    }
}