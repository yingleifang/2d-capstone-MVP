using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using Priority_Queue;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography;

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
        return new CubeCoord(a.coords + b.coords);
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

    private Dictionary<TileBase, TileDataScriptableObject> baseTileDatas;  
    public Dictionary<Vector3Int, DynamicTileData> dynamicTileDatas;
    public Dictionary<Unit, Vector3Int> unitLocations;
    public static TileManager Instance {get; private set;}

    LevelManager levelManager;

    private void SetMapConfig()
    {
        foreach (var info in levelManager.tileInfo)
        {   
            map.SetTile(info.Item2, info.Item1.tiles[0]);
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
            //map.ClearAllTiles();
        SetMapConfig();
        baseTileDatas = new Dictionary<TileBase, TileDataScriptableObject>();
        dynamicTileDatas = new Dictionary<Vector3Int, DynamicTileData>();
        unitLocations = new Dictionary<Unit, Vector3Int>();
        foreach (TileDataScriptableObject tileData in levelManager.typesOfTilesToSpawn)
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
                    dynamicTileDatas.Add(new Vector3Int(x, y, z), new DynamicTileData());
                }
            }
        }
    }

    public Vector3Int GetTileAtScreenPosition(Vector3 pos)
    {
        Vector2 screenPos = Camera.main.ScreenToWorldPoint(pos);
        return map.WorldToCell(screenPos);
    }

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

    public Unit GetUnit(Vector3Int tilePos)
    {
        if (!dynamicTileDatas.ContainsKey(tilePos))
        {
            return null;
        }
        return dynamicTileDatas[tilePos].unit;
    }

    public void AddUnitToTile(Vector3Int location, Unit unit)
    {
        if (unitLocations.ContainsKey(unit))
        {
            RemoveUnitFromTile(unitLocations[unit]);
        }
        dynamicTileDatas[location].unit = unit;
        unitLocations[unit] = location;
        unit.location = location;
    }

    public void KillUnit(Vector3Int location)
    {
        if(dynamicTileDatas.ContainsKey(location))
        {
            unitLocations.Remove(dynamicTileDatas[location].unit);
            dynamicTileDatas[location].unit = null;
        }
    }

    public void RemoveUnitFromTile(Vector3Int location)
    {
        if (dynamicTileDatas.ContainsKey(location))
        {
            unitLocations.Remove(dynamicTileDatas[location].unit);
            dynamicTileDatas[location].unit = null;
        }
    }

    public int Distance(CubeCoord start, CubeCoord end)
    {
        CubeCoord temp = start - end;
        return Mathf.Max(Mathf.Abs(temp.x), Mathf.Abs(temp.y), Mathf.Abs(temp.z));
    }

    public int Distance(Vector3Int start, Vector3Int end) 
    {
        return Distance(UnityCellToCube(start), UnityCellToCube(end));
    }

    public int RealDistance(Vector3Int start, Vector3Int end, bool unitBlocks = true)
    {
        return FindShortestPath(start, end, unitBlocks).Count;
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

    public bool IsImpassable(Vector3Int cellCoords, bool unitsBlock = true)
    {
        TileBase tile = map.GetTile(cellCoords);
        if(tile == null || baseTileDatas[tile].impassable)
        {
            return true;
        }

        if(unitsBlock)
        {
            if(GetUnit(cellCoords))
            {
                return true;
            }
        }
        return false;
    }

    public bool IsImpassable(CubeCoord cubeCoords) 
    {
        return IsImpassable(CubeToUnityCell(cubeCoords));
    }

    public bool IsHazardous(Vector3Int cellCoords)
    {
        TileBase tile = map.GetTile(cellCoords);
        if(tile == null || baseTileDatas[tile].hazardous)
        {
            return true;
        }
        return false;       
    }

    public bool IsHazardous(CubeCoord cubeCoords)
    {
        return IsHazardous(CubeToUnityCell(cubeCoords));
    }

    public bool InBounds(Vector3Int cellCoords)
    {
        if (GetTile(cellCoords))
        {
            return true;
        }
        return false;
    }

    public List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, bool unitBlocks = true)
    {
        return FindShortestPath(start, goal, (pos) => 10, unitBlocks);
    }

    public List<Vector3Int> FindShortestPath(Vector3Int start, Vector3Int goal, System.Func<Vector3Int, float> tileCostFunction, bool unitBlocks = true)
    {
        if(!map.GetTile(start))
        {
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
                if(!IsImpassable(next, unitBlocks) || next == goal)
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
                    if (IsImpassable(next, unitsBlock)) continue;
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
                if (IsImpassable(next)) continue;
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
        if(!IsImpassable(start))
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
                if (IsImpassable(next))
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

    public TileBase GetTile(Vector3Int tilePos)
    {
        return map.GetTile(tilePos);
    }


    public void SetTileColor(Vector3Int cellCoord, Color color)
    {
        map.SetTileFlags(cellCoord, TileFlags.None);
        map.SetColor(cellCoord, color);
        coloredTiles.Add(cellCoord);
    }

    public void HighlightPath(List<Vector3Int> path, Color color)
    {
        foreach(Vector3Int tile in path)
        {
            SetTileColor(tile, color);
        }
    }

    public void ClearHighlights()
    {
        foreach(Vector3Int tile in coloredTiles)
        {
            map.SetColor(tile, Color.white);
        }
        coloredTiles.Clear();
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

    private CubeCoord UnityCellToCube(Vector3Int cell)
    {
        var col = cell.x; 
        var row = cell.y * -1;
        var q = col - (row - (row & 1)) / 2;
        var r = row;
        var s = -q - r;
        return new CubeCoord(q, r, s);
    }

    private Vector3Int CubeToUnityCell(CubeCoord cube)
    {
        var q = cube.x;
        var r = cube.y;
        var col = q + (r - (r & 1)) / 2;
        var row = r * -1;

        return new Vector3Int(col, row,  0);
    }

}
