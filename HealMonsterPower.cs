using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpecialAttack/HealMonsterPower_ScriptableObject", order = 1)]
public class HealMonsterPower : SpecialAttackScript
{
    public enum eHealTarget { MOSTDAMAGEDALLY, BOSS, SPECIFICMONSTERREF}
    [Header("Heal Power Settings")]
    [SerializeField] private float amountToHeal = 75f;
    [SerializeField] private eHealTarget myHealTarget;
    [Tooltip("Allow this power to heal its user assuming its a valid target according to all other rules.")]
    [SerializeField] private bool canSelfHeal;

    //variable is public so it can by dynamically set by the custom editor class at the bottom of this file
    [HideInInspector] public string specificTargetMonsterRef;

    public override bool CanUse(Fighter owner)
    {
        //Check we have a valid heal target, return false if we don't
        switch (myHealTarget)
        {
            case eHealTarget.BOSS:
                bool bossFound = false;
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    MonsterTemplate template = f.myMonsterTemplate;
                    if (template != null && template.isBoss)
                    {
                        if (!canSelfHeal && f == owner) continue;
                        bossFound = true;
                    }
                }
                if (!bossFound) return false;
                break;

            case eHealTarget.MOSTDAMAGEDALLY:
                bool damagedAllyFound = false;
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    MonsterTemplate template = f.myMonsterTemplate;
                    if (template != null && f.GetCurHealth() < f.myMonsterTemplate.health)
                    {
                        if (!canSelfHeal && f == owner) continue;
                        damagedAllyFound = true;
                    }
                }
                if (!damagedAllyFound) return false;
                break;

            case eHealTarget.SPECIFICMONSTERREF:
                bool specificMonsterPresent = false;
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    MonsterTemplate template = f.myMonsterTemplate;
                    if (template != null && template.refName.Contains(specificTargetMonsterRef))
                    {
                        if (!canSelfHeal && f == owner) continue;
                        specificMonsterPresent = true;
                    }
                }
                if (!specificMonsterPresent) return false;
                break;
        }

        //still check the normal canuse rules
        return base.CanUse(owner);
    }

    public override AttackPackage ExecuteAttack(Fighter target, Fighter attacker)
    {
        //Choose target in an unusual way since we're targeting an ally.
        switch (myHealTarget)
        {
            case eHealTarget.BOSS:
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    MonsterTemplate template = f.myMonsterTemplate;
                    if (template != null)
                    {
                        if (!canSelfHeal && f == attacker) continue;
                        if (template.isBoss) target = f; //One of them has to be a boss, because we already checked in CanUse()
                        break;
                    }
                }
                break;

            case eHealTarget.MOSTDAMAGEDALLY:
                float mostMissingHPInAllies = 0;
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    //loop through the monsters and save the one with lowest health as the target
                    if (f.isTheHero) continue; //We're only looking for monsters
                    MonsterTemplate template = f.myMonsterTemplate;
                    float curMissingHealth = GetMissingHealth(f, template);
                    if (curMissingHealth > mostMissingHPInAllies)
                    {
                        if (!canSelfHeal && f == attacker) continue;
                        mostMissingHPInAllies = curMissingHealth;
                        target = f;
                    }
                }
                break;

            case eHealTarget.SPECIFICMONSTERREF:
                foreach (Fighter f in BattleManager.fightersInBattle)
                {
                    //loop through the monsters and save the one with the target ref name as target
                    MonsterTemplate template = f.myMonsterTemplate;
                    if (template != null && f.refName.Contains(specificTargetMonsterRef))
                    {
                        if (!canSelfHeal && f == attacker) continue;
                        target = f;
                        break;
                    }
                }
                break;
        }

        AttackPackage ap = new AttackPackage(target);
        ap.attacker = attacker;
        ap.attackType = AttackTypes.MONSTER_ABILITY;
        ap.noDamageIntended = true;
        ap.impactAnimationsOnTarget = new List<string>();
        ap.impactAnimationsOnTarget.Add("FX_Healing");


        //actually heal the target

        //Don't heal more than their max health
        float amountNeededToFullHeal = target.myMonsterTemplate.health - target.GetCurHealth();
        float amountActuallyHealing = amountToHeal;
        if (amountActuallyHealing > amountNeededToFullHeal) amountActuallyHealing = amountNeededToFullHeal;

        //change their health
        target.ChangeHealth(amountActuallyHealing);

        return ap;
    }

    public eHealTarget GetHealTarget()
    {
        return myHealTarget;
    }

    /// <summary>
    /// Returns the amount of damage this fighter is below their maximum health
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    private float GetMissingHealth(Fighter f, MonsterTemplate fighterTemplate)
    {
        return (fighterTemplate.health - f.GetCurHealth());
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HealMonsterPower))]
public class HealMonsterPowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var myHealScript = target as HealMonsterPower;

        //If we're targeting a specific monster ref, create an inspector field where we can enter which ref we're targeting
        if (myHealScript.GetHealTarget() == HealMonsterPower.eHealTarget.SPECIFICMONSTERREF)
        {
            myHealScript.specificTargetMonsterRef = EditorGUILayout.TextField("Specific Target Monster Ref", myHealScript.specificTargetMonsterRef);
        }
    }
}
#endif