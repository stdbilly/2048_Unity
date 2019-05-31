using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public static int gridWidth = 4, gridHeight = 4;
    public static Transform[,] grid = new Transform[gridWidth, gridHeight];
    public Canvas gameOverCanvas;
    public Text gameScoreText;
    public Text bestScoreText;
    public int score = 0;

    private int numberOfCoroutinesRunning = 0;
    private bool generatedNewTileThisTurn = true;

    public AudioClip moveTilesSound;
    public AudioClip mergeTilesSound;

    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        GenerateNewTile(2);

        audioSource = transform.GetComponent<AudioSource>();

        UpdateBestSocer();
    }

    // Update is called once per frame
    void Update()
    {
        if (numberOfCoroutinesRunning == 0)
        {
            if (!generatedNewTileThisTurn)
            {
                generatedNewTileThisTurn = true;
                GenerateNewTile(1);
            }

            if (!CheckGameOver())
            {
                CheckUserInput();
            }
            else
            {
                SavedBestScore();
                UpdateScore();
                gameOverCanvas.gameObject.SetActive(true);
            }
        }
          
    }

    void CheckUserInput()
    {
        bool down = Input.GetKeyDown(KeyCode.DownArrow);
        bool up = Input.GetKeyDown(KeyCode.UpArrow);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow);
        bool right = Input.GetKeyDown(KeyCode.RightArrow);
        if (down || up || left || right)
        {
            PrepareTileForMerging();

            if (down) {
                MoveAllTiles(Vector2.down);
            }
            if (up) {
                MoveAllTiles(Vector2.up);
            }         
            if (left) {
                MoveAllTiles(Vector2.left);
            }
            if (right) {
                MoveAllTiles(Vector2.right);
            }
        }
    }

    void UpdateScore()
    {
        gameScoreText.text = score.ToString("000000000");
    }

    void UpdateBestSocer()
    {
        bestScoreText.text = PlayerPrefs.GetInt("bestscore").ToString();
    }

    void SavedBestScore()
    {
        int oldBestScore = PlayerPrefs.GetInt("bestscore");
        if (score > oldBestScore)
        {
            PlayerPrefs.SetInt("bestscore", score);
        }
    }

    bool CheckGameOver()
    {

        if (transform.childCount < gridWidth * gridWidth) //方块没有填满
        {
            return false;
        } 
            

        for(int x = 0; x < gridWidth; x++)
        {
            for(int y = 0; y < gridHeight; y++)
            {
                Transform currentTile = grid[x, y];
                Transform tileBelow = null;
                Transform tileBeside = null;

                if (y != 0) //方块不在最下面
                    tileBelow = grid[x, y - 1];

                if (x != gridWidth - 1) //方块不在最右边
                    tileBeside = grid[x + 1, y];

                if (tileBeside != null) //如果右边有方块，检查两者值是否相等
                {
                    if (currentTile.GetComponent<Tile>().tileValue == tileBeside.GetComponent<Tile>().tileValue)
                    {
                        return false;
                    }
                }

                if (tileBelow != null) //如果下边有方块，检查两者值是否相等
                {
                    if (currentTile.GetComponent<Tile>().tileValue == tileBelow.GetComponent<Tile>().tileValue)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    void MoveAllTiles(Vector2 direction)
    {
        int tilesCount = 0;
        UpdateGrid();

        if (direction == Vector2.left)//从左到右，从上到下移动
        {
            for(int x = 0; x < gridWidth; x++)
            {
                for(int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesCount++;
                    }
                }
            }
        }

        if (direction == Vector2.right)//从右到左,从上到下
        {
            for(int x = gridWidth-1; x >= 0; x--)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesCount++;
                    }
                }
            }
        }

        if (direction == Vector2.down)//从左到右，从上到下
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesCount++;
                    }
                }
            }
        }

        if (direction == Vector2.up)//从左到右，从下到上
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = gridHeight-1; y >=0 ; y--)
                {
                    if (grid[x, y] != null)
                    {
                        if (MoveTile(grid[x, y], direction))
                            tilesCount++;
                    }
                }
            }
        }

        if (tilesCount != 0)
        {
            generatedNewTileThisTurn = false;

            audioSource.PlayOneShot(moveTilesSound);
        }

        for(int x = 0; x < gridWidth; ++x)
        {
            for(int y = 0; y < gridHeight; ++y)
            {
                if (grid[x, y] != null)
                {
                    Transform t = grid[x, y];
                    StartCoroutine(SlidTile(t.gameObject, 10f));
                }
            }
        }
    }

    bool MoveTile(Transform tile,Vector2 direction)//移动一个方块，并检查是否移动
    {
        Vector2 startPos = tile.localPosition;
        Vector2 phantomTilePosition = tile.localPosition;

        tile.GetComponent<Tile>().startingPosition = startPos;

        while (true)
        {
            phantomTilePosition += direction;
            Vector2 previousPosition = phantomTilePosition - direction;
            
            if (CheckInsideGride(phantomTilePosition))
            {
                if (CheckIsAtValidPosition(phantomTilePosition))
                {
                    tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;
                    grid[(int)previousPosition.x, (int)previousPosition.y] = null;
                    grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = tile;
                }
                else
                {
                    if (!CheckAndCombineTiles(tile,phantomTilePosition,previousPosition))
                    {
                        phantomTilePosition += -direction; //让方块回到原来的位置
                        tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

                        if (phantomTilePosition == startPos)
                        {
                            return false; //方块没有移动
                        }
                        else
                        {
                            return true;
                        }
                    }
                }             
            }
            else
            {
                phantomTilePosition += -direction; //让方块回到原来的位置
                tile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

                if (phantomTilePosition == startPos)
                {
                    return false; //方块没有移动
                }
                else
                {
                    return true;
                }
            }
        }
    }

    bool CheckAndCombineTiles(Transform movingTile,Vector2 phantomTilePosition,Vector2 previousPosition)
    {
        Vector2 pos = movingTile.transform.localPosition;

        Transform collidingTile = grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y];

        int movingTileValue = movingTile.GetComponent<Tile>().tileValue;
        int collidingTileValue=collidingTile.GetComponent<Tile>().tileValue;
        //将值相同的可以合并的,并且这次没有合并过的两个方块删除，然后在合并的位置生成一个新的方块,值为两个方块的2倍
        if (movingTileValue == collidingTileValue && !movingTile.GetComponent<Tile>().mergedThisTurn && !collidingTile.GetComponent<Tile>().mergedThisTurn && !collidingTile.GetComponent<Tile>().willMergeWithCollidingTile)
        {
            movingTile.GetComponent<Tile>().destroyMe = true;
            movingTile.GetComponent<Tile>().collidingTile = collidingTile;
            movingTile.GetComponent<Tile>().moveToPosition = phantomTilePosition;

            grid[(int)previousPosition.x, (int)previousPosition.y] = null;
            grid[(int)phantomTilePosition.x, (int)phantomTilePosition.y] = movingTile;

            movingTile.GetComponent<Tile>().willMergeWithCollidingTile = true;

            UpdateScore();
            return true;
        }

        return false;
    }

    void GenerateNewTile(int howMany) {
        for(int i = 0; i < howMany; ++i)
        {
            Vector2 locationForNewTile = GetRandomLocationForNewTile();
            string tile = "tile_2";
            float chanceOfTwo = Random.Range(0f, 1f);
            if (chanceOfTwo > 0.9f) //90%的概率生成方块2
            {
                tile = "tile_4";
            }

            GameObject newTile = (GameObject)Instantiate(Resources.Load(tile, typeof(GameObject)), locationForNewTile, Quaternion.identity);
            newTile.transform.parent = transform;

            grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localPosition.y] = newTile.transform;
            newTile.transform.localScale = new Vector2(0, 0);
            newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);

            StartCoroutine(NewTilePopIn(newTile, new Vector2(0, 0), new Vector2(1, 1), 10f, newTile.transform.localPosition, new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));
        }
    }

    void UpdateGrid()
    {
        for(int x = 0; x < gridWidth; x++)
        {
            for(int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    if (grid[x, y].parent == transform)
                        grid[x, y] = null;
                }
            }
        }
        
        foreach(Transform tile in transform)
        {
            Vector2 v = new Vector2(Mathf.Round(tile.position.x), Mathf.Round(tile.position.y));
            grid[(int)v.x, (int)v.y] = tile;
        }
    }

    Vector2 GetRandomLocationForNewTile()
    {
        List<int> x = new List<int>();
        List<int> y = new List<int>();

        for(int i = 0; i < gridWidth; i++)
        {
            for(int j = 0; j < gridHeight; j++)
            {
                if (grid[i, j] == null)
                {
                    x.Add(i);
                    y.Add(j);
                }
            }
        }

        int randIndex = Random.Range(0, x.Count);
        int randX = x.ElementAt(randIndex);
        int randY = y.ElementAt(randIndex);

        return new Vector2(randX, randY);
    }

    bool CheckInsideGride(Vector2 pos)
    {
        if(pos.x>=0&&pos.x<=gridWidth-1&& pos.y >= 0 && pos.y <= gridHeight - 1)
        {
            return true;
        }
        return false;
    }

    bool CheckIsAtValidPosition(Vector2 pos)//检查要移动的方向是不是已存在方块
    {
        if (grid[(int)pos.x, (int)pos.y] == null)
        {
            return true;
        }
        return false;
    }

    void PrepareTileForMerging()
    {
        foreach(Transform t in transform)
        {
            t.GetComponent<Tile>().mergedThisTurn = false;
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void playAgain()
    {
        grid = new Transform[gridWidth, gridHeight];
        score = 0;
        List<GameObject> children = new List<GameObject>();
        foreach(Transform t in transform)
        {
            children.Add(t.gameObject);
        }
        children.ForEach(t => DestroyImmediate(t));
        gameOverCanvas.gameObject.SetActive(false);
        UpdateScore();
        UpdateBestSocer();
        GenerateNewTile(2);
    }

    IEnumerator NewTilePopIn(GameObject tile,Vector2 initialScale,Vector2 finalScale,float timeScale,Vector2 initialPosition,Vector2 finalPosition)
    {
        numberOfCoroutinesRunning++;

        float progress = 0;
        while (progress <= 1)
        {
            tile.transform.localScale = Vector2.Lerp(initialScale, finalScale, progress);
            tile.transform.localPosition = Vector2.Lerp(initialPosition, finalPosition, progress);

            progress += Time.deltaTime * timeScale;
            yield return null;
        }

        tile.transform.localScale = finalScale;
        tile.transform.localPosition = finalPosition;

        numberOfCoroutinesRunning--;
    }

    IEnumerator SlidTile(GameObject tile,float timeScale)
    {
        numberOfCoroutinesRunning++;

        float progress = 0;
        while (progress <= 1)
        {
            tile.transform.localPosition = Vector2.Lerp(tile.GetComponent<Tile>().startingPosition, tile.GetComponent<Tile>().moveToPosition, progress);
            progress += Time.deltaTime * timeScale;
            yield return null;
        }
        tile.transform.localPosition = tile.GetComponent<Tile>().moveToPosition;

        if (tile.GetComponent<Tile>().destroyMe)
        {
            int movingTileValue = tile.GetComponent<Tile>().tileValue;
            if (tile.GetComponent<Tile>().collidingTile != null)
            {
                DestroyImmediate(tile.GetComponent<Tile>().collidingTile.gameObject);
            }

            Destroy(tile.gameObject);

            string newTileName = "tile_" + movingTileValue * 2;
            score += movingTileValue * 2;

            audioSource.PlayOneShot(mergeTilesSound);

            GameObject newTile = (GameObject)Instantiate(Resources.Load(newTileName, typeof(GameObject)), tile.transform.localPosition, Quaternion.identity);
            newTile.transform.parent = transform;
            newTile.GetComponent<Tile>().mergedThisTurn = true;

            grid[(int)newTile.transform.localPosition.x, (int)newTile.transform.localPosition.y] = newTile.transform;
            newTile.transform.localScale = new Vector2(0, 0);
            newTile.transform.localPosition = new Vector2(newTile.transform.localPosition.x + 0.5f, newTile.transform.localPosition.y + 0.5f);

            yield return StartCoroutine(NewTilePopIn(newTile, new Vector2(0, 0), new Vector2(1, 1), 10f, newTile.transform.localPosition, new Vector2(newTile.transform.localPosition.x - 0.5f, newTile.transform.localPosition.y - 0.5f)));
        }

        numberOfCoroutinesRunning--;
    }
}