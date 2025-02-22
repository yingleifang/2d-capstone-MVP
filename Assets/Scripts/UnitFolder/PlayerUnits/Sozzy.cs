using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sozzy : PlayerUnit
{
    public SoundEffect StartOfBattleAbilitySound;
    public int abilityDamage;
    public GameObject flamePrefab;

    protected override void Awake()
    {
        base.Awake();
        prefab = unitPrefabSO.GetPrefab("Sozzy");
    }

    public override IEnumerator StartOfBattleAbility(BattleState state)
    {
        Debug.Log("Speeding up adjacent units");
        List<Vector3Int> tiles = state.tileManager.GetTilesInRange(location, 1, false);
        yield return StartCoroutine(PlayEOBAnim());
        foreach (Vector3Int tile in tiles)
        {
            Unit unit = state.tileManager.GetUnit(tile);
            if (unit && unit != this && unit is PlayerUnit player)
            {
                player.currentMovementSpeed += 1;
                player.movementSpeed += 1;
                yield return StartCoroutine(unit.SpawnStatNumber("<sprite=\"fast\" name=\"fast\">", 1, Color.green));
            }
        }
        yield break;
    }

    /// <summary>
    /// Does damage in a straight line from Sozzy. Assume that the
    /// coordinate passed in is a valid target
    /// </summary>
    public override IEnumerator UseAbility(Vector3Int target, BattleState state)
    {
        Debug.Log("USING ABILITY");
        TileManager map = state.tileManager;
        List<Vector3Int> path = map.FindShortestPath(location, target, false, false);
        if (path.Count > abilityRange || target == location)
        {
            yield break;
        }
        else
        {
            FlipSprite(state.tileManager.CellToWorldPosition(target));
            StartCoroutine(PlayAbilityAnimation());
            yield return new WaitForSeconds(0.5f);
            foreach (Vector3Int coord in path)
            {
                Vector3 worldPos = state.tileManager.CellToWorldPosition(coord);
                GameObject effect = Instantiate(flamePrefab, worldPos, Quaternion.identity);
                if (worldPos.x < transform.position.x)
                {
                    Vector3 scale = effect.transform.localScale;
                    effect.transform.localScale = new Vector3(scale.x * -1, scale.y, scale.z);
                }
                Unit curUnit = state.tileManager.GetUnit(coord);
                Debug.Log(curUnit);
                if (!curUnit)
                {
                    Debug.Log(curUnit);
                    continue;
                }
                else
                {
                    Debug.Log(curUnit);
                    curUnit.ChangeHealth(abilityDamage * -1);
                }
            }
            currentCoolDown = coolDown;
        }
        yield return null;
    }

    /// <summary>
    /// Returns the tiles within range for the unit's ability.
    /// For Sozzy, returns tiles in straight lines in all directions up to abilityRange tiles away.
    /// </summary>
    /// <returns>a list of tiles within ability range for the unit</returns>
    public override List<Vector3Int> GetTilesInAbilityRange()
    {
        return BattleManager.instance.tileManager.GetTilesInRangeStraight(location, abilityRange);
    }
}
