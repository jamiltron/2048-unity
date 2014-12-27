using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour {
  private static int rows = 4;
  private static int cols = 4;
  private static int lowestNewTileValue = 2;
  private static int highestNewTileValue = 4;
  private static float borderOffset = 0.05f;
  private static float horizontalSpacingOffset = -1.65f;
  private static float verticalSpacingOffset = 1.65f;
  private static float borderSpacing = 0.1f;
  private static float resetButtonWidth = 80f;
  private static float resetButtonHeight = 40f;
  private static float gameOverButtonWidth = 150f;
  private static float gameOverButtonHeight = 300f;
  private static float halfTileWidth = 0.55f;
  private static float spaceBetweenTiles = 1.1f;

  private int points;
  private List<GameObject> tiles;
  private GUIText scoreText;
  private Rect resetButton;
  private Rect gameOverButton;

  public GameObject noTile;
  public GameObject scoreObject;
  public GameObject[] tilePrefabs;
  public LayerMask backgroundLayer;
  public Transform resetButtonTransform;

  private enum State {
    Loaded, 
    WaitingForInput, 
    CheckingMatches,
    GameOver
  }

  private State state;

  #region monodevelop
  void Awake() {
    tiles = new List<GameObject>();
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
        ReadyTilesForUpgrading();
        state = State.WaitingForInput;
      } else {
        state = State.GameOver;
      }
    }
  }
  #endregion

  #region class methods
  private static Vector2 GridToWorldPoint(int x, int y) {
    return new Vector2(x + horizontalSpacingOffset + borderSpacing * x, 
                       -y + verticalSpacingOffset - borderSpacing * y);
  }
  
  private static Vector2 WorldToGridPoint(float x, float y) {
    return new Vector2((x - horizontalSpacingOffset) / (1 + borderSpacing),
                       (y - verticalSpacingOffset) / -(1 + borderSpacing));
  }
  #endregion

  #region private methods
  private bool CheckForMovesLeft() {
    if (tiles.Count < rows * cols) {
      return true;
    }
    
    for (int x = 0; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        Tile currentTile = GetObjectAtGridPosition(x, y).GetComponent<Tile>();
        Tile rightTile = GetObjectAtGridPosition(x + 1, y).GetComponent<Tile>();
        Tile downTile = GetObjectAtGridPosition (x, y + 1).GetComponent<Tile>();
        
        if (x != cols - 1 && currentTile.value == rightTile.value) {
          return true;
        } else if (y != rows - 1 && currentTile.value == downTile.value) {
          return true;
        }
      }
    }
    return false;
  }

  public void GenerateRandomTile() {
    if (tiles.Count >= rows * cols) {
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
      if (GetObjectAtGridPosition(x, y) == noTile) {
        found = true;
        Vector2 worldPosition = GridToWorldPoint(x, y);
        GameObject obj;
        if (value == lowestNewTileValue) {
          obj = (GameObject) Instantiate(tilePrefabs[0], worldPosition, transform.rotation);
        } else {
          obj = (GameObject) Instantiate(tilePrefabs[1], worldPosition, transform.rotation);
        }
        
        tiles.Add(obj);
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

  private GameObject GetObjectAtGridPosition(int x, int y) {
    RaycastHit2D hit = Physics2D.Raycast(GridToWorldPoint(x, y), Vector2.right, borderSpacing);
    
    if (hit && hit.collider.gameObject.GetComponent<Tile>() != null) {
      return hit.collider.gameObject;
    } else {
      return noTile;
    }
  }

  private bool MoveTilesDown() {
    bool hasMoved = false;
    for (int y = rows - 1; y >= 0; y--) {
      for (int x = 0; x < cols; x++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.y -= halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.up, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.y += spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.y = hit.point.y + halfTileWidth + borderOffset;
              if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private bool MoveTilesLeft() {
    bool hasMoved = false;
    for (int x = 1; x < cols; x++) {
      for (int y = 0; y < rows; y++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.x -= halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, -Vector2.right, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.x += spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.x = hit.point.x + halfTileWidth + borderOffset;
              if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private bool MoveTilesRight() {
    bool hasMoved = false;
    for (int x = cols - 1; x >= 0; x--) {
      for (int y = 0; y < rows; y++) {
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.x += halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.right, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.x -= spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.x = hit.point.x - halfTileWidth - borderOffset;
              if (!Mathf.Approximately(obj.transform.position.x, newPosition.x)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
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
        GameObject obj = GetObjectAtGridPosition(x, y);
        
        if (obj == noTile) {
          continue;
        }
        
        Vector2 raycastOrigin = obj.transform.position;
        raycastOrigin.y += halfTileWidth;
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.up, Mathf.Infinity);
        if (hit.collider != null) {
          GameObject hitObject = hit.collider.gameObject;
          if (hitObject != obj) {
            if (hitObject.tag == "Tile") {
              Tile thatTile = hitObject.GetComponent<Tile>();
              Tile thisTile = obj.GetComponent<Tile>();
              if (thisTile.power == thatTile.power && !thisTile.upgradedThisTurn && !thatTile.upgradedThisTurn) {
                UpgradeTile(obj, thisTile, hitObject, thatTile);
                hasMoved = true;
              } else {
                Vector3 newPosition = hitObject.transform.position;
                newPosition.y -= spaceBetweenTiles;
                if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                  obj.transform.position = newPosition;
                  hasMoved = true;
                }
              }
            } else if (hitObject.tag == "Border") {
              Vector3 newPosition = obj.transform.position;
              newPosition.y = hit.point.y - halfTileWidth - borderOffset;
              if (!Mathf.Approximately(obj.transform.position.y, newPosition.y)) {
                obj.transform.position = newPosition;
                hasMoved = true;
              }
            } 
          }
        }
      }
    }
    
    return hasMoved;
  }

  private void ReadyTilesForUpgrading() {
    foreach (var obj in tiles) {
      Tile tile = obj.GetComponent<Tile>();
      tile.upgradedThisTurn = false;
    }
  }

  private void Reset() {
    foreach (var tile in tiles) {
      Destroy(tile);
    }

    tiles.Clear();
    points = 0;
    scoreText.text = "0";
    state = State.Loaded;
  }

  private void UpgradeTile(GameObject toDestroy, Tile destroyTile, GameObject toUpgrade, Tile upgradeTile) {
    Vector3 toUpgradePosition = toUpgrade.transform.position;

    tiles.Remove(toDestroy);
    tiles.Remove(toUpgrade);
    Destroy(toDestroy);
    Destroy(toUpgrade);

    // create the upgraded tile
    GameObject newTile = (GameObject) Instantiate(tilePrefabs[upgradeTile.power], toUpgradePosition, transform.rotation);
    tiles.Add(newTile);
    Tile tile = newTile.GetComponent<Tile>();
    tile.upgradedThisTurn = true;

    points += upgradeTile.value * 2;
    scoreText.text = points.ToString();

    TileAnimationHandler tileAnim = newTile.GetComponent<TileAnimationHandler>();
    tileAnim.AnimateUpgrade();
  }
  #endregion
}
