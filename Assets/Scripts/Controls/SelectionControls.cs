﻿#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionControls : MonoBehaviour
{
    [SerializeField]
    private Camera m_battlegroundCamera;

    [SerializeField]
    private LayerMask m_unitLayerMask;

    [SerializeField]
    private LayerMask m_movementfieldLayerMask;

    [SerializeField]
    private LayerMask m_attackfieldLayerMask;

    [SerializeField]
    private CameraControls m_cameraControls;

    private BaseUnit m_currentlySelectedUnit;
    private bool m_abortNextSelectionTry;

    private List<Vector2> m_routeToDestinationField;

    private Dictionary<Vector2, PathfindingNodeDebugData> m_pathfindingNodeDebug;

    private BattlegroundUI m_battlegroundUi;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (m_routeToDestinationField == null || m_pathfindingNodeDebug == null)
        {
            return;
        }

        foreach (var node in m_pathfindingNodeDebug)
        {
            //Debug.Log(node);

            BaseMapTile baseMapTileOnPath = ControllerContainer.TileNavigationController.GetMapTile(node.Key);

            if (baseMapTileOnPath != null)
            {
                // Draw sphere
                Handles.color = m_routeToDestinationField.Contains(node.Key) ? Color.red : Color.gray;
                Handles.SphereCap(0, baseMapTileOnPath.transform.position + Vector3.up, Quaternion.identity, 0.5f);

                // Draw text label
                GUIStyle guiStyle = new GUIStyle { normal = { textColor = Color.black }, alignment = TextAnchor.MiddleCenter };
                Handles.Label(baseMapTileOnPath.transform.position + Vector3.up + Vector3.back, 
                    string.Format("C{0} P{1}", node.Value.CostToMoveToNode, node.Value.NodePriority), guiStyle);
            }
        }
    }
#endif

    private void Start()
    {
        ControllerContainer.MonoBehaviourRegistry.TryGet(out m_battlegroundUi);

        ControllerContainer.BattleController.AddTurnEndEvent("DeselectUnit", DeselectCurrentUnit);
    }

    // Update is called once per frame
    private void Update ()
    {
        if (Input.GetMouseButton(0) && m_cameraControls.IsDragging)
        {
            m_abortNextSelectionTry = true;
            //Debug.Log("Aborting Next Selection Try");
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (ControllerContainer.BattleController.IsPlayersTurn() && !m_abortNextSelectionTry)
            {
                RaycastHit raycastHit;

                if (m_currentlySelectedUnit != null && TrySelection(m_battlegroundCamera, m_attackfieldLayerMask, out raycastHit))
                {
                    Debug.Log("Selected attack field");

                    // Select attack field
                    BaseUnit unitToAttack = raycastHit.transform.parent.parent.GetComponent<BaseUnit>();

                    if (unitToAttack != null)
                    {
                        m_currentlySelectedUnit.AttackUnit(unitToAttack);
                        DeselectCurrentUnit();
                    }
                }
                else if (TrySelection(m_battlegroundCamera, m_unitLayerMask, out raycastHit))
                {
                    Debug.Log("Selected unit");

                    // Select Unit
                    BaseUnit selectedUnit = raycastHit.transform.GetComponent<BaseUnit>();

                    if (selectedUnit != null && selectedUnit.CanUnitTakeAction())
                    {
                        DeselectCurrentUnit();

                        m_currentlySelectedUnit = selectedUnit;
                        m_currentlySelectedUnit.OnUnitWasSelected();
                    }
                }
                else if (m_currentlySelectedUnit != null &&
                         TrySelection(m_battlegroundCamera, m_movementfieldLayerMask, out raycastHit))
                {
                    Debug.Log("Selected movement field");

                    // Select movement field
                    BaseMapTile baseMapTile = raycastHit.transform.parent.parent.GetComponent<BaseMapTile>();

                    if (baseMapTile != null)
                    {
                        m_routeToDestinationField = ControllerContainer.TileNavigationController.
                            GetBestWayToDestination(m_currentlySelectedUnit, baseMapTile, out m_pathfindingNodeDebug);
                        m_currentlySelectedUnit.DisplayRouteToDestination(m_routeToDestinationField, DeselectCurrentUnit);

                        m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(true);
                    }
                    
                }
                else if (m_currentlySelectedUnit != null && !IsPointerOverUIObject())
                {
                    // Deselect unit
                    DeselectCurrentUnit();
                    Debug.Log("Deselected Unit");
                }
            }
            else if (m_abortNextSelectionTry)
            {
                m_abortNextSelectionTry = false;

                //Debug.Log("Selection Try was aborted!");
            }
        }
    }

    /// <summary>
    /// De-Selects the current unit.
    /// </summary>
    private void DeselectCurrentUnit()
    {
        if (m_currentlySelectedUnit == null)
        {
            return;
        }

        m_currentlySelectedUnit.OnUnitWasDeselected();
        m_currentlySelectedUnit = null;

        m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(false);
    }

    /// <summary>
    /// Tries selecting a unit
    /// </summary>
    /// <param name="cameraToBaseSelectionOn">The camera to base selection on.</param>
    /// <param name="selectionMask">The selection mask.</param>
    /// <param name="selectionTarget">The selection target.</param>
    /// <returns></returns>
    private bool TrySelection(Camera cameraToBaseSelectionOn, LayerMask selectionMask, out RaycastHit selectionTarget)
    {
        Ray selectionRay = cameraToBaseSelectionOn.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));

        //To support orthographic cameras
        if (cameraToBaseSelectionOn.orthographic)
        {
            selectionRay.direction = cameraToBaseSelectionOn.transform.forward.normalized;
        }

        Debug.DrawRay(selectionRay.origin, selectionRay.direction * 100f, Color.yellow, 1f);

        return Physics.Raycast(selectionRay, out selectionTarget, 100f, selectionMask);
    }

    /// <summary>
    /// Determines whether a pointer is over an UI object.
    /// This is the only solution that also works on mobile.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if [is pointer over UI object]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
