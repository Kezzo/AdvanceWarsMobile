﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    [SerializeField]
    private GameObject m_selectionMarker;

    [SerializeField]
    private GameObject m_attackMarker;

    [SerializeField]
    private float m_worldMovementSpeed;

    [SerializeField]
    private MeshFilter m_meshFilter;

    [SerializeField]
    private MeshRenderer m_meshRenderer;

    [SerializeField]
    private UnitStatManagement m_statManagement;
    public UnitStatManagement StatManagement { get { return m_statManagement; } }

    public Team TeamAffinity { get; private set; }
    public UnitType UnitType { get; private set; }
    public bool UnitHasMovedThisRound { get; private set; }

    private bool m_unitHasAttackedThisRound;
    public bool UnitHasAttackedThisRound
    {
        get
        {
            return m_unitHasAttackedThisRound;
        }
        private set
        {
            m_unitHasAttackedThisRound = value;

            m_materialPropertyBlock.SetColor("_Color", value ? Color.gray : Color.white);

            m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);
        }
    }

    private MaterialPropertyBlock m_materialPropertyBlock;

    private Vector2 m_currentSimplifiedPosition;
    public Vector2 CurrentSimplifiedPosition { get { return m_currentSimplifiedPosition; } }

    private List<BaseMapTile> m_currentWalkableMapTiles;
    private BattlegroundUI m_battlegroundUi;

    private List<BaseUnit> m_attackableUnits;

    private void Start()
    {
        ControllerContainer.MonoBehaviourRegistry.TryGet(out m_battlegroundUi);

        m_materialPropertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Initializes the specified team.
    /// </summary>
    /// <param name="unitData">The unit data.</param>
    /// <param name="initialSimplifiedPosition">The initial simplified position.</param>
    public void Initialize(MapGenerationData.Unit unitData, Vector2 initialSimplifiedPosition)
    {
        TeamAffinity = unitData.m_Team;
        UnitType = unitData.m_UnitType;
        UnitHasMovedThisRound = false;

        m_currentSimplifiedPosition = initialSimplifiedPosition;

        if (Application.isPlaying)
        {
            ControllerContainer.BattleController.RegisterUnit(this);
        }

        m_statManagement.Initialize(this, GetUnitBalancing().m_Health);

        // Load balancing once here and keep for the round.
    }

    /// <summary>
    /// Kills this unit.
    /// </summary>
    public void Die()
    {
        ControllerContainer.BattleController.RemoveRegisteredUnit(this);
        // Play explosion effect and destroy delayed.

        Destroy(this.gameObject);
    }

    /// <summary>
    /// Attacks the unit.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    public void AttackUnit(BaseUnit baseUnit)
    {
        baseUnit.StatManagement.TakeDamage(GetUnitBalancing().m_Damage);
        baseUnit.ChangeVisibiltyOfAttackMarker(false);

        UnitHasAttackedThisRound = true;
        // An attack will always keep the unit from moving in this round.
        UnitHasMovedThisRound = true;
    }

    /// <summary>
    /// Sets the team color uv mesh.
    /// </summary>
    /// <param name="mesh">The mesh.</param>
    public void SetTeamColorUVMesh(Mesh mesh)
    {
        m_meshFilter.mesh = mesh;
    }

    /// <summary>
    /// Resets the unit.
    /// </summary>
    public void ResetUnit()
    {
        UnitHasMovedThisRound = false;
        UnitHasAttackedThisRound = false;
    }

    /// <summary>
    /// Called when this unit was selected.
    /// Will call the MovementService to get the positions the unit can move to
    /// </summary>
    public void OnUnitWasSelected()
    {
        Debug.LogFormat("Unit: '{0}' from Team: '{1}' was selected.", UnitType, TeamAffinity.m_TeamColor);

        m_selectionMarker.SetActive(true);

        if (!UnitHasMovedThisRound)
        {
            m_currentWalkableMapTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(this);
            SetWalkableTileFieldVisibiltyTo(true);
        }

        if (!UnitHasAttackedThisRound)
        {
            TryToDisplayActionOnUnitsInRange(out m_attackableUnits);
        }
    }

    /// <summary>
    /// Called when the unit was deselected.
    /// </summary>
    public void OnUnitWasDeselected()
    {
        m_selectionMarker.SetActive(false);
        ChangeVisibiltyOfAttackMarker(false);

        SetWalkableTileFieldVisibiltyTo(false);
        HideAllRouteMarker();

        m_currentWalkableMapTiles = null;

        ClearAttackableUnits(m_attackableUnits);
    }

    /// <summary>
    /// Changes the visibilty of attack marker.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    private void ChangeVisibiltyOfAttackMarker(bool setVisible)
    {
        m_attackMarker.SetActive(setVisible);
    }

    /// <summary>
    /// Hides all route marker.
    /// </summary>
    private void HideAllRouteMarker()
    {
        if (m_currentWalkableMapTiles == null)
        {
            //Debug.LogError("Redundant call of HideAllRouteMarker.");
            return;
        }

        for (int tileIndex = 0; tileIndex < m_currentWalkableMapTiles.Count; tileIndex++)
        {
            m_currentWalkableMapTiles[tileIndex].HideAllRouteMarker();
        }
    }

    /// <summary>
    /// Sets the walkable tile field visibilty to.
    /// </summary>
    /// <param name="setVisibiltyTo">if set to <c>true</c> [set visibilty to].</param>
    private void SetWalkableTileFieldVisibiltyTo(bool setVisibiltyTo)
    {
        if (m_currentWalkableMapTiles == null)
        {
            //Debug.LogError("Redundant call of SetWalkableTileFieldVisibiltyTo.");
            return;
        }

        for (int tileIndex = 0; tileIndex < m_currentWalkableMapTiles.Count; tileIndex++)
        {
            m_currentWalkableMapTiles[tileIndex].ChangeVisibiltyOfMovementField(setVisibiltyTo);
        }
    }

    /// <summary>
    /// Determines whether this instance can be selected.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if this instance can be selected; otherwise, <c>false</c>.
    /// </returns>
    public bool CanUnitTakeAction()
    {
        return (!UnitHasMovedThisRound || !UnitHasAttackedThisRound) && ControllerContainer.BattleController.GetCurrentlyPlayingTeam().m_TeamColor == TeamAffinity.m_TeamColor;
    }

    /// <summary>
    /// Gets the unit balancing.
    /// </summary>
    /// <returns></returns>
    public SimpleUnitBalancing.UnitBalancing GetUnitBalancing()
    {
        return Root.Instance.SimeSimpleUnitBalancing.GetUnitBalancing(UnitType);
    }

    /// <summary>
    /// Displays the route to the destination.
    /// </summary>
    /// <param name="routeToDestination">The route to destination.</param>
    /// <param name="onUnitMovedToDestinationCallback">The on unit moved to destination callback.</param>
    public void DisplayRouteToDestination(List<Vector2> routeToDestination, Action onUnitMovedToDestinationCallback)
    {
        HideAllRouteMarker();
        SetWalkableTileFieldVisibiltyTo(true);

        var routeMarkerDefinitions = ControllerContainer.TileNavigationController.GetRouteMarkerDefinitions(routeToDestination);

        for (int routeMarkerIndex = 0; routeMarkerIndex < routeMarkerDefinitions.Count; routeMarkerIndex++)
        {
            var routeMarkerDefinition = routeMarkerDefinitions[routeMarkerIndex];

            BaseMapTile mapTile = ControllerContainer.TileNavigationController.GetMapTile(routeMarkerDefinition.Key);

            if (mapTile != null)
            {
                mapTile.DisplayRouteMarker(routeMarkerDefinition.Value);
            }
        }

        ControllerContainer.BattleController.AddConfirmMoveButtonPressedListener(() =>
        {
            ControllerContainer.BattleController.RemoveCurrentConfirmMoveButtonPressedListener();

            SetWalkableTileFieldVisibiltyTo(false);
            ClearAttackableUnits(m_attackableUnits);

            StartCoroutine(MoveAlongRoute(routeToDestination, () =>
            {
                if (TryToDisplayActionOnUnitsInRange(out m_attackableUnits))
                {
                    Debug.Log("Attackable units: "+m_attackableUnits.Count);

                    HideAllRouteMarker();
                    m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(false);
                }
                else
                {
                    UnitHasAttackedThisRound = true;

                    if (onUnitMovedToDestinationCallback != null)
                    {
                        onUnitMovedToDestinationCallback();
                    }
                }
                
            }));
        });
    }

    /// <summary>
    /// Clears the action on units.
    /// </summary>
    /// <param name="unitToClearActionsFrom">The unit to clear actions from.</param>
    private void ClearAttackableUnits(List<BaseUnit> unitToClearActionsFrom)
    {
        for (int unitIndex = 0; unitIndex < unitToClearActionsFrom.Count; unitIndex++)
        {
            unitToClearActionsFrom[unitIndex].ChangeVisibiltyOfAttackMarker(false);
        }

        unitToClearActionsFrom.Clear();
    }

    /// <summary>
    /// Tries to display action on units in range.
    /// For units that can take action on friendly units, it will display the field to do the friendly action on the unit.
    /// For enemy units the attack field will be displayed, if the unit can attack.
    /// </summary>
    /// <returns></returns>
    private bool TryToDisplayActionOnUnitsInRange(out List<BaseUnit> attackableUnits)
    {
        List<BaseUnit> unitsInRange =
            ControllerContainer.BattleController.GetUnitsInRange(this.CurrentSimplifiedPosition, GetUnitBalancing().m_AttackRange);

        attackableUnits = new List<BaseUnit>();

        for (int unitIndex = 0; unitIndex < unitsInRange.Count; unitIndex++)
        {
            BaseUnit unit = unitsInRange[unitIndex];

            // Is unit enemy or friend?
            if (unit.TeamAffinity.m_TeamColor != TeamAffinity.m_TeamColor)
            {
                if (GetUnitBalancing().m_AttackableUnitMetaTypes.Contains(unit.GetUnitBalancing().m_UnitMetaType))
                {
                    unit.ChangeVisibiltyOfAttackMarker(true);
                    attackableUnits.Add(unit);
                }
            }
            else
            {
                //TODO: Handle interaction with friendly units.
            }
        }

        return attackableUnits.Count > 0; // || supportableUnits.Count > 0;
    }

    /// <summary>
    /// Moves the along route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="onMoveFinished">The on move finished.</param>
    /// <returns></returns>
    public IEnumerator MoveAlongRoute(List<Vector2> route, Action onMoveFinished)
    {
        // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
        for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
        {
            Vector2 nodeToMoveTo = route[nodeIndex];
            Vector2 currentNode = route[nodeIndex - 1];

            yield return MoveToNeighborNode(currentNode, nodeToMoveTo);

            if (nodeIndex == route.Count - 1)
            {
                UnitHasMovedThisRound = true;

                if (onMoveFinished != null)
                {
                    onMoveFinished();
                }
            }
        }
    }

    /// <summary>
    /// Moves from to neighbor node.
    /// </summary>
    /// <param name="startNode">The start node.</param>
    /// <param name="destinationNode">The destination node.</param>
    private IEnumerator MoveToNeighborNode(Vector2 startNode, Vector2 destinationNode)
    {
        Vector2 nodePositionDiff = startNode - destinationNode;

        // Rotate unit to destination node
        CardinalDirection directionToRotateTo = ControllerContainer.TileNavigationController.GetCardinalDirectionFromNodePositionDiff(
            nodePositionDiff, false);

        switch (directionToRotateTo)
        {
            case CardinalDirection.North:
                this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                break;
            case CardinalDirection.East:
                this.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            case CardinalDirection.South:
                this.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                break;
            case CardinalDirection.West:
                this.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                break;
        }

        BaseMapTile mapTile = ControllerContainer.TileNavigationController.GetMapTile(destinationNode);
        Vector3 targetWorldPosition = Vector3.zero;

        if (mapTile != null)
        {
            targetWorldPosition = mapTile.UnitRoot.position;
            this.transform.SetParent(mapTile.UnitRoot, true);
        }
        else
        {
            Debug.LogErrorFormat("Unable to find destination MapTile for node: '{0}'", destinationNode);
            yield break;
        }

        // Move to world position
        while (true)
        {
            float movementStep = m_worldMovementSpeed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, movementStep);

            if (transform.position == targetWorldPosition)
            {
                m_currentSimplifiedPosition = mapTile.SimplifiedMapPosition;
                yield break;
            }

            yield return null;
        }
    }
}
