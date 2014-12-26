using UnityEngine;
using System.Collections;

public class GridManager : MonoBehaviour {

  private static int rows = 4;
  private static int cols = 4;
  private static int lowestNewTileValue = 2;
  private static int highestNewTileValue = 4;
  private static float horizontalSpacingOffset = -1.65f;
  private static float verticalSpacingOffset = 1.65f;
  private static float borderSpacing = 0.1f;
  private static float raycastDistance = 0.4f;
  private static float resetButtonWidth = 80f;
  private static float resetButtonHeight = 40f;
  private static float gameOverButtonWidth = 150f;
  private static float gameOverButtonHeight = 300f;
  private static float spaceBetweenTiles = 1.1f;
  private static Vector3 horizontalRay = new Vector3(0.6f, 0f, 0f);
  private static Vector3 verticalRay = new Vector3(0f, 0.6f, 0f);
  private int points;
  private int[,] grid = new int[rows, cols];
  private int currentTilesAmount = 0;
  private GUIText scoreText;
  private Rect resetButton;
  private Rect gameOverButton;
  public GameObject[] tilePrefabs;
  public GameObject scoreObject;
  public Transform resetButtonTransform;

  private enum State {
    Loaded, 
    WaitingForInput, 
    CheckingMatches,
    GameOver
  }

  private State state;

  void OnGUI() {
    if (GUI.Button(resetButton, "Reset")) {
      Reset();
    }
    if (state == State.GameOver) {
      if (GUI.Button(gameOverButton, "Game Over")) {
        Reset();
      }
    }
  }

  void Awake() {
    state = State.Loaded;
    scoreText = scoreObject.GetComponent<GUIText>();
    Vector3 resetButtonWorldPosition = Camera.main.WorldToScreenPoint(new Vector3(resetButtonTransform.position.x,
                                                                                  -resetButtonTransform.position.y,
                                                                                  resetButtonTransform.position.z));
    resetButton = new Rect(resetButtonWorldPosition.x,
                            resetButtonWorldPosition.y,
                            resetButtonWidth,
                            resetButtonHeight);

    Vector3 gameOverButtonWorldPosition = Camera.main.WorldToScreenPoint(new Vector3(-1f, 1f, 0f));
    gameOverButton = new Rect(gameOverButtonWorldPosition.x,
                              gameOverButtonWorldPosition.y,
                              gameOverButtonWidth,
                              gameOverButtonHeight);
  }

  void Update() {
    if (state == State.Loaded) {
      state = State.WaitingForInput;
      GenerateRandomTile();
      GenerateRandomTile();
    } else if (state == State.WaitingForInput) {
      if (Input.GetButtonDown("Left")) {
        if (MoveTilesLeft()) {
          state = State.CheckingMatches;
        }
      } else if (Input.GetButtonDown("Right")) {
        if (MoveTilesRight()) {
          state = State.CheckingMatches;
        }
      } else if (Input.GetButtonDown("Up")) {
        if (MoveTilesUp()) {
          state = State.CheckingMatches;
        }
      } else if (Input.GetButtonDown("Down")) {
        if (MoveTilesDown()) {
          state = State.CheckingMatches;
        }
      } else if (Input.GetButtonDown("Reset")) {
        Reset();
      } else if (Input.GetButtonDown("Quit")) {
        Application.Quit();
      }
    } else if (state == State.CheckingMatches) {
      GenerateRandomTile();
      if (CheckForMovesLeft()) {
        state = State.WaitingForInput;
      } else {
        state = State.GameOver;
      }
    }
  }

  private bool CheckForMovesLeft() {
    if (currentTilesAmount < rows * cols) {
      return true;
    }

    for (int x = 0; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        if (x != cols - 1 && grid[x, y] == grid[x + 1, y]) {
          return true;
        } else if (y != rows - 1 && grid[x, y] == grid[x, y + 1]) {
          return true;
        }
      }
    }
    return false;
  }

  private void Reset() {
    for (int x = 0; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        if (grid[x, y] != 0) {
          GameObject currentObject = GetObjectAtGridPosition(x, y);
          grid[x, y] = 0;
          Destroy(currentObject);
        }
      }
    }

    points = 0;
    scoreText.text = "0";
    currentTilesAmount = 0;
    state = State.Loaded;
  }

  private static Vector2 GridToWorldPoint(int x, int y) {
    return new Vector2(x + horizontalSpacingOffset + borderSpacing * x, 
                       -y + verticalSpacingOffset - borderSpacing * y);
  }

  private static Vector2 WorldToGridPoint(float x, float y) {
    return new Vector2((x - horizontalSpacingOffset) / (1 + borderSpacing),
                       (y - verticalSpacingOffset) / -(1 + borderSpacing));
  }

  public void GenerateRandomTile() {
    // make sure we can create tiles
    if (currentTilesAmount >= rows * cols) {
      throw new UnityException("Unable to create new tile - grid is already full");
    }

    int value;
    // find out if we are generating a tile with the lowest or highest value
    float highOrLowChance = Random.Range(0f, 0.99f);
    if (highOrLowChance >= 0.9f) {
      value = highestNewTileValue;
    } else {
      value = lowestNewTileValue;
    }

    // attempt to get the starting position
    int x = Random.Range(0, cols);
    int y = Random.Range(0, rows);

    // starting from the random starting position, loop through
    // each cell in the grid until we find an empty positio
    bool found = false;
    while (!found) {
      if (grid[x, y] == 0) {
        found = true;
        grid[x, y] = value;
        Vector2 worldPosition = GridToWorldPoint(x, y);
        GameObject obj;
        if (value == lowestNewTileValue) {
          obj = (GameObject) Instantiate(tilePrefabs[0], worldPosition, transform.rotation);
        } else {
          obj = (GameObject) Instantiate(tilePrefabs[1], worldPosition, transform.rotation);
        }

        currentTilesAmount++;
        TileAnimationHandler tileAnimManager = obj.GetComponent<TileAnimationHandler>();
        tileAnimManager.AnimateEntry();
      }

      x++;
      if (x >= cols) {
        y++;
        x = 0;
      }

      if (y >= rows) {
        y = 0;
      }
    }
  }

  private void UpdateGrid(GameObject currentTile, Vector2 amountToMove) {
    Transform tileTransform = currentTile.transform;
    Vector2 gridPoint = WorldToGridPoint(tileTransform.position.x, tileTransform.position.y);
    grid[Mathf.RoundToInt(gridPoint.x), Mathf.RoundToInt(gridPoint.y)] = 0;

    Vector2 newPosition = currentTile.transform.position;
    newPosition += amountToMove;
    currentTile.transform.position = newPosition;

    gridPoint = WorldToGridPoint(tileTransform.position.x, tileTransform.position.y);
    Tile tile = currentTile.GetComponent<Tile>();
    grid[Mathf.RoundToInt(gridPoint.x), Mathf.RoundToInt(gridPoint.y)] = tile.value;
  }

  private void UpgradeTile(GameObject toDestroy, Tile destroyTile, GameObject toUpgrade, Tile upgradeTile) {
    Vector3 toDestroyPosition = toDestroy.transform.position;
    Vector3 toUpgradePosition = toUpgrade.transform.position;
    Vector2 upgradeGridPoint = WorldToGridPoint(toUpgradePosition.x, toUpgradePosition.y);
    Vector2 destroyGridPoint = WorldToGridPoint(toDestroyPosition.x, toDestroyPosition.y);

    // create the upgraded tile
    GameObject newTile = (GameObject) Instantiate(tilePrefabs[upgradeTile.power], toUpgradePosition, transform.rotation);

    // set the upgrade tile's grid value to double its current value
    grid[Mathf.RoundToInt(upgradeGridPoint.x), Mathf.RoundToInt(upgradeGridPoint.y)] = upgradeTile.value * 2;

    // clear out the destroyed tile's grid entry
    grid[Mathf.RoundToInt(destroyGridPoint.x), Mathf.RoundToInt(destroyGridPoint.y)] = 0;

    points += upgradeTile.value * 2;
    scoreText.text = points.ToString();

    // destroy both tiles
    Destroy(toDestroy);
    Destroy(toUpgrade);
    currentTilesAmount--;
    TileAnimationHandler tileAnim = newTile.GetComponent<TileAnimationHandler>();
    tileAnim.AnimateUpgrade();
  }

  private bool MoveTilesLeft() {
    bool hasMoved = false;
    for (int x = 1; x < cols; x++) {
      for (int y = rows - 1; y >= 0; y--) {
        if (grid[x, y] == 0) {
          continue;
        }

        GameObject currentTile = GetObjectAtGridPosition(x, y);
        bool stopped = false;

        while (!stopped) {
          // see if the position to the left is open
          RaycastHit2D hit = Physics2D.Raycast(currentTile.transform.position - horizontalRay, -Vector2.right, raycastDistance);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
                hasMoved = true;
              }
            }
            stopped = true;
          } else {
            UpdateGrid(currentTile, new Vector2(-spaceBetweenTiles, 0f));
            hasMoved = true;
          }
        }
      }
    }
    return hasMoved;
  }

  private bool MoveTilesRight() {
    bool hasMoved = false;
    for (int x = cols - 1; x >= 0; x--) {
      for (int y = rows - 1; y >= 0; y--) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);

        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the right is open
          RaycastHit2D hit = Physics2D.Raycast(currentTile.transform.position + horizontalRay, Vector2.right, raycastDistance);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
                hasMoved = true;
              }
            }
            stopped = true;
          } else {
            UpdateGrid(currentTile, new Vector2(spaceBetweenTiles, 0f));
            hasMoved = true;
          }
        }
      }
    }
    return hasMoved;
  }

  private bool MoveTilesUp() {
    bool hasMoved = false;
    for (int y = 1; y < rows; y++) {
      for (int x = 0; x < cols; x++) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);
        
        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the top is open
          RaycastHit2D hit = Physics2D.Raycast(currentTile.transform.position + verticalRay, Vector2.up, raycastDistance);

          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
                hasMoved = true;
              }
            }
            stopped = true;
          } else {
            UpdateGrid(currentTile, new Vector2(0f, spaceBetweenTiles));
            hasMoved = true;
          }
        }
      }
    }
    return hasMoved;
  }

  private bool MoveTilesDown() {
    bool hasMoved = false;
    for (int y = rows - 1; y >= 0; y--) {
      for (int x = 0; x < cols; x++) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);
        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the left is open
          RaycastHit2D hit = Physics2D.Raycast(currentTile.transform.position - verticalRay, -Vector2.up, raycastDistance);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
                hasMoved = true;
              }
            }
            stopped = true;
          } else {
            UpdateGrid(currentTile, new Vector2(0f, -spaceBetweenTiles));
            hasMoved = true;
          }
        }
      }
    }
    return hasMoved;
  }

  private GameObject GetObjectAtGridPosition(int x, int y) {
    RaycastHit2D hit = Physics2D.Raycast(GridToWorldPoint(x, y), Vector2.right, 0.1f);

    if (hit) {
      return hit.collider.gameObject;
    } else {
      throw new UnityException("Unable to find gameObject in grid position (" + x + ", " + y + ")");
    }
  }
}
