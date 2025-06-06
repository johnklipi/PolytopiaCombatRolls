using BepInEx.Logging;
using HarmonyLib;

namespace PolytopiaCombatRolls;

public class CombatRolls
{
    private static ManualLogSource? modLogger;
    private const int MAX_ROLL_RESULT = 6;
    private const float ROLL_STEP = 0.25f;
    private static bool isMidAttackCommand = false;

    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(CombatRolls));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static bool AttackCommand_ExecuteDefault(GameState gameState)
    {
        isMidAttackCommand = true;
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static void AttackCommand_ExecuteDefault_Postfix(GameState gameState)
    {
        isMidAttackCommand = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BattleHelpers), nameof(BattleHelpers.GetBattleResults))]
    private static void BattleHelpers_GetBattleResults(ref BattleResults __result, GameState gameState, UnitState attackingUnit, UnitState defendingUnit, bool accountForPeace)
    {
        if (isMidAttackCommand && gameState.TryGetPlayer(attackingUnit.owner, out PlayerState playerState))
        {
            float combatRoll = GetCombatRoll(gameState.Seed, gameState.CurrentTurn, attackingUnit.coordinates, defendingUnit.coordinates, attackingUnit.id, defendingUnit.id);
            float attackDamage = __result.attackDamage * combatRoll;
            float retaliationDamage = __result.retaliationDamage * GetOppositeNumber(combatRoll);
            __result.attackDamage = (int)attackDamage;
            __result.retaliationDamage = (int)retaliationDamage;
            NotificationManager.Notify(Localization.Get("combatrolls.message", new Il2CppSystem.Object[] { (combatRoll / ROLL_STEP).ToString() }), Localization.Get("combatrolls.title"), null, playerState);
        }
    }

    public static float GetCombatRoll(
        int seed,
        uint currentTurn,
        WorldCoordinates attackerCoordinates,
        WorldCoordinates defenderCoordinates,
        uint attackingId,
        uint defendingId)
    {
        int hash = seed;
        hash ^= (int)currentTurn;
        hash ^= attackerCoordinates.X.GetHashCode();
        hash ^= attackerCoordinates.Y.GetHashCode();
        hash ^= defenderCoordinates.X.GetHashCode();
        hash ^= defenderCoordinates.Y.GetHashCode();
        hash ^= (int)attackingId;
        hash ^= (int)defendingId;

        hash = Math.Abs(hash);
        
        return (float)((hash % MAX_ROLL_RESULT) + 1) * ROLL_STEP;
    }

    public static float GetOppositeNumber(float value)
    {
        return (ROLL_STEP * (MAX_ROLL_RESULT + 1)) - value;
    }
}
