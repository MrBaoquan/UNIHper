using System.Collections;
using System.Collections.Generic;
using UNIHper;
using UnityEngine;
using UnityEngine.EventSystems;

public class PropagatePointerEvents
    : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler,
        // IPointerEnterHandler,
        // IPointerExitHandler,
        IPointerMoveHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
{
    public bool PropagateClickEvent = true;
    public bool PropagateDragEvent = true;
    public bool PropagateEnterExitEvent = true;

    private GameObject lastClickTarget;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!PropagateClickEvent)
            return;
        lastClickTarget = this.NextPropagateEventTarget(eventData);
        ExecuteEvents.Execute(lastClickTarget, eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!PropagateClickEvent)
            return;
        var _nextEventTarget = this.NextPropagateEventTarget(eventData);
        if (_nextEventTarget == null || _nextEventTarget != lastClickTarget)
            return;
        ExecuteEvents.Execute(_nextEventTarget, eventData, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!PropagateClickEvent)
            return;
        var _nextEventTarget = this.NextPropagateEventTarget(eventData);
        if (_nextEventTarget == null || _nextEventTarget != lastClickTarget)
            return;
        ExecuteEvents.Execute(_nextEventTarget, eventData, ExecuteEvents.pointerClickHandler);
    }

    // public void OnPointerEnter(PointerEventData eventData)
    // {
    //     Debug.LogWarning("pointer enter");
    // }

    // public void OnPointerExit(PointerEventData eventData)
    // {
    //     Debug.LogWarning("pointer exit");
    // }

    private GameObject lastHoverTarget;

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!PropagateEnterExitEvent)
            return;
        var _nextEventTarget = this.NextPropagateEventTarget(eventData);

        if (_nextEventTarget == lastHoverTarget)
            return;
        if (_nextEventTarget != null)
        {
            ExecuteEvents.Execute(_nextEventTarget, eventData, ExecuteEvents.pointerEnterHandler);
        }
        if (lastHoverTarget != null)
        {
            ExecuteEvents.Execute(lastHoverTarget, eventData, ExecuteEvents.pointerExitHandler);
        }
        lastHoverTarget = _nextEventTarget;
    }

    private GameObject dragDropTarget;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!PropagateDragEvent)
            return;
        dragDropTarget = this.NextPropagateEventTarget(eventData);
        ExecuteEvents.Execute(dragDropTarget, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!PropagateDragEvent)
            return;
        if (dragDropTarget == null)
            return;
        ExecuteEvents.Execute(dragDropTarget, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!PropagateDragEvent)
            return;
        if (dragDropTarget == null)
            return;
        ExecuteEvents.Execute(dragDropTarget, eventData, ExecuteEvents.endDragHandler);
        dragDropTarget = null;
    }
}
