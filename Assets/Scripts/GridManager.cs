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
  private static float spaceBetweenTiles = 1.1f;
  private static Vector3 horizontalRay = new Vector3(0.6f, 0f, 0f);
  private static Vector3 verticalRay = new Vector3(0f, 0.6f, 0f);

  private int[,] grid = new int[rows,cols];
  private int currentTilesAmount = 0;
  
  public GameObject[] tilePrefabs;

  private enum State {
    Loaded, 
    WaitingForInput, 
    CheckingMatches
  }

  private State state;
	
  // Use this for initialization
  void Start () {
    state = State.Loaded;
  }
	
  // Update is called once per frame
  void Update () {
    if (state == State.Loaded) {
      state = State.WaitingForInput;
      GenerateRandomTile();
      GenerateRandomTile();
    } else if (state == State.WaitingForInput) {
      if (Input.GetButtonDown ("Left")) {
        MoveTilesLeft();
        state = State.CheckingMatches;
      } else if (Input.GetButtonDown ("Right")) {        
        MoveTilesRight();
        state = State.CheckingMatches;
      } else if (Input.GetButtonDown ("Up")) {
        MoveTilesUp();
        state = State.CheckingMatches;
      } else if (Input.GetButtonDown ("Down")) {
        MoveTilesDown();
        state = State.CheckingMatches;
      }
    } else if (state == State.CheckingMatches) {
      GenerateRandomTile();
      state = State.WaitingForInput;
    }
  }

  private static Vector2 GridToWorldPoint(int x, int y) {
    return new Vector2(x + horizontalSpacingOffset + borderSpacing * x, 
                       -y + verticalSpacingOffset - borderSpacing * y);
  }

  private static Vector2 WorldToGridPoint(float x, float y) {
    return new Vector2((x - horizontalSpacingOffset) / (1 + borderSpacing),
                       (y - verticalSpacingOffset)   / -(1 + borderSpacing));
  }

	public void GenerateRandomTile() {
		// make sure we can create tiles
		if (currentTilesAmount >= 16) {
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
    Instantiate (tilePrefabs[upgradeTile.power], toUpgradePosition, transform.rotation);

    // set the upgrade tile's grid value to double its current value
    grid[Mathf.RoundToInt(upgradeGridPoint.x), Mathf.RoundToInt(upgradeGridPoint.y)] = upgradeTile.value * 2;

    // clear out the destroyed tile's grid entry
    grid[Mathf.RoundToInt(destroyGridPoint.x), Mathf.RoundToInt(destroyGridPoint.y)] = 0;

    // destroy both tiles
    Destroy(toDestroy);
    Destroy(toUpgrade);
    currentTilesAmount--;
  }

  private void MoveTilesLeft() {
    for (int x = 1; x < 4; x++) {
      for (int y = 3; y >= 0; y--) {
        if (grid[x, y] == 0) {
          continue;
        }

        GameObject currentTile = GetObjectAtGridPosition(x, y);
        bool stopped = false;

        while (!stopped) {
          // see if the position to the left is open
          RaycastHit2D hit = Physics2D.Raycast (currentTile.transform.position - horizontalRay, -Vector2.right, 0.4f);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
              }
            }
            stopped = true;
          } else {
            UpdateGrid (currentTile, new Vector2(-spaceBetweenTiles, 0f));
          }
        }
      }
    }
  }

  private void MoveTilesRight() {
    for (int x = 3; x >= 0; x--) {
      for (int y = 3; y >= 0; y--) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);

        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the right is open
          RaycastHit2D hit = Physics2D.Raycast (currentTile.transform.position + horizontalRay, Vector2.right, 0.4f);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
              }
            }
            stopped = true;
          } else {
            UpdateGrid (currentTile, new Vector2(spaceBetweenTiles, 0f));
          }
        }
      }
    }
  }

  private void MoveTilesUp() {
    for (int y = 1; y < 4; y++) {
      for (int x = 0; x < 4; x++) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);
        
        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the top is open
          RaycastHit2D hit = Physics2D.Raycast (currentTile.transform.position + verticalRay, Vector2.up, 0.4f);

          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
              }
            }
            stopped = true;
          } else {

            UpdateGrid (currentTile, new Vector2(0f, spaceBetweenTiles));
          }
        }
      }
    }
  }

  private void MoveTilesDown() {
    for (int y = 3; y >= 0; y--) {
      for (int x = 0; x < 4; x++) {
        if (grid[x, y] == 0) {
          continue;
        }
        
        GameObject currentTile = GetObjectAtGridPosition(x, y);
        bool stopped = false;
        
        while (!stopped) {
          // see if the position to the left is open
          RaycastHit2D hit = Physics2D.Raycast (currentTile.transform.position - verticalRay, -Vector2.up, 0.4f);
          if (hit && hit.collider.gameObject != currentTile) {
            Tile otherTile = hit.collider.gameObject.GetComponent<Tile>();
            if (otherTile != null) {
              Tile thisTile = currentTile.GetComponent<Tile>();
              if (thisTile.power == otherTile.power) {
                UpgradeTile(currentTile, thisTile, hit.collider.gameObject, otherTile);
              }
            }
            stopped = true;
          } else {
            UpdateGrid (currentTile, new Vector2(0f, -spaceBetweenTiles));
          }
        }
      }
    }
  }

  private GameObject GetObjectAtGridPosition(int x, int y) {
    RaycastHit2D hit = Physics2D.Raycast (GridToWorldPoint (x, y), Vector2.right, 0.1f);

    if (hit) {
      return hit.collider.gameObject;
    } else {
      throw new UnityException("Unable to find gameObject in grid position (" + x + ", " + y + ")");
    }
  }
}
