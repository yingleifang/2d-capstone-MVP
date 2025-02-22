using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using Priority_Queue;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography;
using System.Drawing;
using Color = UnityEngine.Color;

/// <summary>
/// Container class for data pertaining to a single tile
/// </summary>
public class HexTileData
{
    public TileDataScriptableObject tileData;
    public TileBase tile;
    public Sprite sprite;
    public DynamicTileData dynamicTileData;

    public HexTileData(TileDataScriptableObject tileData, TileBase tile, Sprite sprite, DynamicTileData dynamicTileData)
    {
        this.tileData = tileData;
        this.tile = tile;
        this.sprite = sprite;
        this.dynamicTileData = dynamicTileData;
    }
}

/// <summary>
/// An implementation of Vec3 to differentiate between cube coordinates
/// and cell coordinates. Implements + and - operators.
/// </summary>
public class CubeCoord {
    private Vector3Int coords;

    public CubeCoord(int x, int y, int z) 
    {
        coords = new Vector3Int(x, y, z);
    }

    public CubeCoord(Vector3Int vec)
    {
        coords = vec;
    }

    public int x {
        get { return coords.x; }
        set { coords.x = value; }
    }

    public int y {
        get { return coords.y; }
        set { coords.y = value; }
    }

    public int z {
        get { return coords.z; }
        set { coords.z = value; }
    }

    public int q {
        get { return coords.x; }
        set { coords.x = value; }
    }

    public int r {
        get { return coords.y; }
        set { coords.y = value; }
    }

    public int s {
        get { return coords.z; }
        set { coords.z = value; }
    }

    public static CubeCoord operator +(CubeCoord a, CubeCoord b) {
        return new CubeCoord(a.coords + b.coords);
    }

    public static CubeCoord operator -(CubeCoord a, CubeCoord b) {
        return new CubeCoord(a.coords - b.coords);
    }

    public static CubeCoord operator *(CubeCoord a, int b) {
        return new CubeCoord(a.coords.x * b, a.coords.y * b, a.coords.z * b);
    }

    public override string ToString() {
        return coords.ToString();
    }
}

public class TileManager : MonoBehaviour
{
    [SerializeField]
    public Tilemap map;
    
    //[SerializeField]
    //public List<TileDataScriptableObject> tileDatas;

    private List<Vector3Int> coloredTiles = new List<Vector3Int>();

    private Dictionary<Vector3Int, Color> coloredTilesTutorial = new Dictionary<Vector3Int, Color>();

    private CubeCoord[] directions = {new CubeCoord(1, 0, -1), new CubeCoord(1, -1, 0), 
             new CubeCoord(0, -1, 1), new CubeCoord(-1, 0, 1), new CubeCoord(-1, 1, 0), new CubeCoord(0, 1, -1)};

    public enum CubeDirections : int {
        RIGHT = 0,
        TOP_RIGHT = 1,
        TOP_LEFT = 2,
        LEFT = 3,
        BOTTOM_LEFT = 4,
        BOTTOM_RIGHT = 5
    }

    public Dictionary<TileBase, TileDataScriptableObject> baseTileDatas;  
    public Dictionary<Vector3Int, DynamicTileData> dynamicTileDatas;
    public Dictionary<Unit, Vector3Int> unitLocations;
    public AnimatedTile animatedTile;

    public SoundEffect crackSound;
    public SoundEffect shatterSound;


    public new AudioComponent audio;

    LevelManager levelManager;

    /// <summary>
    /// Changes the tiles in the current map to the tiles stored in
    /// LevelManager
    /// </summary>
    private void SetMapConfig()
    {
        foreach (var info in LevelManager.instance.tileInfo)
        {   
            map.SetTile(info.Key, info.Value.Item1.tiles[0]);
        }
    }

    private void SetMapConfigNoRandom()
    {
        
    }

    List<Vector3Int> tilePos;

    private void Start()
    {
        tilePos = new List<Vector3Int>();
        audio = GetComponent<AudioComponent>();

        if (!LevelManager.instance.isTutorial)
        {
            Debug.Log("RANDOMIZED");
            SetMapConfig();
        }

        InitializeTileDict();
    }

    public void CrackTiles(int turn)
    {
        audio.PlayDisposable(crackSound);
        foreach (var info in LevelManager.instance.tileInfo)
        {
                if (info.Value.Item2 == turn)
                {
                    map.SetTile(info.Key, info.Value.Item1.tiles[1]);
                    map.SetAnimationFrame(info.Key, 0);
                }
        }
    }

    public IEnumerator ShatterTiles(int turn, List<Unit> units = null)
    {
        // If the level isn't randomized, we will not have set the correct data for tileInfo.
        // Since we don't really need the correct data in tileInfo for anything, I'll just
        // manually set it so the tiles do the animation.
        if (LevelManager.instance.tileInfo.Count == 0 && turn == 0)
        {
            RemoveAllDecorations();
            for (int x = (int)map.localBounds.min.x; x < map.localBounds.max.x; x++)
            {
                for (int y = (int)map.localBounds.min.y; y < map.localBounds.max.y; y++)
                {
                    Vector3Int coord = new Vector3Int(x, y, 0);
                    if (map.GetTile(coord))
                    {
                        TileBase curTile = map.GetTile(coord);
                        tilePos.Add(coord);
                        Unit unit = GetUnit(coord);
                        if (unit)
                        {
                            if (units != null)
                                units.Add(unit);
                            StartCoroutine(unit.Fall());
                        }
                        map.SetTile(coord, baseTileDatas[curTile].animatedTile);
                        map.SetAnimationFrame(coord, 0);
                    }
                }
            }            
        }

        if (turn != 0)
        {
            foreach (var info in LevelManager.instance.tileInfo)
            {
                if (info.Value.Item2 == turn)
                {
                    HandleTileReplacement(units, info);
                }
            }
        }
        else
        {
            if (BattleManager.instance.ui.turnCountDown.currentTurn <= 2)
            {
                foreach (var info in LevelManager.instance.tileInfo)
                {
                    if (info.Value.Item2 == turn)
                    {
                        HandleTileReplacement(units, info);
                    }
                }
            }
            else if (BattleManager.instance.ui.turnCountDown.currentTurn <= 3)
            {
                foreach (var info in LevelManager.instance.tileInfo)
                {
                    if (info.Value.Item2 == 1)
                    {
                        continue;
                    }
                    HandleTileReplacement(units, info);
                }
            }
            else
            {
                foreach (var info in LevelManager.instance.tileInfo)
                {
                    HandleTileReplacement(units, info);
                }
            }
        }

        if (turn != 0 && tilePos.Count == 0)
        {
            yield break;
        }
        audio.PlayDisposable(shatterSound);
        yield return StartCoroutine(WaitForAnimation());
    }

    private void HandleTileReplacement(List<Unit> units, KeyValuePair<Vector3Int, (TileDataScriptableObject, int)> info)
    {
        ClearTileDecoration(info.Key);
        tilePos.Add(info.Key);
        Unit unit = GetUnit(info.Key);
        if (unit)
        {
            if (units != null)
                units.Add(unit);
            StartCoroutine(unit.Fall());
        }
        map.SetTile(info.Key, info.Value.Item1.animatedTile);
        map.SetAnimationFrame(info.Key, 0);
    }

    public Vector3Int ClampPos(Vector3Int tilePos)
    {
        int y = Mathf.Clamp(tilePos.y, -levelManager.y_range, levelManager.y_range);
        var xBound = levelManager.boundList[y + levelManager.y_range];
        int x = Mathf.Clamp(tilePos.x, xBound.Item2, xBound.Item1);
        int z = 0;
        return new Vector3Int(x, y, z);
    }

    //private void Update()
    //{
    //    if (tilePos != null)
    //    {
    //        Debug.Log("***************");
    //        Debug.Log(map.GetAnimationFrame(tilePos));
    //        Debug.Log("~~~~~~~~~~~~~~~~");
    //    }

    //}

    public IEnumerator WaitForAnimation() {
        while (tilePos.Count > 0)
        {
            List<Vector3Int> toRemove = new List<Vector3Int>();

            if (LevelManager.currentLevel == 1 || LevelManager.currentLevel == 2)
            {
                foreach (var pos in tilePos)
                {
                    TileBase curTile = map.GetTile(pos);
                    try
                    {
                        if (map.GetAnimationFrame(pos) >= baseTileDatas[curTile].animatedTile.m_AnimatedSprites.Length - 3)
                        {
                            map.SetTile(pos, levelManager.typesOfTilesToSpawn[0].tiles[0]);
                            toRemove.Add(pos);
                        }
                    }                    
                    catch
                    {
                        toRemove.Add(pos);
                    }
                }
                tilePos.RemoveAll(x => toRemove.Contains(x));
                yield return new WaitForSeconds(.6f);
            }
            else
            {
                foreach (var pos in tilePos)
                {
                    var info = LevelManager.instance.tileInfo[pos];
                    try
                    {
                        if (map.GetAnimationFrame(pos) >= info.Item1.animatedTile.m_AnimatedSprites.Length - 3)
                        {
                            map.SetTile(pos, levelManager.typesOfTilesToSpawn[0].tiles[0]);
                            toRemove.Add(pos);
                        }
                    }
                    catch
                    {
                        toRemove.Add(pos);
                    }
                }
                tilePos.RemoveAll(x => toRemove.Contains(x));
                yield return new WaitForSeconds(0.1f);
            }
        }
        tilePos = new List<Vector3Int>();
    }

    // Start is called before the first frame update
    void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
        map = FindObjectOfType<Tilemap>();
        Debug.Log(map);
        Debug.Log(levelManager);
    }

    public void InitializeTileDict()
    {
        Debug.Log("initializing tile dict");
        baseTileDatas = new Dictionary<TileBase, TileDataScriptableObject>();
        dynamicTileDatas = new Dictionary<Vector3Int, DynamicTileData>();
        unitLocations = new Dictionary<Unit, Vector3Int>();
        var targetTiles = LevelManager.instance.levelOneTiles;
        if (LevelManager.currentLevel == 5 || LevelManager.currentLevel == 6)
        {
            targetTiles = LevelManager.instance.levelTwoTiles;
        }else if (LevelManager.currentLevel == 7 || LevelManager.currentLevel == 8)
        {
            targetTiles = LevelManager.instance.levelThreeTiles;
        }

        foreach (TileDataScriptableObject tileData in targetTiles)
        {
            foreach (TileBase tile in tileData.tiles)
            {
                baseTileDatas.Add(tile, tileData);
            }
        }

        for (int x = (int)map.localBounds.min.x; x < map.localBounds.max.x; x++)
        {
            for (int y = (int)map.localBounds.min.y; y < map.localBounds.max.y; y++)
            {
                for (int z = (int)map.localBounds.min.z; z < map.localBounds.max.z; z++)
                {
                    if (map.GetTile(new Vector3Int(x, y, z)))
                    {
                        dynamicTileDatas.Add(new Vector3Int(x, y, z), new DynamicTileData());
                    }
                }
            }
        }
    }

    /// <summary>
    /// Takes a screen coord and translates it to cell coordinates
    /// </summary>   
    public Vector3Int GetTileAtScreenPosition(Vector3 pos)
    {
        Vector2 screenPos = Camera.main.ScreenToWorldPoint(pos);
        return map.WorldToCell(screenPos);
    }

    /// <summary>
    /// Returns the tile data associated with the given tile position.
    /// </summary>
    /// <param name="tilePos">the tile position to fetch data for</param>
    /// <returns>a HexTileData object containing the tile's data. Returns null if the tile does not exist</returns>
    public HexTileData GetTileData(Vector3Int tilePos)
    {
        TileBase tile = GetTile(tilePos);
        Sprite sprite = map.GetSprite(tilePos);
        TileDataScriptableObject tileData = baseTileDatas[GetTile(tilePos)];
        DynamicTileData dynamicTileData = dynamicTileDatas[tilePos];
        return new HexTileData(tileData, tile, sprite, dynamicTileData);
    }

    /// <summary>
    /// Sets the overlay for the tile.
    /// Used purely for informational purposes (ie so we know there's an overlay).
    /// Doesn't display anything.
    /// </summary>
    /// <param name="tilePos">the tile to set the data for</param>
    /// <param name="overlay">the overlay on the tile</param>
    public void SetTileOverlay(Vector3Int tilePos, Overlay overlay)
    {
        if (dynamicTileDatas.ContainsKey(tilePos))
        {
            dynamicTileDatas[tilePos].overlay = overlay;
        }
    }

    /// <summary>
    /// Stores the given unit as the last unit that died on the given tile.
    /// Used for Mori's ability to revive units.
    /// </summary>
    /// <param name="tilePos">the tile to set</param>
    /// <param name="unit">the unit to set as the last killed unit</param>
    public void SetTileDeadUnit(Vector3Int tilePos, Unit unit)
    {
        if (dynamicTileDatas.ContainsKey(tilePos))
        {
            dynamicTileDatas[tilePos].deadUnit = unit.prefab;
        }
    }

    /// <summary>
    /// Stores the given object as decoration on a the given tile.
    /// Used to store gravestone locations.
    /// </summary>
    /// <param name="tilePos">the tile to set the decoration of</param>
    /// <param name="decoration">the decoration object</param>
    public void SetTileDecoration(Vector3Int tilePos, GameObject decoration)
    {
        if (dynamicTileDatas.ContainsKey(tilePos))
        {
            dynamicTileDatas[tilePos].decoration = decoration;
        }
    }

    /// <summary>
    /// Clears and destroys any decorations on the given tile.
    /// </summary>
    /// <param name="tilePos">the tile position</param>
    public void ClearTileDecoration(Vector3Int tilePos)
    {
        if (dynamicTileDatas.ContainsKey(tilePos))
        {
            if (dynamicTileDatas[tilePos].decoration != null)
            {
                Destroy(dynamicTileDatas[tilePos].decoration.gameObject);
                dynamicTileDatas[tilePos].decoration = null;
            }
        }
    }

    public void RemoveAllDecorations()
    {
        foreach (var data in dynamicTileDatas.Values)
        {
            if (data.decoration != null)
            {
                Destroy(data.decoration.gameObject);
                data.decoration = null;
            }
        }
    }

    public void ClearTileDeadUnit(Vector3Int tilePos)
    {
        if (dynamicTileDatas.ContainsKey(tilePos))
        {
            dynamicTileDatas[tilePos].deadUnit = null;
        }
    }

    /// <summary>
    /// Returns the unit that last died on the given tile.
    /// </summary>
    /// <param name="tilePos">the tile to check</param>
    /// <returns>the unit that last died on the given tile. Returns null if no unit died.</returns>
    public Unit GetTileDeadUnit(Vector3Int tilePos)
    {
        if (!dynamicTileDatas.ContainsKey(tilePos))
        {
            return null;
        }
        return dynamicTileDatas[tilePos].deadUnit;
    }

    /// </summary>
    /// Takes a cell coord and translates it to world coordinates
    /// </summary> 
    public Vector3 CellToWorldPosition(Vector3Int pos)
    {
        return map.CellToWorld(pos);
    }

    public void SpawnUnit(Vector3Int location, Unit unit)
    {
        if(unitLocations.ContainsKey(unit))
        {
            RemoveUnitFromTile(unitLocations[unit]);
        }
        dynamicTileDatas[location].unit = unit;
        unitLocations[unit] = location;
    }

    /// <summary>
    /// Sets unit data and tilemanager data to represent that the unit is
    /// at the given location coordinate. Cannot be called on a unit which
    /// already exists on the map.
    /// </summary>
   public void AddUnitToTile(Vector3Int location, Unit unit)
    {
        // A unit cannot exist on two tiles at once. 
        if (unitLocations.ContainsKey(unit))
        {
            Debug.LogError("Adding same unit to two tiles. TileManager.cs AddUnitToTile");
            return;
        }
        dynamicTileDatas[location].unit = unit;
        unitLocations[unit] = location;
        unit.location = location;
    }

    /// <summary>
    /// Sets tilemanager data to show no unit present on the tile at 
    /// the coordinates given. Does not set any unit data
    /// </summary>
    public void RemoveUnitFromTile(Vector3Int location)
    {
        if (dynamicTileDatas.ContainsKey(location))
        {
            unitLocations.Remove(dynamicTileDatas[location].unit);
            dynamicTileDatas[location].unit = null;
        }
        else
        {
            Debug.LogError("Trying to remove a unit from a tile which does not exist");
        }
    }

    public Unit GetUnit(Vector3Int tilePos)
    {
        if (!dynamicTileDatas.ContainsKey(tilePos))
        {
            return null;
        }
        return dynamicTileDatas[tilePos].unit;
    }

    public IEnumerator OnUnitFallOnTile(BattleState state, Unit unit, Vector3Int TilePos)
    {
        // Eventually want different effects for each tile
        if (IsHazardous(TilePos)) {
            unit.ChangeHealth(-1);
        }
        yield break;
    }

    public IEnumerator OnUnitWalkOnTile(BattleState state, Unit unit, Vector3Int TilePos)
    {
        // Eventually want different effects for each tile
        if (IsHazardous(TilePos))
        {
            unit.ChangeHealth(-1);
        }
        yield break;
    }

    /// <summary>
    /// A tile is impassable if it does not exist in the tilemap or if it has
    /// the impassable trait. If unitsBlock = true, then a unit present on a tile
    /// will also render it impassable
    /// </summary>
    public bool IsImpassableTile(Vector3Int cellCoords, bool unitsBlock = true, bool terrainBlocks = true)
    {
            TileBase tile = map.GetTile(cellCoords);
            if (tile is AnimatedTile)
            {
            return true;
            }
            if (tile == null || (baseTileDatas[tile].impassable && terrainBlocks))
            {
                return true;
            }

            if (unitsBlock)
            {
                if (GetUnit(cellCoords))
                {
                    return true;
                }
            }
            return false;
    }

    public bool IsImpassableTile(CubeCoord cubeCoords) 
    {
        return IsImpassableTile(CubeToUnityCell(cubeCoords));
    }

    public bool IsHazardous(Vector3Int cellCoords)
    {
        TileBase tile = map.GetTile(cellCoords);
        if (tile == null)
        {
            Debug.LogError("Tile does not exist in tilemap");
        }
        else if(baseTileDatas[tile].hazardous)
        {
            return true;
        }
        return false;       
    }

    public bool IsHazardous(CubeCoord cubeCoords)
    {
        return IsHazardous(CubeToUnityCell(cubeCoords));
    }

    /// <summary>
    /// returns true if there exists a tile in the tilemap
    /// at the coordinate. False otherwise
    /// </summary>
    public bool InBounds(Vector3Int cellCoords)
    {
        if (GetTile(cellCoords))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the distance between two coordinates on the map, ignoring impassable
    /// tiles and other blockers. https://www.redblobgames.com/grids/hexagons/#distances
    /// </summary>
    public int Distance(CubeCoord start, CubeCoord end)
    {
        CubeCoord temp = start - end;
        return Mathf.Max(Mathf.Abs(temp.x), Mathf.Abs(temp.y), Mathf.Abs(temp.z));
    }

    /// <summary>
    /// Returns the distance between two coordinates on the map, ignoring impassable
    /// tiles and other blockers. https://www.redblobgames.com/grids/hexagons/#distances
    /// </summary>
    public int Distance(Vector3Int start, Vector3Int end) 
    {
        return Distance(UnityCellToCube(start), UnityCellToCube(end));
    }

    /// <summary>
    /// Returns the distance between two coordinates on the map while accounting for impassable
    /// tiles. If no path is found, then return an arbitrarily large number.
    /// </summary>
    public int RealDistance(Vector3Int start, Vector3Int end, bool unitBlocks = true)
    {
        int dist = FindShortestPath(start, end, unitBlocks).Count;
        if (dist == 0)
        {
            dist = 10000;
        }
        return dist;
    }

    /// <summary>
    /// Returns a list of coordinates which comprise the shortest path between the start and goal.
    /// Contains the destination tile but not the starting tile.
    /// Assumes all tiles have equal weighting/value
    /// </summary>
    public List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, bool unitBlocks = true, bool terrainBlocks = true)
    {   
        return FindShortestPath(start, goal, (pos) => 10, unitBlocks, terrainBlocks);
    }

    /// <summary>
    /// Returns a list of coordinates which comprise the shortest path between the start and goal.
    /// Contains the destination tile but not the starting tile
    /// </summary>
    public List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, System.Func<Vector3Int, float> tileCostFunction, bool unitBlocks = true,
                bool terrainBlocks = true)
    {
        if(!map.GetTile(start))
        {
            Debug.LogError("Starting tile does not exist on map");
            return new List<Vector3Int>();
        }
        if(!map.GetTile(goal))
        {
            Debug.LogError("Goal tile does not exist on map");
            return new List<Vector3Int>();
        }

        SimplePriorityQueue<Vector3Int> frontier = new SimplePriorityQueue<Vector3Int>();
        frontier.Enqueue(start, 0);
        Dictionary<Vector3Int, Vector3Int> came_from = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, float> cost_so_far = new Dictionary<Vector3Int, float>();
        came_from[start] = start;
        cost_so_far[start] = 0;

        while(frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            if(current == goal)
            {
                break;
            }

            foreach(CubeDirections direction in System.Enum.GetValues(typeof(CubeDirections)))
            {
                Vector3Int next = CubeToUnityCell(CubeNeighbor(current, direction));
                if(!IsImpassableTile(next, unitBlocks, terrainBlocks) || next == goal)
                {
                    float new_cost = cost_so_far[current] + tileCostFunction(next);
                    if(!cost_so_far.ContainsKey(next) || new_cost < cost_so_far[next])
                    {
                        cost_so_far[next] = new_cost;
                        float priority = new_cost + Distance(next, goal);
                        frontier.Enqueue(next, priority);
                        came_from[next] = current;
                    }
                }
            }
        }

        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int last = goal;
        if(!came_from.ContainsKey(last))
        {
            return path;
        }
        path.Insert(0, last);
        while(!came_from[last].Equals(start))
        {
            last = came_from[last];
            path.Insert(0, last);
        }
        
        return path;
    }

    public List<Vector3Int> GetTilesInRange(Vector3Int start, int range, bool unitsBlock = true)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        path.Add(start);
        var frontier = new Queue<Vector3Int>();
        var nextFrontier = new Queue<Vector3Int>();
        frontier.Enqueue(start);
        /*Dictionary<Vector3Int, Vector3Int> came_from = new Dictionary<Vector3Int, Vector3Int>();
        came_from[start] = start;*/

        for(int i = 0; i < range; i++)
        {
            while(frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (CubeDirections direction in System.Enum.GetValues(typeof(CubeDirections)))
                {
                    Vector3Int next = CubeToUnityCell(CubeNeighbor(current, direction));
                    if (IsImpassableTile(next, unitsBlock)) continue;
                    if (!path.Contains(next))
                    {
                        nextFrontier.Enqueue(next);
                        path.Add(next);
                    }
                }
            }
            Queue<Vector3Int> temp = frontier;
            frontier = nextFrontier;
            nextFrontier = temp;
        }

        return path;
    }

    /// <summary>
    /// Returns a list of tiles which are within a given range of the starting tile using only straight lines which intersect
    /// hexagon midpoints
    /// </summary>
    public List<Vector3Int> GetTilesInRangeStraight(Vector3Int start, int range, bool unitsBlock = false)
    {
        CubeCoord cubeStart = UnityCellToCube(start);
        List<Vector3Int> tiles = new List<Vector3Int>();

        foreach (CubeDirections direction in System.Enum.GetValues(typeof(CubeDirections)))
        {
            for (int i = 1; i <= range; i++)
            {
                CubeCoord coord = cubeStart + (GetCubeDirection(direction) * i);
                tiles.Add(CubeToUnityCell(coord));
            }
        }
        return tiles;
    }

    /// <summary>
    /// Returns a unit vector in the direction between start and end. Requires range:
    /// an int which indicates the distance between start and end.
    /// </summary>
    public CubeCoord GetDirection(Vector3Int start, Vector3Int end, int range)
    {
        CubeCoord cubeStart = UnityCellToCube(start);
        CubeCoord cubeEnd = UnityCellToCube(end);
        Debug.Log("start: " + cubeStart);
        Debug.Log("end: " + cubeEnd);

        CubeCoord distance = cubeEnd - cubeStart;
        Debug.Log("distance: " + distance);
        if (distance.x % range == 0 && distance.y % range == 0 && distance.z % range == 0)
        {
            CubeCoord direction = new CubeCoord(distance.x / range, distance.y / range, distance.z / range);
            return direction;
        }
        else
        {
            Debug.LogError("Not a straight line");
            return new CubeCoord(-1, -1, -1);
        }
    }

    public List<Vector3Int> FindShortestPathBFS(Vector3Int start, Vector3Int goal)
    {
        var frontier = new Queue<Vector3Int>();
        frontier.Enqueue(start);
        Dictionary<Vector3Int, Vector3Int> came_from = new Dictionary<Vector3Int, Vector3Int>();
        came_from[start] = start;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (CubeDirections direction in System.Enum.GetValues(typeof(CubeDirections)))
            {
                Vector3Int next = CubeToUnityCell(CubeNeighbor(current, direction));
                if (IsImpassableTile(next)) continue;
                if (!came_from.ContainsKey(next))
                {
                    frontier.Enqueue((Vector3Int)next);
                    came_from[next] = current;
                }
            }
        }
        var traverse = goal;
        var path = new List<Vector3Int>();
        while (traverse != start)
        {
            path.Add(traverse);
            traverse = came_from[traverse];
        }

        return path;

    }

    public bool FindClosestFreeTile(Vector3Int start, out Vector3Int end)
    {
        end = start;
        if(!IsImpassableTile(start))
        {
            return true;
        }

        var frontier = new Queue<Vector3Int>();
        frontier.Enqueue(start);

        while(frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (CubeDirections direction in System.Enum.GetValues(typeof(CubeDirections)))
            {
                Vector3Int next = CubeToUnityCell(CubeNeighbor(current, direction));
                if (!InBounds(next)) continue;
                if (IsImpassableTile(next))
                {
                    frontier.Enqueue(next);
                } else if (InBounds(next))
                {
                    end = next;
                    return true;
                }
            }
        }

        // No free tiles found
        return false;
    }

    /// <summary>
    /// Returns true if path contained can be traversed with a straight line
    /// through the midpoints of the hexagons.
    /// </summary>
    public bool IsStraightPath(List<Vector3Int> path)
    {
        bool xSame = true;
        bool ySame = true;
        bool zSame = true;
        bool straight = true;
        int x = -1;
        int y = -1;
        int z = -1;

        foreach (Vector3Int coord in path)
        {
            CubeCoord curCubeCoord = UnityCellToCube(coord);
            if (x != -1 && x != curCubeCoord.x)
            {
                xSame = false;
            }
            else if (xSame)
            {
                x = curCubeCoord.x;
            }

            if (y != -1 && y != curCubeCoord.y)
            {
                ySame = false;
            }
            else if (ySame)
            {
                y = curCubeCoord.y;
            }

            if (z != -1 && z != curCubeCoord.z)
            {
                zSame = false;
            }
            else if (zSame)
            {
                z = curCubeCoord.z;
            }

            if (xSame)
            {
                straight = ySame || zSame;
            }
            else
            {
                straight = ySame && zSame;
            }
            if (!straight)
            {
                break;
            }
        }
        return straight;
    }

    public TileBase GetTile(Vector3Int tilePos)
    {
        return map.GetTile(tilePos);
    }

    /// <summary>
    /// Sets the color of the tile at the given position.
    /// Adds the tile to a list of tiles with their colors changed so
    /// they can all be reset in ClearHighlights()
    /// </summary>
    /// <param name="cellCoord">the position of the tile to change</param>
    /// <param name="color">the new color for the tile</param>
    public void SetTileColor(Vector3Int cellCoord, Color color, bool tutorial = false)
    {
        map.SetTileFlags(cellCoord, TileFlags.None);
        map.SetColor(cellCoord, color);
        if (!tutorial)
        {
            coloredTiles.Add(cellCoord);
        }
        else
        {
            coloredTilesTutorial.Add(cellCoord, color);
        }

    }

    /// <summary>
    /// Returns the color of the tile at the given tile position
    /// </summary>
    /// <param name="tilePos">the position of the tile to retrieve</param>
    /// <returns>the color of the tile at the given position</returns>
    public Color GetTileColor(Vector3Int tilePos)
    {
        return map.GetColor(tilePos);
    }

    public void HighlightPath(List<Vector3Int> path, Color color)
    {
        foreach(Vector3Int tile in path)
        {
            SetTileColor(tile, color);
        }
    }

    public void ClearHighlights(bool tutorial = false)
    {
        if (!tutorial)
        {
            foreach(Vector3Int tile in coloredTiles)
            {
                map.SetColor(tile, Color.white);
            }
            coloredTiles.Clear();

            foreach(var tile in coloredTilesTutorial)
            {
                map.SetColor(tile.Key, tile.Value);
            }
        }
        else
        {
            foreach(var tile in coloredTilesTutorial)
            {
                map.SetColor(tile.Key, Color.white);
            }     
            coloredTilesTutorial.Clear();       
        }

    }

    public CubeCoord CubeNeighbor(Vector3Int cellCoord, CubeDirections direction)
    {
        return CubeNeighbor(UnityCellToCube(cellCoord), direction);
    }

    public CubeCoord CubeNeighbor(CubeCoord cubeCoords, CubeDirections direction)
    {
        return cubeCoords + GetCubeDirection(direction);
    }

    private CubeCoord GetCubeDirection(CubeDirections direction)
    {
        return directions[(int) direction];
    }

    public CubeCoord UnityCellToCube(Vector3Int cell)
    {
        var col = cell.x; 
        var row = cell.y * -1;
        var q = col - (row - (row & 1)) / 2;
        var r = row;
        var s = -q - r;
        return new CubeCoord(q, r, s);
    }

    public Vector3Int CubeToUnityCell(CubeCoord cube)
    {
        var q = cube.x;
        var r = cube.y;
        var col = q + (r - (r & 1)) / 2;
        var row = r * -1;

        return new Vector3Int(col, row,  0);
    }

}
