using SpriteGlow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingSettings : MonoBehaviour
{
    [SerializeField]
    private Color SelectColor;

    [SerializeField]
    private Color DeSelectColor;

    [SerializeField]
    private float CanMoveAndAttackGlowBrightness;

    [SerializeField]
    private int CanMoveAndAttackOutlineWidth;

    [SerializeField]
    private int EnemyOutlineWidth;

    [SerializeField]
    private float CanAttackGlowBrightness;

    [SerializeField]
    private int CanAttackOutlineWidth;

    public static PostProcessingSettings instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
    }

    public void ShowTheGlow(List<PlayerUnit> playerUnits)
    {
        foreach (var playerUnit in playerUnits)
        {
            var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
            spriteGlow.GlowBrightness = CanMoveAndAttackGlowBrightness;
            spriteGlow.OutlineWidth = CanMoveAndAttackOutlineWidth;
        };
    }

    public void DisableTheGlow(List<PlayerUnit> playerUnits)
    {
        foreach (var playerUnit in playerUnits)
        {
            DisableGlow(playerUnit);
        };
    }
    public void ChangeAllColorToDeSelected(List<PlayerUnit> playerUnits)
    {
        foreach (var playerUnit in playerUnits)
        {
            ChangeColorToDeSelected(playerUnit);
        };
    }
    public void DisableGlow(PlayerUnit playerUnit)
    {
            var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
            spriteGlow.OutlineWidth = 0;
    }

    public void CanAttackGlow(PlayerUnit playerUnit)
    {
        var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        spriteGlow.GlowBrightness = CanAttackGlowBrightness;
        spriteGlow.OutlineWidth = CanAttackOutlineWidth;
    }

    public void CanMoveAndAttackGlow(PlayerUnit playerUnit)
    {
        var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        spriteGlow.GlowBrightness = CanMoveAndAttackGlowBrightness;
        spriteGlow.OutlineWidth = CanMoveAndAttackOutlineWidth;
    }

    public void ChangeColorToSelected(PlayerUnit playerUnit)
    {
        var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        spriteGlow.GlowColor = SelectColor;
    }

    public void ChangeColorToDeSelected(PlayerUnit playerUnit)
    {
        var spriteGlow = playerUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        spriteGlow.GlowColor = DeSelectColor;
    }

    public void EnableEnemyGlow(EnemyUnit enemyUnit)
    {
        Debug.Log("HERE");
        var spriteGlow = enemyUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        Debug.Log(CanMoveAndAttackGlowBrightness);
        Debug.Log(EnemyOutlineWidth);
        spriteGlow.GlowBrightness = CanMoveAndAttackGlowBrightness;
        spriteGlow.OutlineWidth = EnemyOutlineWidth;
    }

    public void DisableEnemyGlow(EnemyUnit enemyUnit)
    {
        var spriteGlow = enemyUnit.gameObject.transform.GetComponentInChildren<SpriteGlowEffect>();
        spriteGlow.OutlineWidth = 0;
    }
}
