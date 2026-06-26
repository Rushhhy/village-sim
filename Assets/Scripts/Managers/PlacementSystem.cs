using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlacementSystem : MonoBehaviour
{
    public event Action<int> OnStructureBuilt;
    public event Action<int, int> OnStructureRemoved;
    public event Action<Vector3Int, int, int> OnMoved;

    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private RoadManager roadManager;
    [SerializeField] private BuildingRegistryManager buildingUIManager;
    [SerializeField] private UIController userInterface;
    [SerializeField] private StructuresDatabaseSO database;

    public int selectedObjectIndex = -1;
    private int selectedDatabaseIndex = -1;

    public GridData gridData;

    public List<GameObject> placedGameObjects = new();
    public List<GameObject> placedDecorations = new();

    public bool isBuilding = false;
    public GameObject previewStructure;

    private GameObject buildingBlock;
    private GameObject occupiedBuildingBlock;

    [SerializeField] private GameObject occupiedBlockOne;
    [SerializeField] private GameObject unoccupiedBlockOne;
    [SerializeField] private GameObject occupiedBlockTwo;
    [SerializeField] private GameObject unoccupiedBlockTwo;
    [SerializeField] private GameObject occupiedBlockThree;
    [SerializeField] private GameObject unoccupiedBlockThree;

    private int numberOfRoadsPlaced = 0;

    private readonly Color whiteColor = new(1f, 1f, 1f, 0.9215f);
    private readonly Color redColor = new(1f, 0.2688f, 0.2688f, 0.9215f);

    private void Start()
    {
        StopPlacement();
        gridData = new GridData();

        InitializeGridData();
        PlaceMinesMarketsRefinery();
    }

    private void InitializeGridData()
    {
        gridData.AddPositionsInArea(new Vector3Int(-27, 33, 0), new Vector3Int(18, 52, 0));
        gridData.AddPositionsInArea(new Vector3Int(-30, 26, 0), new Vector3Int(-8, 31, 0));
        gridData.AddPositionsInArea(new Vector3Int(-28, 32, 0), new Vector3Int(11, 32, 0));
        gridData.AddPositionsInArea(new Vector3Int(-28, 25, 0), new Vector3Int(-9, 25, 0));
        gridData.AddPositionsInArea(new Vector3Int(-28, 36, 0), new Vector3Int(-28, 53, 0));
        gridData.AddPositionsInArea(new Vector3Int(19, 36, 0), new Vector3Int(20, 50, 0));
        gridData.AddPositionsInArea(new Vector3Int(-22, 53, 0), new Vector3Int(-9, 55, 0));
        gridData.AddPositionsInArea(new Vector3Int(-8, 53, 0), new Vector3Int(-2, 54, 0));
        gridData.AddPositionsInArea(new Vector3Int(-1, 53, 0), new Vector3Int(12, 55, 0));
        gridData.AddPositionsInArea(new Vector3Int(-7, 28, 0), new Vector3Int(-6, 31, 0));
        gridData.AddPositionsInArea(new Vector3Int(-4, 28, 0), new Vector3Int(7, 28, 0));
        gridData.AddPositionsInArea(new Vector3Int(7, 29, 0), new Vector3Int(11, 29, 0));
        gridData.AddPositionsInArea(new Vector3Int(21, 38, 0), new Vector3Int(21, 44, 0));

        gridData.RemovePositionsInArea(new Vector3Int(12, 36, 0), new Vector3Int(18, 39, 0));
        gridData.RemovePositionsInArea(new Vector3Int(-24, 33, 0), new Vector3Int(-18, 36, 0));

        gridData.mapBoundaries.ExceptWith(gridData.positionsToRemove);
    }

    public void StartPlacement(int databaseIndex)
    {
        selectedDatabaseIndex = databaseIndex;

        if (!IsValidDatabaseIndex(selectedDatabaseIndex))
        {
            Debug.LogError($"No database entry found at index {databaseIndex}");
            return;
        }

        if (IsRotatableDecoration(selectedDatabaseIndex))
        {
            userInterface.ActivateBuildRotatePanel();
        }
        else if (IsRoad(selectedDatabaseIndex))
        {
            inputManager.OnMouseTapped -= HandleRoadPlacement;
            inputManager.OnMouseTapped += HandleRoadPlacement;
            userInterface.ActivateRoadBuildPanel();
        }
        else
        {
            userInterface.ActivateBuildPanel();
        }

        isBuilding = true;

        if (!IsRoad(selectedDatabaseIndex))
        {
            Vector3Int initialCellPosition = GetCenteredCellPosition();
            int structureLength = GetSelectedStructureData().Size.x;

            if (structureLength > 2)
            {
                initialCellPosition = new Vector3Int(initialCellPosition.x - 1, initialCellPosition.y, 0);
            }

            CreatePreviewStructure(GetSelectedStructureData().LevelOnePrefab, initialCellPosition);
            CreateDisplayBlocks(structureLength, initialCellPosition, false, false);

            inputManager.OnMouseTapped -= MoveStructure;
            inputManager.OnMouseTapped += MoveStructure;

            UpdatePreviewPlacementState(initialCellPosition);
        }
    }

    private void HandleRoadPlacement()
    {
        Vector3 worldPosition = inputManager.GetSelectedMapPosition();
        Vector3Int cellPosition = grid.WorldToCell(worldPosition);

        bool placementValidity = gridData.CanPlaceObjectAt(cellPosition, GetSelectedStructureData().Size);
        if (placementValidity)
        {
            List<Vector3Int> neighbourRoadPositions = gridData.GetNeighbourRoadPositions(cellPosition);
            bool[] neighbourRoadCheck = gridData.GetNeighbouringRoads(cellPosition);

            roadManager.FixRoadAt(cellPosition, neighbourRoadCheck);
            roadManager.FixNeighbouringRoadsAt(neighbourRoadPositions);

            numberOfRoadsPlaced++;
            userInterface.UpdateRoadPlacedUI(numberOfRoadsPlaced);
        }
        else
        {
            int placedStructureId = gridData.GetIDAtPosition(cellPosition);
            if (IsRoadByStructureId(placedStructureId))
            {
                RemoveStructureAt(cellPosition);

                List<Vector3Int> neighbourRoadPositions = gridData.GetNeighbourRoadPositions(cellPosition);
                roadManager.FixNeighbouringRoadsAt(neighbourRoadPositions);

                numberOfRoadsPlaced--;
                userInterface.UpdateRoadPlacedUI(numberOfRoadsPlaced);
            }
        }
    }

    public void RemoveStructureAt(Vector3Int position)
    {
        PlacementData placementData = gridData.GetPlacementDataAt(position);
        if (placementData == null)
        {
            Debug.LogWarning("No structure found at this position.");
            return;
        }

        selectedObjectIndex = placementData.PlaceObjectIndex;
        selectedDatabaseIndex = GetDatabaseIndexByStructureID(placementData.ID);

        if (!IsValidDatabaseIndex(selectedDatabaseIndex))
        {
            Debug.LogWarning($"No database index found for structure ID {placementData.ID}");
            return;
        }

        GameObject objectToRemove;

        if (isStructure())
        {
            objectToRemove = placedGameObjects[selectedObjectIndex];
            placedGameObjects.RemoveAt(selectedObjectIndex);
            gridData.RemoveObjectAt(position);
            ShiftPlacementIndicesAfterRemoval(placedGameObjects, selectedObjectIndex);
        }
        else
        {
            objectToRemove = placedDecorations[selectedObjectIndex];
            placedDecorations.RemoveAt(selectedObjectIndex);
            gridData.RemoveObjectAt(position);
            OnStructureRemoved?.Invoke(selectedObjectIndex, 1);
            ShiftPlacementIndicesAfterRemoval(placedDecorations, selectedObjectIndex);
        }

        Destroy(objectToRemove);
    }

    public void PlaceStructure()
    {
        if (previewStructure == null || !IsValidDatabaseIndex(selectedDatabaseIndex))
        {
            return;
        }

        Vector3 worldPosition = previewStructure.transform.position;
        Vector3Int gridPosition = grid.WorldToCell(worldPosition);

        bool placementValidity = gridData.CanPlaceObjectAt(gridPosition, GetSelectedStructureData().Size);
        if (!placementValidity)
        {
            return;
        }

        GameObject newObject = Instantiate(GetSelectedStructureData().LevelOnePrefab);
        newObject.transform.position = grid.CellToWorld(gridPosition);
        newObject.GetComponent<SpriteRenderer>().sortingOrder = -gridPosition.y * 10;

        if (isStructure())
        {
            placedGameObjects.Add(newObject);
            gridData.AddObjectAt(
                gridPosition,
                GetSelectedStructureData().Size,
                GetSelectedStructureData().ID,
                placedGameObjects.Count - 1,
                ObjectType.Object
            );

            Building building = newObject.GetComponent<Building>();
            if (building != null)
            {
                building.Index = placedGameObjects.Count - 1;
                building.UpgradeBuilding();
            }
        }
        else
        {
            placedDecorations.Add(newObject);
            gridData.AddObjectAt(
                gridPosition,
                GetSelectedStructureData().Size,
                GetSelectedStructureData().ID,
                placedDecorations.Count - 1,
                ObjectType.Decoration
            );
        }

        OnStructureBuilt?.Invoke(selectedDatabaseIndex);
        StopPlacement();
    }

    public void PlaceStructureAt(int structureID, Vector3Int position)
    {
        selectedDatabaseIndex = GetDatabaseIndexByStructureID(structureID);
        if (selectedDatabaseIndex < 0)
        {
            Debug.LogError($"No object found with structure ID {structureID}");
            return;
        }

        bool placementValidity = gridData.CanPlaceObjectAt(position, GetSelectedStructureData().Size);
        if (!placementValidity)
        {
            Debug.LogWarning("Placement is invalid.");
            return;
        }

        GameObject newObject = Instantiate(GetSelectedStructureData().LevelOnePrefab);
        newObject.transform.position = grid.CellToWorld(position);
        newObject.GetComponent<SpriteRenderer>().sortingOrder = -position.y * 10;

        if (isStructure())
        {
            placedGameObjects.Add(newObject);
            gridData.AddObjectAt(
                position,
                GetSelectedStructureData().Size,
                GetSelectedStructureData().ID,
                placedGameObjects.Count - 1,
                ObjectType.Object
            );
        }
        else
        {
            placedDecorations.Add(newObject);
            OnStructureBuilt?.Invoke(selectedDatabaseIndex);
            gridData.AddObjectAt(
                position,
                GetSelectedStructureData().Size,
                GetSelectedStructureData().ID,
                placedDecorations.Count - 1,
                ObjectType.Decoration
            );
        }
    }

    private void MoveStructure()
    {
        if (previewStructure == null)
        {
            Debug.LogWarning("Preview structure has been destroyed.");
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        int structureLength = GetSelectedStructureData().Size.x;
        Vector3 worldPosition = inputManager.GetSelectedMapPosition();

        if (structureLength == 2)
        {
            worldPosition = new Vector3(worldPosition.x - 0.5f, worldPosition.y, 0);
        }

        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        cellPosition.z = 0;

        if (structureLength > 2)
        {
            cellPosition.x--;
        }

        previewStructure.transform.position = cellPosition;

        if (buildingBlock != null)
        {
            buildingBlock.transform.position = cellPosition;
        }

        if (occupiedBuildingBlock != null)
        {
            occupiedBuildingBlock.transform.position = cellPosition;
        }

        UpdatePreviewPlacementState(cellPosition);
    }

    public void StopPlacement()
    {
        Destroy(previewStructure);
        Destroy(buildingBlock);
        Destroy(occupiedBuildingBlock);

        previewStructure = null;
        buildingBlock = null;
        occupiedBuildingBlock = null;

        userInterface.DeactivateBuildPanel();
        userInterface.DeactivateEditPanel();
        userInterface.DeactivateBuildRotatePanel();
        userInterface.DeactivateEditRotatePanel();

        selectedObjectIndex = -1;
        inputManager.OnMouseTapped -= MoveStructure;
        isBuilding = false;
    }

    public void StopRoadPlacement()
    {
        inputManager.OnMouseTapped -= HandleRoadPlacement;
        userInterface.DeactivateRoadBuildPanel();
        isBuilding = false;
        numberOfRoadsPlaced = 0;
        userInterface.UpdateRoadPlacedUI(numberOfRoadsPlaced);
    }

    public void Rotate()
    {
        if (selectedDatabaseIndex == -1 || previewStructure == null)
        {
            return;
        }

        Vector3Int position = Vector3Int.RoundToInt(previewStructure.transform.position);

        if (selectedDatabaseIndex == 43)
        {
            selectedDatabaseIndex++;
        }
        else if (selectedDatabaseIndex == 44)
        {
            selectedDatabaseIndex--;
        }
        else if (selectedDatabaseIndex > 44 && selectedDatabaseIndex < 48)
        {
            selectedDatabaseIndex++;
        }
        else if (selectedDatabaseIndex == 48)
        {
            selectedDatabaseIndex = 45;
        }
        else
        {
            return;
        }

        Destroy(previewStructure);
        CreatePreviewStructure(GetSelectedStructureData().LevelOnePrefab, position);

        bool placementValidity = gridData.CanPlaceObjectAt(position, GetSelectedStructureData().Size);
        previewStructure.GetComponent<SpriteRenderer>().color = placementValidity ? whiteColor : redColor;
    }

    public void SelectStructure(Vector3Int cellPosition)
    {
        if (isBuilding)
        {
            return;
        }

        PlacementData placementData = gridData.GetPlacementDataAt(cellPosition);
        if (placementData == null)
        {
            return;
        }

        int databaseIndex = GetDatabaseIndexByStructureID(placementData.ID);
        if (!IsValidDatabaseIndex(databaseIndex))
        {
            Debug.LogWarning($"No database index found for structure ID {placementData.ID}");
            return;
        }

        isBuilding = true;
        selectedObjectIndex = placementData.PlaceObjectIndex;
        selectedDatabaseIndex = databaseIndex;

        Vector3Int objectPosition = placementData.occupiedPositions[0];

        if (IsRoad(selectedDatabaseIndex))
        {
            BeginRoadEditing(cellPosition, objectPosition);
            return;
        }

        ActivateEditUIForSelectedObject();
        HideSelectedPlacedObject();
        CreatePreviewForSelectedObject(objectPosition);

        gridData.RemoveObjectAt(objectPosition);

        inputManager.OnMouseTapped -= MoveStructure;
        inputManager.OnMouseTapped += MoveStructure;

        int structureLength = GetSelectedStructureData().Size.x;
        CreateDisplayBlocks(structureLength, objectPosition, true, false);
    }

    public void Demolish()
    {
        if (selectedObjectIndex < 0)
        {
            return;
        }

        GameObject objectToRemove;

        if (isStructure())
        {
            objectToRemove = placedGameObjects[selectedObjectIndex];

            Building building = objectToRemove.GetComponent<Building>();
            if (building != null)
            {
                building.ClearVillagers();

                Vector3 smokePosition = previewStructure != null
                    ? previewStructure.transform.position
                    : objectToRemove.transform.position;

                building.SmokeEffectAt(smokePosition);
            }
            placedGameObjects.RemoveAt(selectedObjectIndex);
            ShiftPlacementIndicesAfterRemoval(placedGameObjects, selectedObjectIndex);
            OnStructureRemoved?.Invoke(selectedObjectIndex, 0);
        }
        else
        {
            objectToRemove = placedDecorations[selectedObjectIndex];
            placedDecorations.RemoveAt(selectedObjectIndex);
            ShiftPlacementIndicesAfterRemoval(placedDecorations, selectedObjectIndex);
            OnStructureRemoved?.Invoke(selectedObjectIndex, 1);
        }

        Destroy(objectToRemove);
        StopPlacement();
    }

    public void PlaceEditedStructure()
    {
        if (previewStructure == null || !IsValidDatabaseIndex(selectedDatabaseIndex) || selectedObjectIndex < 0)
        {
            return;
        }

        Vector3Int gridPosition = grid.WorldToCell(previewStructure.transform.position);

        bool placementValidity = gridData.CanPlaceObjectAt(gridPosition, GetSelectedStructureData().Size);
        if (!placementValidity)
        {
            return;
        }

        if (isStructure())
        {
            FinalizeEditedStructure(gridPosition);
        }
        else
        {
            FinalizeEditedDecoration(gridPosition);
        }
    }

    public void PlaceMinesMarketsRefinery()
    {
        List<Vector3Int> refineryPositions = new()
        {
            new Vector3Int(10, 30, 0), new Vector3Int(11, 30, 0), new Vector3Int(12, 30, 0)
        };

        List<Vector3Int> mineOnePositions = new()
        {
            new Vector3Int(14, 37, 0), new Vector3Int(15, 37, 0), new Vector3Int(16, 37, 0),
            new Vector3Int(14, 36, 0), new Vector3Int(15, 36, 0)
        };

        List<Vector3Int> mineTwoPositions = new()
        {
            new Vector3Int(-22, 33, 0), new Vector3Int(-21, 33, 0), new Vector3Int(-20, 33, 0),
            new Vector3Int(-22, 34, 0), new Vector3Int(-21, 34, 0), new Vector3Int(-20, 34, 0)
        };

        List<Vector3Int> mineThreePositions = new()
        {
            new Vector3Int(-6, 55, 0), new Vector3Int(-5, 55, 0), new Vector3Int(-4, 55, 0),
            new Vector3Int(-6, 56, 0), new Vector3Int(-5, 56, 0), new Vector3Int(-4, 56, 0)
        };

        List<Vector3Int> mineFivePositions = new()
        {
            new Vector3Int(11, 54, 0), new Vector3Int(12, 54, 0), new Vector3Int(13, 54, 0),
            new Vector3Int(11, 55, 0), new Vector3Int(12, 55, 0), new Vector3Int(13, 55, 0)
        };

        List<Vector3Int> mineFourPositions = new()
        {
            new Vector3Int(-23, 54, 0), new Vector3Int(-22, 54, 0), new Vector3Int(-21, 54, 0),
            new Vector3Int(-23, 55, 0), new Vector3Int(-22, 55, 0), new Vector3Int(-21, 55, 0)
        };

        List<Vector3Int> marketOnePositions = new()
        {
            new Vector3Int(-4, 29, 0), new Vector3Int(-3, 29, 0), new Vector3Int(-2, 29, 0), new Vector3Int(-1, 29, 0),
            new Vector3Int(-4, 27, 0), new Vector3Int(-3, 27, 0), new Vector3Int(-2, 27, 0), new Vector3Int(-1, 27, 0)
        };

        List<Vector3Int> marketTwoPositions = new()
        {
            new Vector3Int(3, 29, 0), new Vector3Int(4, 29, 0), new Vector3Int(5, 29, 0), new Vector3Int(6, 29, 0),
            new Vector3Int(3, 27, 0), new Vector3Int(4, 27, 0), new Vector3Int(5, 27, 0), new Vector3Int(6, 27, 0)
        };

        GameObject marketOne = Instantiate(database.objectsData[38].Base);
        marketOne.transform.position = new Vector3(-2.49f, 27.3f, 0);
        placedGameObjects.Add(marketOne);
        gridData.AddFixedObjects(marketOnePositions, 38, 0);
        OnStructureBuilt?.Invoke(38);
        buildingUIManager.UpgradeBuildingWithIndex(0, 38);
        marketOne.GetComponent<Building>().FinishConstruction();

        GameObject marketTwo = Instantiate(database.objectsData[38].Base);
        marketTwo.transform.position = new Vector3(4.53f, 27.3f, 0);
        placedGameObjects.Add(marketTwo);
        gridData.AddFixedObjects(marketTwoPositions, 38, 1);
        OnStructureBuilt?.Invoke(38);

        GameObject refinery = Instantiate(database.objectsData[31].Base);
        refinery.transform.position = new Vector3Int(10, 30, 0);
        refinery.GetComponent<SpriteRenderer>().sortingOrder = 300;
        placedGameObjects.Add(refinery);
        gridData.AddFixedObjects(refineryPositions, 31, 2);
        OnStructureBuilt?.Invoke(31);

        GameObject mineOne = Instantiate(database.objectsData[28].Base);
        mineOne.transform.position = new Vector3(14, 35.5f, 0);
        mineOne.GetComponent<SpriteRenderer>().sortingOrder = 360;
        placedGameObjects.Add(mineOne);
        gridData.AddFixedObjects(mineOnePositions, 28, 3);
        OnStructureBuilt?.Invoke(28);

        GameObject mineTwo = Instantiate(database.objectsData[28].Base);
        mineTwo.transform.position = new Vector3(-22, 32.5f, 0);
        mineTwo.GetComponent<SpriteRenderer>().sortingOrder = 330;
        placedGameObjects.Add(mineTwo);
        gridData.AddFixedObjects(mineTwoPositions, 28, 4);
        OnStructureBuilt?.Invoke(28);

        GameObject mineThree = Instantiate(database.objectsData[28].Base);
        mineThree.transform.position = new Vector3(-6, 54.5f, 0);
        mineThree.GetComponent<SpriteRenderer>().sortingOrder = 550;
        placedGameObjects.Add(mineThree);
        gridData.AddFixedObjects(mineThreePositions, 28, 5);
        OnStructureBuilt?.Invoke(28);

        GameObject mineFour = Instantiate(database.objectsData[28].Base);
        mineFour.transform.position = new Vector3(-23, 53.5f, 0);
        mineFour.GetComponent<SpriteRenderer>().sortingOrder = 540;
        placedGameObjects.Add(mineFour);
        gridData.AddFixedObjects(mineFourPositions, 28, 6);
        OnStructureBuilt?.Invoke(28);

        GameObject mineFive = Instantiate(database.objectsData[28].Base);
        mineFive.transform.position = new Vector3(11, 53.5f, 0);
        mineFive.GetComponent<SpriteRenderer>().sortingOrder = 540;
        placedGameObjects.Add(mineFive);
        gridData.AddFixedObjects(mineFivePositions, 28, 7);
        OnStructureBuilt?.Invoke(28);
    }

    private void BeginRoadEditing(Vector3Int selectedCellPosition, Vector3Int objectPosition)
    {
        RemoveStructureAt(objectPosition);

        inputManager.OnMouseTapped -= HandleRoadPlacement;
        inputManager.OnMouseTapped += HandleRoadPlacement;
        userInterface.ActivateRoadBuildPanel();

        List<Vector3Int> neighbourRoadPositions = gridData.GetNeighbourRoadPositions(selectedCellPosition);
        roadManager.FixNeighbouringRoadsAt(neighbourRoadPositions);

        numberOfRoadsPlaced--;
        userInterface.UpdateRoadPlacedUI(numberOfRoadsPlaced);
    }

    private void ActivateEditUIForSelectedObject()
    {
        if (IsRotatableDecoration(selectedDatabaseIndex))
        {
            userInterface.ActivateEditRotatePanel();
        }
        else
        {
            userInterface.ActivateEditPanel();
        }
    }

    private void HideSelectedPlacedObject()
    {
        if (IsBuilding(selectedDatabaseIndex))
        {
            if (selectedObjectIndex >= 0 && selectedObjectIndex < placedGameObjects.Count)
            {
                GameObject selectedBuilding = placedGameObjects[selectedObjectIndex];

                Building building = selectedBuilding.GetComponent<Building>();
                if (building != null)
                {
                    building.HideAssignedVillagers();
                }

                selectedBuilding.SetActive(false);
            }
        }
        else
        {
            if (selectedObjectIndex >= 0 && selectedObjectIndex < placedDecorations.Count)
            {
                placedDecorations[selectedObjectIndex].SetActive(false);
            }
        }
    }

    private void CreatePreviewForSelectedObject(Vector3Int objectPosition)
    {
        GameObject previewPrefab = GetPreviewPrefabForSelectedObject();
        if (previewPrefab == null)
        {
            Debug.LogWarning($"No preview prefab found for selected database index {selectedDatabaseIndex}.");
            return;
        }

        CreatePreviewStructure(previewPrefab, objectPosition);

        if (previewStructure != null)
        {
            SpriteRenderer spriteRenderer = previewStructure.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = whiteColor;
            }
        }
    }

    private GameObject GetPreviewPrefabForSelectedObject()
    {
        if (IsBuilding(selectedDatabaseIndex))
        {
            Building building = placedGameObjects[selectedObjectIndex].GetComponent<Building>();
            if (building == null)
            {
                return GetSelectedStructureData().LevelOnePrefab;
            }

            return building.Level switch
            {
                1 => GetSelectedStructureData().LevelOnePrefab,
                2 => GetSelectedStructureData().LevelTwoPrefab,
                3 => GetSelectedStructureData().LevelThreePrefab,
                _ => GetSelectedStructureData().LevelOnePrefab
            };
        }

        return GetSelectedStructureData().LevelOnePrefab;
    }

    private void FinalizeEditedStructure(Vector3Int gridPosition)
    {
        GameObject building = placedGameObjects[selectedObjectIndex];
        building.transform.position = gridPosition;
        building.SetActive(true);
        Building buildingComponent = building.GetComponent<Building>();
        if (buildingComponent != null)
        {
            buildingComponent.ShowAssignedVillagers();
            buildingComponent.SetBuildingSprite();
            buildingComponent.RefreshVillagersAfterMove();
        }

        gridData.AddObjectAt(
            gridPosition,
            GetSelectedStructureData().Size,
            GetSelectedStructureData().ID,
            selectedObjectIndex,
            ObjectType.Object
        );

        SpriteRenderer spriteRenderer = building.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -gridPosition.y * 10;
        }

        OnMoved?.Invoke(gridPosition, selectedObjectIndex, 0);
        buildingUIManager.ChangeBuildingToggleUIPlacement(
            selectedObjectIndex,
            GetSelectedStructureData().Size,
            gridPosition,
            0
        );

        StopPlacement();
    }

    private void FinalizeEditedDecoration(Vector3Int gridPosition)
    {
        GameObject oldDecoration = placedDecorations[selectedObjectIndex];
        Destroy(oldDecoration);

        GameObject newDecoration = Instantiate(GetSelectedStructureData().LevelOnePrefab);
        newDecoration.transform.position = grid.CellToWorld(gridPosition);

        SpriteRenderer spriteRenderer = newDecoration.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -gridPosition.y * 10;
        }

        placedDecorations[selectedObjectIndex] = newDecoration;

        gridData.AddObjectAt(
            gridPosition,
            GetSelectedStructureData().Size,
            GetSelectedStructureData().ID,
            selectedObjectIndex,
            ObjectType.Decoration
        );

        OnMoved?.Invoke(gridPosition, selectedObjectIndex, 1);
        buildingUIManager.ChangeBuildingToggleUIPlacement(
            selectedObjectIndex,
            GetSelectedStructureData().Size,
            gridPosition,
            1
        );

        StopPlacement();
    }

    private void ShiftPlacementIndicesAfterRemoval(List<GameObject> objectList, int removedIndex)
    {
        for (int i = removedIndex; i < objectList.Count; i++)
        {
            GameObject currentObject = objectList[i];
            Vector3 currentPosition = currentObject.transform.position;
            Vector3Int currentGridPosition = grid.WorldToCell(currentPosition);

            PlacementData currentData = gridData.GetPlacementDataAt(currentGridPosition);
            if (currentData != null)
            {
                currentData.PlaceObjectIndex--;
            }
        }
    }

    private void CreatePreviewStructure(GameObject prefab, Vector3Int position)
    {
        previewStructure = Instantiate(prefab);
        previewStructure.transform.position = position;

        SpriteRenderer spriteRenderer = previewStructure.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 1000;
        }
    }

    private void CreateDisplayBlocks(int structureLength, Vector3Int position, bool unoccupiedActive, bool occupiedActive)
    {
        Destroy(buildingBlock);
        Destroy(occupiedBuildingBlock);

        switch (structureLength)
        {
            case 1:
                buildingBlock = Instantiate(unoccupiedBlockOne);
                occupiedBuildingBlock = Instantiate(occupiedBlockOne);
                break;

            case 2:
                buildingBlock = Instantiate(unoccupiedBlockTwo);
                occupiedBuildingBlock = Instantiate(occupiedBlockTwo);
                break;

            case 3:
                buildingBlock = Instantiate(unoccupiedBlockThree);
                occupiedBuildingBlock = Instantiate(occupiedBlockThree);
                break;

            default:
                buildingBlock = Instantiate(unoccupiedBlockThree);
                occupiedBuildingBlock = Instantiate(occupiedBlockThree);
                break;
        }

        buildingBlock.transform.position = position;
        occupiedBuildingBlock.transform.position = position;

        buildingBlock.SetActive(unoccupiedActive);
        occupiedBuildingBlock.SetActive(occupiedActive);
    }

    private void UpdatePreviewPlacementState(Vector3Int cellPosition)
    {
        if (previewStructure == null)
        {
            return;
        }

        bool placementValidity = gridData.CanPlaceObjectAt(cellPosition, GetSelectedStructureData().Size);

        SpriteRenderer spriteRenderer = previewStructure.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = placementValidity ? whiteColor : redColor;
        }

        if (buildingBlock != null)
        {
            buildingBlock.SetActive(placementValidity);
        }

        if (occupiedBuildingBlock != null)
        {
            occupiedBuildingBlock.SetActive(!placementValidity);
        }
    }

    private Vector3Int GetCenteredCellPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector3Int.zero;
        }

        Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, cam.nearClipPlane);
        Vector3 worldPosition = cam.ScreenToWorldPoint(screenCenter);
        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        cellPosition.z = 0;

        return cellPosition;
    }

    private int GetDatabaseIndexByStructureID(int structureID)
    {
        return database.objectsData.FindIndex(data => data.ID == structureID);
    }

    private bool IsValidDatabaseIndex(int index)
    {
        return index >= 0 && index < database.objectsData.Count;
    }

    private StructureData GetSelectedStructureData()
    {
        return database.objectsData[selectedDatabaseIndex];
    }

    private StructureCategory GetStructureCategory(int databaseIndex)
    {
        if (!IsValidDatabaseIndex(databaseIndex))
        {
            return StructureCategory.Unknown;
        }

        return database.objectsData[databaseIndex].Category;
    }

    private bool IsRoad(int databaseIndex) => GetStructureCategory(databaseIndex) == StructureCategory.Road;

    private bool IsBuilding(int databaseIndex) => GetStructureCategory(databaseIndex) == StructureCategory.Building;

    private bool IsRotatableDecoration(int databaseIndex) => GetStructureCategory(databaseIndex) == StructureCategory.RotatableDecoration;

    private bool IsDecoration(int databaseIndex)
    {
        StructureCategory category = GetStructureCategory(databaseIndex);
        return category == StructureCategory.Decoration || category == StructureCategory.RotatableDecoration;
    }

    private bool IsRoadByStructureId(int structureId)
    {
        int databaseIndex = GetDatabaseIndexByStructureID(structureId);
        return IsRoad(databaseIndex);
    }

    private bool isStructure()
    {
        return IsBuilding(selectedDatabaseIndex);
    }

    private IEnumerator SelectStructureAfterEffect(Vector3Int objectPosition)
    {
        yield return new WaitForSeconds(0.3f);

        isBuilding = true;

        ActivateEditUIForSelectedObject();
        HideSelectedPlacedObject();
        CreatePreviewForSelectedObject(objectPosition);

        gridData.RemoveObjectAt(objectPosition);

        inputManager.OnMouseTapped -= MoveStructure;
        inputManager.OnMouseTapped += MoveStructure;

        int structureLength = GetSelectedStructureData().Size.x;
        CreateDisplayBlocks(structureLength, objectPosition, true, false);
    }
}