using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InWorldButton : MonoBehaviour
{
    [Header("Drag and Drop Interactables")]
    [SerializeField] private ButtonInteractable[] onPressInteractables = new ButtonInteractable[0];
    [SerializeField] private ButtonInteractable[] onUnpressInteractables = new ButtonInteractable[0];

    [Header("Event-Based Interactables")]
    public UnityEvent OnPound;
    public UnityEvent OnUnpound;

    private bool pressed = false;

    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

#if UNITY_EDITOR
    public void OnDrawGizmosSelected()
    {
        //Draw gizmos for the drag and drop interactables
        foreach (ButtonInteractable bi in onPressInteractables)
        {
            DrawInteractableGizmos(bi);
        }
        foreach (ButtonInteractable bi2 in onUnpressInteractables)
        {
            DrawInteractableGizmos(bi2);
        }

        //Draw gizmos for the event-based ones
        for (int i = 0; i < OnPound.GetPersistentEventCount(); i++)
        {
            DrawEventBasedGizmos(OnPound, i);
        }
        for (int i = 0; i < OnUnpound.GetPersistentEventCount(); i++)
        {
            DrawEventBasedGizmos(OnUnpound, i);
        }
    }

    private void DrawInteractableGizmos(ButtonInteractable bi)
    {
        GameObject iObject = bi.gameObject;
        DrawBezierGizmo(transform, iObject.transform, Color.yellow);

        //Also draw any gizmos associated with the interactable themselves
        bi.OnDrawGizmosSelected();
    }

    private void DrawEventBasedGizmos(UnityEvent ev, int index)
    {
        MonoBehaviour eMono = ev.GetPersistentTarget(index) as MonoBehaviour;
        Transform eT = eMono.transform;
        DrawBezierGizmo(transform, eT, Color.yellow);
    }



    private void DrawBezierGizmo(Transform origin, Transform target, Color chosenColor)
    {
        Vector3 targetPos = target.position;
        Vector3 myPos = origin.position;
        float halfHeight = (targetPos.y - myPos.y) / 2;
        Vector3 bPoint = Vector3.up * halfHeight;

        Handles.DrawBezier(myPos, targetPos, (targetPos - bPoint), (myPos + bPoint), chosenColor, EditorGUIUtility.whiteTexture, 3f);
    }
#endif

    // This function is called by a Unity Event associated with a child of this button with a trigger that invokes the event when an object enters the trigger.
    public void GetPressedIfPounded(Collider coll)
    {
        //Check if it's the player
        PlayerController pCon = coll.GetComponent<PlayerController>();

        //Return if it's not the player or if the player isn't ground pounding
        if (pCon == null || pCon.GetPlayerState() != PlayerController.ePlayerState.GROUNDPOUND)
        {
            return;
        }

        anim.SetBool("pressed", true);
        pressed = true;

        //Actually trigger the interactables
        OnPound.Invoke();
        foreach (ButtonInteractable bi in onPressInteractables)
        {
            bi.OnPress();
        }
    }

    // This function is called by a Unity Event associated with a child of this button with a trigger that invokes the event when an object enters the trigger.
    public void GetUnpressedIfPressed(Collider coll)
    {
        //Check if it's the player
        PlayerController pCon = coll.GetComponent<PlayerController>();

        //Return if it's not the player
        if (pCon == null || !pressed)
        {
            return;
        }

        anim.SetBool("pressed", false);
        pressed = false;

        //Actually trigger the interactables
        OnUnpound.Invoke();
        foreach (ButtonInteractable bi in onUnpressInteractables)
        {
            bi.OnUnpress();
        }
    }
}
