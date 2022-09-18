using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sozzy : PlayerUnit
{
    public SoundEffect StartOfBattleAbilitySound;
    public override IEnumerator StartOfBattleAbility(BattleState state)
    {
        Debug.Log("Speeding up adjacent units");
        List<Vector3Int> tiles = state.map.GetTilesInRange(location, 1, false);
        audio.PlaySound(StartOfBattleAbilitySound);
        foreach (Vector3Int tile in tiles)
        {
            Unit unit = state.map.GetUnit(tile);
            if (unit && unit != this && unit is PlayerUnit player)
            {
                player.currentMovementSpeed += 1;
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield break;
    }
}
