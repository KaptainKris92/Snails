using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// Builds level scenes from text maps in Assets/Levels. One character is one world unit, drawn as
/// a 2x2 block of half-unit tiles on a Tilemap with a single CompositeCollider2D (no seams).
///
///   #  solid        /  floor slope rising to the right     \  floor slope falling to the right
///   q  ceiling slope, solid top-left                        p  ceiling slope, solid top-right
///   ^  thorns (deadly)   S  spawn   E  end zone (any number of cells)   . or space  air
///   lines starting with // are comments
///
/// The scene is created from Assets/Scenes/LevelTemplate.unity, which carries the camera, UI,
/// timer and leaderboard objects of a normal level without any geometry.
public static class LevelBuilder
{
    private const string LevelsDir = "Assets/Levels";
    private const string TilesDir = "Assets/Tiles";
    private const string ScenesDir = "Assets/Scenes";
    private const string SpritesDir = "Assets/Sprites/1-unit sprites";
    private const string TemplateScene = "Assets/Scenes/LevelTemplate.unity";
    private const string SnailPrefab = "Assets/Prefabs/4-unit prefabs/Snail.prefab";
    private const float Cell = 0.5f;
    private const int EnvironmentLayer = 7;

    [MenuItem("Snails/Build All Levels From Text")]
    public static void BuildAll()
    {
        foreach (string path in Directory.GetFiles(LevelsDir, "*.txt").OrderBy(p => p))
            Build(path.Replace('\\', '/'));
    }

    [MenuItem("Snails/Create Level Template From Level 1")]
    public static void CreateTemplateFromLevel1()
    {
        var scene = EditorSceneManager.OpenScene(ScenesDir + "/Level 1.unity", OpenSceneMode.Single);
        var remove = new[] { "1x1Grid", "2x2Grid", "EndZone", "Snail" };
        foreach (var root in scene.GetRootGameObjects())
        {
            bool geometry = remove.Contains(root.name) || root.GetComponentInChildren<Rigidbody2D>() != null
                            || root.GetComponent<Grid>() != null || root.GetComponent<EndZone>() != null;
            if (geometry)
                UnityEngine.Object.DestroyImmediate(root);
        }
        EditorSceneManager.SaveScene(scene, TemplateScene);
        Debug.Log("[LevelBuilder] Template saved to " + TemplateScene);
    }

    public static void Build(string textPath)
    {
        string levelName = Path.GetFileNameWithoutExtension(textPath);
        string[] rows = ParseRows(textPath);
        int height = rows.Length;
        int width = rows.Max(r => r.Length);
        rows = rows.Select(r => r.PadRight(width, '.')).ToArray();
        // rows[0] is the top of the map. Map char (col,row) covers x in [col, col+1], y in [height-1-row, height-row].

        var scene = EditorSceneManager.OpenScene(TemplateScene, OpenSceneMode.Single);

        // Sub-cell solidity for grass decoration, at half-unit resolution.
        int sw = width * 2, sh = height * 2;
        var solid = new bool[sw, sh];
        var subTiles = new string[sw, sh]; // sprite name per sub cell, null = empty
        var thorns = new bool[sw, sh];
        var endCells = new List<Vector2Int>();
        Vector2 spawn = new Vector2(1.5f, 1.5f);
        bool spawnFound = false;
        var rng = new System.Random(levelName.GetHashCode());

        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                char c = rows[row][col];
                int cx = col * 2, cy = (height - 1 - row) * 2; // bottom-left sub cell of this char
                switch (c)
                {
                    case '#':
                        Set(solid, subTiles, cx, cy, "#"); Set(solid, subTiles, cx + 1, cy, "#");
                        Set(solid, subTiles, cx, cy + 1, "#"); Set(solid, subTiles, cx + 1, cy + 1, "#");
                        break;
                    case '/': // solid below the diagonal from bottom-left to top-right
                        Set(solid, subTiles, cx, cy, "RATriangle_UL"); Set(solid, subTiles, cx + 1, cy, "#");
                        Set(solid, subTiles, cx + 1, cy + 1, "RATriangle_UL");
                        break;
                    case '\\': // solid below the diagonal from top-left to bottom-right
                        Set(solid, subTiles, cx, cy, "#"); Set(solid, subTiles, cx + 1, cy, "RATriangle_UR");
                        Set(solid, subTiles, cx, cy + 1, "RATriangle_UR");
                        break;
                    case 'q': // solid above the diagonal from top-left to bottom-right
                        Set(solid, subTiles, cx, cy + 1, "RATriangle_BR"); Set(solid, subTiles, cx + 1, cy + 1, "#");
                        Set(solid, subTiles, cx + 1, cy, "RATriangle_BR");
                        break;
                    case 'p': // solid above the diagonal from bottom-left to top-right
                        Set(solid, subTiles, cx, cy + 1, "#"); Set(solid, subTiles, cx + 1, cy + 1, "RATriangle_BL");
                        Set(solid, subTiles, cx, cy, "RATriangle_BL");
                        break;
                    case '^':
                        thorns[cx, cy] = thorns[cx + 1, cy] = thorns[cx, cy + 1] = thorns[cx + 1, cy + 1] = true;
                        break;
                    case 'S':
                        spawn = new Vector2(col + 0.5f, height - row - 0.5f);
                        spawnFound = true;
                        break;
                    case 'E':
                        endCells.Add(new Vector2Int(col, height - 1 - row));
                        break;
                }
            }
        }
        if (!spawnFound)
            Debug.LogWarning("[LevelBuilder] " + levelName + " has no S spawn; using (1.5, 1.5)");

        // Decorate full sub cells with grass where they are exposed.
        for (int x = 0; x < sw; x++)
            for (int y = 0; y < sh; y++)
                if (subTiles[x, y] == "#")
                    subTiles[x, y] = GrassFor(solid, x, y, rng);

        // ---- terrain
        var gridGo = new GameObject("Terrain");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(Cell, Cell, 0f);

        Tilemap ground = MakeTilemap(gridGo.transform, "Ground", EnvironmentLayer, false, 0);
        Tilemap thornMap = MakeTilemap(gridGo.transform, "Thorns", 0, true, 1);
        thornMap.gameObject.AddComponent<RedThorns>();

        var tiles = new Dictionary<string, TileBase>();
        for (int x = 0; x < sw; x++)
        {
            for (int y = 0; y < sh; y++)
            {
                if (subTiles[x, y] != null)
                    ground.SetTile(new Vector3Int(x, y, 0), GetTile(tiles, subTiles[x, y]));
                if (thorns[x, y])
                    thornMap.SetTile(new Vector3Int(x, y, 0), GetTile(tiles, "RedThorns"));
            }
        }

        // Edit mode does not run physics, so the collider geometry has to be generated by hand.
        foreach (var tilemapCollider in gridGo.GetComponentsInChildren<TilemapCollider2D>())
            tilemapCollider.ProcessTilemapChanges();
        foreach (var composite in gridGo.GetComponentsInChildren<CompositeCollider2D>())
            composite.GenerateGeometry();

        // ---- end zone
        if (endCells.Count > 0)
        {
            var endGo = new GameObject("EndZone");
            int minX = endCells.Min(c => c.x), maxX = endCells.Max(c => c.x);
            int minY = endCells.Min(c => c.y), maxY = endCells.Max(c => c.y);
            endGo.transform.position = new Vector3((minX + maxX + 1) * 0.5f, (minY + maxY + 1) * 0.5f, 0f);
            var box = endGo.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(maxX - minX + 1, maxY - minY + 1);
            endGo.AddComponent<EndZone>();

            var endSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/EndZone.png");
            foreach (var c in endCells)
            {
                var cellGo = new GameObject("EndCell");
                cellGo.transform.SetParent(endGo.transform);
                cellGo.transform.position = new Vector3(c.x, c.y, 0f); // bottom-left pivot sprite
                cellGo.transform.localScale = new Vector3(2f, 2f, 1f); // 0.5 u sprite drawn 1 u
                var sr = cellGo.AddComponent<SpriteRenderer>();
                sr.sprite = endSprite;
                sr.sortingOrder = 1;
            }
        }
        else
        {
            Debug.LogWarning("[LevelBuilder] " + levelName + " has no E end zone");
        }

        // ---- kill zone below the map, in case something leaks out
        var killGo = new GameObject("KillZone");
        killGo.transform.position = new Vector3(width * 0.5f, -6f, 0f);
        var kill = killGo.AddComponent<BoxCollider2D>();
        kill.isTrigger = true;
        kill.size = new Vector2(width + 200f, 4f);
        killGo.AddComponent<RedThorns>();

        // ---- bounds, background, camera, player
        var boundsGo = new GameObject("LevelBounds");
        boundsGo.AddComponent<LevelBounds>().SetBounds(new Rect(-1f, -1f, width + 2f, height + 2f));

        var background = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Background");
        if (background != null)
        {
            var sr = background.GetComponent<SpriteRenderer>();
            float unit = sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
            background.transform.position = new Vector3(width * 0.5f, height * 0.5f, background.transform.position.z);
            background.transform.localScale = new Vector3((width + 80f) / unit, (height + 40f) / unit, 1f);
        }

        var snailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SnailPrefab);
        var snail = (GameObject)PrefabUtility.InstantiatePrefab(snailPrefab, scene);
        snail.name = "Snail";
        // The shell is a child offset from the prefab root; put the shell itself on the spawn cell.
        var shell = snail.GetComponentInChildren<RopeController>().transform;
        snail.transform.position += (Vector3)spawn - shell.position;

        var cam = Camera.main;
        if (cam != null)
            cam.transform.position = new Vector3(spawn.x, spawn.y, cam.transform.position.z);

        string scenePath = ScenesDir + "/" + levelName + ".unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AddToBuildSettings(scenePath);
        NormalizeBuildSettings();
        Debug.Log("[LevelBuilder] Built " + scenePath + " (" + width + "x" + height + ")");
    }

    /// Menu first, then OG Level, then Level N in numeric order; drops scenes missing from disk.
    [MenuItem("Snails/Sort Build Settings")]
    public static void NormalizeBuildSettings()
    {
        int Rank(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name == "MainMenu") return -2;
            if (name == "OG Level") return -1;
            string digits = new string(name.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int n) ? n : 10000;
        }

        var scenes = EditorBuildSettings.scenes
            .Where(s => File.Exists(s.path))
            .OrderBy(s => Rank(s.path))
            .ThenBy(s => s.path)
            .ToArray();
        EditorBuildSettings.scenes = scenes;
    }

    // ------------------------------------------------------------------ helpers

    private static string[] ParseRows(string path)
    {
        var rows = File.ReadAllLines(path)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !l.StartsWith("//"))
            .ToList();
        while (rows.Count > 0 && rows[rows.Count - 1].Trim().Length == 0)
            rows.RemoveAt(rows.Count - 1);
        while (rows.Count > 0 && rows[0].Trim().Length == 0)
            rows.RemoveAt(0);
        if (rows.Count == 0)
            throw new Exception("Empty level file: " + path);
        return rows.Select(r => r.Replace(' ', '.')).ToArray();
    }

    private static void Set(bool[,] solid, string[,] tiles, int x, int y, string sprite)
    {
        solid[x, y] = true;
        tiles[x, y] = sprite;
    }

    private static string GrassFor(bool[,] solid, int x, int y, System.Random rng)
    {
        bool up = !Solid(solid, x, y + 1), down = !Solid(solid, x, y - 1);
        bool left = !Solid(solid, x - 1, y), right = !Solid(solid, x + 1, y);
        int exposed = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        if (exposed == 0)
            return rng.NextDouble() < 0.8 ? "DirtSquare" + (1 + rng.Next(3)) : "DirtRockSquare" + (1 + rng.Next(6));
        if (exposed == 1)
            return up ? "Half-Grass_Up" : down ? "Half-Grass_Down" : left ? "Half-Grass_Left" : "Half-Grass_Right";
        if (exposed == 2)
        {
            if (up && left) return "CornerGrass_UL";
            if (up && right) return "CornerGrass_UR";
            if (down && left) return "CornerGrass_BL";
            if (down && right) return "CornerGrass_BR";
        }
        return "GrassBlock";
    }

    private static bool Solid(bool[,] solid, int x, int y)
    {
        if (x < 0 || y < 0 || x >= solid.GetLength(0) || y >= solid.GetLength(1))
            return true; // treat outside the map as solid so borders do not grow grass outward
        return solid[x, y];
    }

    private static Tilemap MakeTilemap(Transform parent, string name, int layer, bool trigger, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.layer = layer;
        var tilemap = go.AddComponent<Tilemap>();
        tilemap.tileAnchor = Vector3.zero; // the tile sprites have a bottom-left pivot
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var collider = go.AddComponent<TilemapCollider2D>();
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        var composite = go.AddComponent<CompositeCollider2D>();
        // Solid ground as edge chains: a rolling circle never catches on internal edges. Thorns stay
        // polygons so the trigger covers the whole area.
        composite.geometryType = trigger ? CompositeCollider2D.GeometryType.Polygons : CompositeCollider2D.GeometryType.Outlines;
        composite.isTrigger = trigger;
        return tilemap;
    }

    private static TileBase GetTile(Dictionary<string, TileBase> cache, string spriteName)
    {
        if (cache.TryGetValue(spriteName, out var cached))
            return cached;

        if (!AssetDatabase.IsValidFolder(TilesDir))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        string assetPath = TilesDir + "/" + spriteName + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/" + spriteName + ".png");
            if (sprite == null)
                throw new Exception("Missing sprite " + spriteName);
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        // Square tiles collide as flat grid cells (the grass silhouette would make the floor bumpy);
        // only slopes and thorns use their sprite outline.
        bool outline = spriteName.StartsWith("RATriangle") || spriteName.StartsWith("RedThorns");
        var wanted = outline ? Tile.ColliderType.Sprite : Tile.ColliderType.Grid;
        if (tile.colliderType != wanted)
        {
            tile.colliderType = wanted;
            EditorUtility.SetDirty(tile);
        }
        cache[spriteName] = tile;
        return tile;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath))
            return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
