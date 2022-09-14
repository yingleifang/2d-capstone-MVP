using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit: MonoBehaviour
{
    public int currentHealth, currentAttackDamage, currentAttackRange,
            currentMovementSpeed, currentCoolDown;

    public int health, attackDamage, attackRange, movementSpeed, coolDown;

    public Vector3Int location;

    [SerializeField]
    private AudioSource deathSound;
    [SerializeField]
    private AudioSource attackSound, damageSound;
    [SerializeField]
    public SpriteRenderer spriteRenderer;

    public bool isDead = false;

    protected int currentWaypointIndex = 0;
    protected List<Vector3Int> path = null;

    public bool inMovement = false;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void Start() 
    {
        currentHealth = health;
        currentAttackDamage = attackDamage;
        currentAttackRange = attackRange;
        currentMovementSpeed = movementSpeed;
        currentCoolDown = coolDown;
        BattleManager.instance.unitsToSpawn.Add(this);
    }

    public abstract IEnumerator UseAbility(Vector3Int target);

    public virtual IEnumerator DoAttack(Unit target)
    {
        target.ChangeHealth(currentAttackDamage * -1);
        attackSound.Play();
        yield break;
    }

    public virtual IEnumerator StartOfBattleAbility(BattleState state)
    {
        yield break;
    }

    public virtual void StartOfTurn()
    {
        return;
    }

    public virtual IEnumerator DoMovement(BattleState state, Vector3Int target)
    {
        if (state.map.GetTile(target) == null)
        {
            yield break;
        }
        if (path == null)
        {
            path = state.map.FindShortestPath(location, target);
            inMovement = true;
        }
        state.map.RemoveUnitFromTile(location);
        state.map.AddUnitToTile(target, this);
        yield return StartCoroutine(smoothMovement(state, target));

        yield break;
        //TODO Check bounds here. Access map classs to do this.
        //Trigger animations here
    }

    IEnumerator smoothMovement(BattleState state, Vector3Int target)
    {

        while (currentWaypointIndex < path.Count)
        {
            var step = movementSpeed * Time.deltaTime * 10;
            Vector3 worldPostion = state.map.CellToWorldPosition(path[currentWaypointIndex]);
            transform.position = Vector3.MoveTowards(transform.position, worldPostion, step);
            if (Vector3.Distance(transform.position, worldPostion) < 0.00001f)
            {
                currentWaypointIndex++;
            }
            yield return null;
        }

        path = null;
        inMovement = false;
        currentWaypointIndex = 0;
        transform.position = state.map.CellToWorldPosition(location);

        if (state.map.IsHazardous(target))
        {
            ChangeHealth(-1);
            damageSound.Play();
        }
    }

    public virtual List<Vector3Int> GetTilesInMoveRange(TileManager map)
    {
        // Can override this method for unique move ranges
        return map.GetTilesInRange(location, currentMovementSpeed);
    }

    public virtual bool IsTileInMoveRange(Vector3Int tile, TileManager map)
    {
        return map.RealDistance(location, tile) <= currentMovementSpeed;
    }

    public virtual List<Vector3Int> GetTilesInAttackRange(TileManager map)
    {
        // Can override this method for unique attack ranges
        return map.GetTilesInRange(location, currentAttackRange, false);
    }

    public virtual bool IsTileInAttackRange(Vector3Int tile, TileManager map)
    {
        return map.RealDistance(location, tile, false) <= currentAttackRange;
    }

    public virtual List<Vector3Int> GetTilesInThreatRange(TileManager map)
    {
        // Can override this method for unique threat ranges
        return map.GetTilesInRange(location, currentMovementSpeed + currentAttackRange, false);
    }

    public bool SetLocation(BattleState state, Vector3Int target)
    {
        if (state.map.GetTile(target) == null)
        {
            return false;
        }

        location = target;
        
        transform.position = state.map.CellToWorldPosition(target);

        return true;     
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isDead = true;
        }
    }

    public IEnumerator Die() 
    {
        deathSound.Play();
        spriteRenderer.enabled = false;
        Destroy(gameObject);
        yield break;
    }
    
}
